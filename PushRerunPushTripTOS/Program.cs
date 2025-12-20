using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Plugin.Logging;
using PushTrip.AdhocSchedule;
using PushTrip.Common;
using Serilog;
using Serilog.Sinks.MSSqlServer;
using Setting.Configuration.Application;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Data;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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
                new SqlColumn("RequestXml", SqlDbType.NVarChar, dataLength: -1),
                new SqlColumn("ResponseXml", SqlDbType.NVarChar,dataLength: -1),
                new SqlColumn("CustomData", SqlDbType.NVarChar, dataLength : -1)
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

            // Load your URL section
            Application.URL.TOSWebService = config["URL:TOSWebService"];
            Application.URL.SoapActionBase = config["URL:SoapActionBase"];
            Application.URL.Xmlns = config["URL:Xmlns"];

            // await the async worker
            await RerunAdhocSchedule(sourceConn).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var malaysiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            var ts = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone);
            LogMigrationException.Error(ts, "RerunTrip", "Main", null, null, null, "Unhandled exception in Main()", ex);
        }
        finally
        {
            // Ensure logs are flushed before exit
            (LogMigrationProcess.Logger as IDisposable)?.Dispose();
            (LogMigrationException.Logger as IDisposable)?.Dispose();
        }
    }

    static async Task RerunAdhocSchedule(string sourceConn)
    {
        var logs = new ConcurrentBag<(DateTime TimeStamp, string Type, string Process, string Message, string RequestXml, string ResponseXml, string CustomData, bool? IsSuccess)>();
        var malaysiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");

        try
        {
            // Load SQL
            string sqlPath = Path.Combine(Directory.GetCurrentDirectory(), "SQL", "RerunAdhocSchedule.sql");
            string sql = File.ReadAllText(sqlPath);

            using var source = new SqlConnection(sourceConn);
            await source.OpenAsync().ConfigureAwait(false);

            using var multi = await source.QueryMultipleAsync(sql).ConfigureAwait(false);
            List<AdhocScheduleModel> adhocModels = multi.Read<AdhocScheduleModel>().ToList();

            var groupedByTripDate = adhocModels
                .GroupBy(a => a.TripDate.Date)
                .OrderBy(g => g.Key);

            var url = Application.URL.TOSWebService;
            var soapActionBase = Application.URL.SoapActionBase;
            var xmlns = Application.URL.Xmlns;

            if (string.IsNullOrEmpty(url))
                throw new FurtherActionRequiredException(string.Format(ErrorMessage.MissingIntegrationInfo, "ApiUrl"));

            Uri requestUrl = new Uri(url);
            string soapAction = soapActionBase + Constant.ApiUrlKey.AdhocSchedule;

            foreach (var group in groupedByTripDate)
            {
                DateTime tripDate = group.Key;
                List<AdhocScheduleModel> allTrips = group.ToList();

                // Process each TripDate in batches of 50
                foreach (var batch in allTrips.Chunk(100))
                {
                    int batchSize = batch.Length;

                    string requestXml = null;
                    string responseXml = null;
                    AdhocScheduleResponseModel response;

                    var requestContent = new AdhocScheduleRequestModel
                    {
                        InsertList = new InsertList
                        {
                            Schedule = new Schedule
                            {
                                AdhocList = batch.Select(a => new Adhoc
                                {
                                    OperatorCode = a.OperatorCode,
                                    RouteNo = a.RouteNo,
                                    TripNo = a.TripNo,
                                    Type = a.Type,
                                    TripDate = a.TripDate.ToString("yyyy-MM-dd"),
                                    Date = a.Date.ToString("yyyy-MM-dd"),
                                    Time = FormatTime(a.Time),
                                    PlateNo = a.PlateNo,
                                    Position = a.Position,
                                    Remark = a.Remark
                                }).ToList()
                            }
                        }
                    };

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

                    // --- SOAP Insert ---
                    try
                    {
                        requestXml = SerializeToXml(requestContent, xmlns);

                        response = await WebServicePostAsync<AdhocScheduleResponseModel>(
                            requestUrl, soapAction, xmlns, requestContent, cts.Token);

                        responseXml = SerializeToXml(response, xmlns);

                        var adhocList = response?.AdhocScheduleInsertResult?.InsertStatus?.AdhocList;

                        var successTripNos = adhocList?.Where(x => x.Code == "1").Select(x => x.TripNo).ToList() ?? new List<string>();
                        var errorTripNos = adhocList?.Where(x => x.Code == "0").Select(x => x.TripNo).ToList() ?? new List<string>();

                        string customData = JsonSerializer.Serialize(new { success = successTripNos, error = errorTripNos });
                        bool isSuccess = adhocList != null && !errorTripNos.Any();

                        logs.Add((TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone),
                                  "RerunTrip", "Insert", $"Date: {tripDate:yyyy-MM-dd} Batch Records: {batchSize}",
                                  requestXml, responseXml, customData, isSuccess));
                    }
                    catch (Exception ex)
                    {
                        var ts = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone);
                        LogMigrationException.Error(ts, "RerunTrip", "Insert", requestXml, responseXml,
                                                   $"{tripDate:yyyy-MM-dd}", "Exception during Insert batch", ex);
                        continue; // move to next batch
                    }

                    // --- Database Update for this batch ---
                    try
                    {
                        string updateSqlPath = Path.Combine(Directory.GetCurrentDirectory(), "SQL", "AdhocDetailUpdate.sql");
                        string updateSql = File.ReadAllText(updateSqlPath);

                        using var conn = new SqlConnection(sourceConn);
                        await conn.OpenAsync();

                        bool isSuccess = true;

                        if (response?.AdhocScheduleInsertResult?.InsertStatus?.AdhocList != null)
                        {
                            foreach (var adhoc in response.AdhocScheduleInsertResult.InsertStatus.AdhocList)
                            {
                                var param = new AdhocDetailUpdateModel.Request()
                                {
                                    TripNo = adhoc.TripNo,
                                    GateNo = adhoc.Bay,
                                    GateNo2 = adhoc.Gate,
                                    TripDate = DateTime.Parse(adhoc.TripDate),
                                    AdhocId = adhoc.ScheduleId,
                                    Position = adhoc.Position,
                                    CompanyCode = adhoc.OperatorCode
                                };

                                int rows = await conn.ExecuteAsync(updateSql, param);
                                if (rows == 0) isSuccess = false;
                            }
                        }

                        logs.Add((TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone),
                                  "RerunTrip", "Update", $"Date: {tripDate:yyyy-MM-dd} Batch Records: {batchSize}",
                                  null, null, $"{tripDate:yyyy-MM-dd}", isSuccess));
                    }
                    catch (Exception ex)
                    {
                        var ts = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone);
                        LogMigrationException.Error(ts, "RerunTrip", "Update", requestXml, responseXml,
                                                   $"{tripDate:yyyy-MM-dd}", "Exception during Update batch", ex);
                    }
                } // end batch foreach
            } // end groupedByTripDate foreach
        }
        catch (Exception ex)
        {
            var ts = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone);
            LogMigrationException.Error(ts, "RerunTrip", "Overall", null, null, null, "Unhandled exception in AdhocSchedule() overall", ex);
        }
        finally
        {
            // Write all process logs at the end
            LogMigrationProcess.WriteAll(logs);
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

    static string FormatTime(string rawTime)
    {
        if (string.IsNullOrWhiteSpace(rawTime))
            return ""; // or return null if needed

        rawTime = rawTime.Trim();

        // If format is 6 digits: HHmmss
        if (rawTime.Length == 6)
        {
            string hh = rawTime.Substring(0, 2);
            string mm = rawTime.Substring(2, 4); // but last 2 digits are seconds

            return hh + ":" + mm.Substring(0, 2); // HH:mm only
        }

        // If format is 4 digits HHmm
        if (rawTime.Length == 4)
            return rawTime.Substring(0, 2) + ":" + rawTime.Substring(2, 2);

        return ""; // return empty if still invalid
    }

}
