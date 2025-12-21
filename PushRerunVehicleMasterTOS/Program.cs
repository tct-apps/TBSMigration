using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Plugin.Logging;
using PushMaster.Common;
using PushMaster.Vehicle;
using Serilog;
using Serilog.Sinks.MSSqlServer;
using Setting.Configuration.Application;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Data;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using static Dapper.SqlMapper;

class Program
{
    // Single shared HttpClient (do not recreate per-request).
    static readonly HttpClient SharedHttpClient = CreateHttpClient();

    static HttpClient CreateHttpClient()
    {
        var hc = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60) // global timeout; adjust as needed
        };

        // Default Accept header (optional).
        hc.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));

        return hc;
    }

    static async Task Main()
    {
        try
        {
            // Load configuration
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            string sourceConn = config.GetConnectionString("Source");

            // Initialize Serilog loggers with custom columns
            var columnOptions = new ColumnOptions();
            // Remove unwanted standard columns from the Store collection
            columnOptions.Store.Remove(StandardColumn.MessageTemplate);
            columnOptions.Store.Remove(StandardColumn.Level);
            columnOptions.Store.Remove(StandardColumn.Exception);
            columnOptions.Store.Remove(StandardColumn.Properties);

            columnOptions.AdditionalColumns = new Collection<SqlColumn>
            {
                new SqlColumn("Type", SqlDbType.NVarChar, dataLength: 100),
                new SqlColumn("Process", SqlDbType.NVarChar, dataLength: 100),
                new SqlColumn("IsSuccess", SqlDbType.Bit),
                new SqlColumn("RequestXml", SqlDbType.NVarChar, dataLength: -1, allowNull: true),
                new SqlColumn("ResponseXml", SqlDbType.NVarChar, dataLength: -1, allowNull: true),
                new SqlColumn("CustomData", SqlDbType.NVarChar, dataLength: -1, allowNull: true)
            };

            LogMigrationProcess.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.MSSqlServer(
                    connectionString: sourceConn,
                    tableName: "LogMigrationProcess",
                    autoCreateSqlTable: true,
                    columnOptions: columnOptions)
                .CreateLogger();

            LogMigrationException.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.MSSqlServer(
                    connectionString: sourceConn,
                    tableName: "LogMigrationException",
                    autoCreateSqlTable: true,
                    columnOptions: columnOptions)
                .CreateLogger();

            Application.URL.TOSWebService = config["URL:TOSWebService"];
            Application.URL.SoapActionBase = config["URL:SoapActionBase"];
            Application.URL.Xmlns = config["URL:Xmlns"];

            await RerunVehicle(sourceConn).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var malaysiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            var ts = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone);
            LogMigrationException.Error(ts, "RerunVehicle", "Main", null, null, null, "Unhandled exception in Main()", ex);
        }
        finally
        {
            // Ensure logs are flushed before exit
            (LogMigrationProcess.Logger as IDisposable)?.Dispose();
            (LogMigrationException.Logger as IDisposable)?.Dispose();
        }
    }

    static async Task RerunVehicle(string sourceConn)
    {
        var logs = new ConcurrentBag<(DateTime TimeStamp, string Type, string Process, string Message, string RequestXml, string ResponseXml, string CustomData, bool? IsSuccess)>();
        var malaysiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");

        logs.Add((TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone), "RerunVehicle", "Start", "RerunVehicle migration started", null, null, null, null));

        try
        {
            string sqlPath = Path.Combine(Directory.GetCurrentDirectory(), "SQL", "RerunVehicle.sql");
            string sql = File.ReadAllText(sqlPath);

            using var source = new SqlConnection(sourceConn);
            await source.OpenAsync().ConfigureAwait(false);

            using var multi = await source.QueryMultipleAsync(sql).ConfigureAwait(false);
            List<VehicleModel> vehicleList = multi.Read<VehicleModel>().ToList();

            var url = Application.URL.TOSWebService;
            var soapActionBase = Application.URL.SoapActionBase;
            var xmlns = Application.URL.Xmlns;

            if (string.IsNullOrEmpty(url))
                throw new FurtherActionRequiredException(string.Format(ErrorMessage.MissingIntegrationInfo, "ApiUrl"));

            Uri requestUrl = new Uri(url);
            string soapAction = soapActionBase + Constant.ApiUrlKey.VehicleInsert;

            int batchSize = 100;

            foreach (var batch in vehicleList.Chunk(batchSize))
            {
                var tasks = batch.Select(async vehicle =>
                {
                    string requestXml = null;
                    string responseXml = null;
                    bool isSuccess = false;

                    var requestContent = new VehicleRequestModel
                    {
                        PlateNo = vehicle.PlateNo,
                        OperatorCode = vehicle.OperatorCode
                    };

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

                    try
                    {
                        requestXml = SerializeToXml(requestContent, xmlns);
                        var response = await WebServicePostAsync<VehicleResponseModel>(requestUrl, soapAction, xmlns, requestContent, cts.Token);
                        responseXml = SerializeToXml(response, xmlns);

                        if (response.Result.Code == "0")
                        {
                            isSuccess = false;
                        }
                        else
                        {
                            isSuccess = true;
                        }

                        logs.Add((TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone),
                                  "RerunVehicle", "Insert", $"Rerun Vehicle: {vehicle.PlateNo}", requestXml, responseXml, vehicle.PlateNo, isSuccess));
                    }
                    catch (Exception ex)
                    {
                        var ts = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone);
                        LogMigrationException.Error(ts, "RerunVehicle", "Insert", requestXml, responseXml, $"{vehicle.PlateNo}", "Exception during Insert phase", ex);

                        // Always log failed vehicles
                        logs.Add((ts, "RerunVehicle", "Insert", $"FAILED Rerun Vehicle: {vehicle.PlateNo}", requestXml, responseXml, vehicle.PlateNo, false));
                    }
                });

                await Task.WhenAll(tasks);
            }

            logs.Add((TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone), "RerunVehicle", "End", "RerunVehicle migration ended", null, null, null, null));
        }
        catch (Exception ex)
        {
            var ts = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone);
            LogMigrationException.Error(ts, "RerunVehicle", "Overall", null, null, null, "Unhandled exception in RerunVehicle() overall", ex);
        }
        finally
        {
            // Write process logs
            LogMigrationProcess.WriteAll(logs.ToList());
        }
    }

    static string SerializeToXml(object obj, string defaultNamespace)
    {
        if (obj == null) return null;

        // namespaces for inner body only
        var ns = new XmlSerializerNamespaces();
        ns.Add("", defaultNamespace);

        // serialize inner object
        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(false),
            OmitXmlDeclaration = true
        };

        string innerXml;
        var serializer = new XmlSerializer(obj.GetType(), defaultNamespace);

        using (var sw = new StringWriter())
        using (var writer = XmlWriter.Create(sw, settings))
        {
            serializer.Serialize(writer, obj, ns);
            innerXml = sw.ToString();
        }

        // final SOAP envelope (exact as TOS)
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
                <soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                               xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
                               xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                  <soap:Body>
                {innerXml}
                  </soap:Body>
                </soap:Envelope>";
    }

    public static async Task ProcessInBatches<T>(IEnumerable<T> items, int batchSize, Func<T, Task> action)
    {
        foreach (var batch in items.Chunk(batchSize))
        {
            await Task.WhenAll(batch.Select(action));
        }
    }

    static async Task<T> WebServicePostAsync<T>(
        Uri requestUri,
        string soapAction,
        string xmlns,
        object requestContent,
        CancellationToken cancellationToken = default) where T : class
    {
        if (requestContent == null) throw new ArgumentNullException(nameof(requestContent));

        // Build SOAP envelope with the requestContent assigned into the Body.
        XmlAttributeOverrides requestXmlAttributeOverrides = new XmlAttributeOverrides();
        XmlRequestEnvelope envelope = new XmlRequestEnvelope();
        envelope.Body.Element = requestContent;

        XmlAttributes requestBodyXmlAttributes = new XmlAttributes();
        requestBodyXmlAttributes.XmlElements.Add(
            new XmlElementAttribute(GetXmlRootElementName(requestContent), requestContent.GetType()) { Namespace = xmlns });
        requestXmlAttributeOverrides.Add(typeof(XmlRequestEnvelope.Body_), nameof(XmlRequestEnvelope.Body_.Element), requestBodyXmlAttributes);

        XmlWriterSettings settings = new XmlWriterSettings { OmitXmlDeclaration = true, Encoding = Encoding.UTF8 };

        XmlSerializerNamespaces nss = new XmlSerializerNamespaces();
        nss.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
        nss.Add("xsd", "http://www.w3.org/2001/XMLSchema");
        nss.Add("soap", "http://schemas.xmlsoap.org/soap/envelope/");

        string contentString;
        using (StringWriter stringWriter = new StringWriter())
        using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter, settings))
        {
            var requestSerializer = new XmlSerializer(envelope.GetType(), requestXmlAttributeOverrides);
            requestSerializer.Serialize(xmlWriter, envelope, nss);
            contentString = stringWriter.ToString();
        }

        // Retry policy (simple exponential backoff)
        const int maxAttempts = 3;
        int attempt = 0;
        TimeSpan delay = TimeSpan.FromSeconds(1);

        while (true)
        {
            attempt++;
            // create new HttpRequestMessage each attempt
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new StringContent(contentString, Encoding.UTF8, "text/xml")
            };

            // SOAPAction is a header that many legacy services require exactly; set per-request.
            httpRequest.Headers.Remove("SOAPAction");
            httpRequest.Headers.Add("SOAPAction", soapAction);

            DateTime beforePost = DateTime.UtcNow;
            HttpResponseMessage response = null;
            try
            {
                // Use the shared HttpClient
                response = await SharedHttpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseContentRead, cancellationToken)
                                               .ConfigureAwait(false);
            }
            catch (OperationCanceledException oce) when (!cancellationToken.IsCancellationRequested)
            {
                // HttpClient timeout happened (TaskCanceled) — treat as transient
                Console.Error.WriteLine($"OperationCanceledException (likely timeout) on attempt {attempt}: {oce.Message}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"HttpClient SendAsync exception on attempt {attempt}: {ex.GetType().Name} - {ex.Message}");
            }

            if (response == null)
            {
                if (attempt >= maxAttempts)
                    throw new HttpRequestException($"No HTTP response after {attempt} attempts.");
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                delay = TimeSpan.FromSeconds(delay.TotalSeconds * 2);
                continue;
            }

            string responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            // If non-success, include body in exception to help debugging
            if (!response.IsSuccessStatusCode)
            {
                string msg = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}. Body: {Truncate(responseString, 2000)}";
                // Optionally treat some status codes as transient (429, 5xx)
                if ((int)response.StatusCode >= 500 && attempt < maxAttempts)
                {
                    Console.Error.WriteLine($"Server error (attempt {attempt}): {msg}");
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    delay = TimeSpan.FromSeconds(delay.TotalSeconds * 2);
                    continue;
                }
                throw new HttpRequestException(msg);
            }

            if (string.IsNullOrWhiteSpace(responseString))
                throw new XmlException("Response content is empty.");

            // Parse XML
            XDocument xDocument;
            try
            {
                xDocument = XDocument.Parse(responseString);
            }
            catch (Exception px)
            {
                throw new XmlException("Failed parsing response XML.", px);
            }

            // Detect SOAP Faults
            XNamespace soapNs = "http://schemas.xmlsoap.org/soap/envelope/";
            var fault = xDocument.Descendants(soapNs + "Fault").FirstOrDefault();
            if (fault != null)
            {
                string faultCode = fault.Element("faultcode")?.Value;
                string faultString = fault.Element("faultstring")?.Value;
                throw new InvalidOperationException($"SOAP Fault: {faultCode} - {faultString}");
            }

            // Determine expected response element name
            string expectedResponseName = GetXmlRootElementName<T>();
            // Try to locate an element whose local-name matches expectedResponseName (namespace-agnostic).
            var responseElement = xDocument.Descendants().FirstOrDefault(e => string.Equals(e.Name.LocalName, expectedResponseName, StringComparison.OrdinalIgnoreCase));

            // If not found, also try with common suffixes like "Response" or "Result"
            if (responseElement == null)
            {
                responseElement = xDocument.Descendants()
                    .FirstOrDefault(e =>
                        string.Equals(e.Name.LocalName, expectedResponseName + "Response", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(e.Name.LocalName, expectedResponseName + "Result", StringComparison.OrdinalIgnoreCase));
            }

            if (responseElement == null)
            {
                // If this was transient / unexpected response content, consider retrying (but usually not)
                string snippet = Truncate(responseString, 2000);
                throw new XmlException($"Response element '{expectedResponseName}' not found in SOAP response. Response snippet: {snippet}");
            }

            // Deserialize using XmlSerializer from that XElement
            try
            {
                var serializer = new XmlSerializer(typeof(T), xmlns);
                using var reader = responseElement.CreateReader();
                var obj = serializer.Deserialize(reader) as T;

                if (obj == null)
                    throw new InvalidOperationException($"Deserialized object is null for type {typeof(T).Name}");

                return obj;
            }
            catch (Exception ex)
            {
                // Provide the response snippet to ease debugging
                string snippet = Truncate(responseElement.ToString(), 2000);
                throw new InvalidOperationException($"Failed to deserialize element '{responseElement.Name.LocalName}' to {typeof(T).Name}. Element snippet: {snippet}", ex);
            }
        } // end while
    }

    static string Truncate(string s, int max) => string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s.Substring(0, max) + "...");

    static string GetXmlRootElementName<T>() => GetXmlRootElementName(typeof(T));
    static string GetXmlRootElementName(object obj) => GetXmlRootElementName(obj.GetType());
    static string GetXmlRootElementName(Type type)
    {
        var attributes = type.GetCustomAttributes(false);
        foreach (object attribute in attributes)
        {
            if (attribute is XmlRootAttribute xmlRootAttribute)
            {
                return string.IsNullOrWhiteSpace(xmlRootAttribute.ElementName) ? type.Name : xmlRootAttribute.ElementName;
            }
        }
        // fallback to class name if no XmlRoot attribute
        return type.Name;
    }
}
