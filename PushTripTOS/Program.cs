using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PushTripTOS.Model;
using PushTripTOS.Model.Common;
using Serilog;
using Serilog.Settings.Configuration;
using Serilog.Sinks.MSSqlServer;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Net.Http.Headers;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using static Dapper.SqlMapper;

class Program
{
    static void Main()
    {
        try
        {
            #region Load configuration
            // Load configuration from appsettings
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Initialize Serilog logger(s) from configuration
            //LogETLProcess.Logger = new LoggerConfiguration()
            //    .ReadFrom.Configuration(config, sectionName: "Serilog_ETLProcess")
            //    .CreateLogger();
            //LogETLException.Logger = new LoggerConfiguration()
            //    .ReadFrom.Configuration(config, sectionName: "Serilog_ETLException")
            //    .CreateLogger();

            string sourceConn = config.GetConnectionString("Source");
            #endregion

            State(sourceConn);
            City(sourceConn);
            BusOperator(sourceConn);
            Vehicle(sourceConn);
            Route(sourceConn);
        }
        catch (Exception ex)
        {
            var malaysiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            var ts = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone);
            //LogETLException.Error(ts, "Main", "Unhandled exception in Main()", ex);
        }
        finally
        {
            // Ensure logs are flushed before exit
            //(LogETLProcess.Logger as IDisposable)?.Dispose();
            //(LogETLException.Logger as IDisposable)?.Dispose();
        }
    }

    static void State(string sourceConn)
    {
        var logs = new List<(DateTime TimeStamp, string Project, string Message)>();
        var malaysiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");

        // Log process start
        logs.Add((TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone), $"StateStart", $"State process started"));

        try
        {
            string sqlPath = Path.Combine(
                             Directory.GetCurrentDirectory(),
                             "Model", "SQL", "State.sql");

            string sql = File.ReadAllText(sqlPath);

            using var source = new SqlConnection(sourceConn);
            source.Open();

            using var multi = source.QueryMultiple(sql);

            List<StateModel> stateList;

            // Read
            try
            {
                stateList = multi.Read<StateModel>().ToList();

                // Prepare connection.
                #region Get value 
                //var integrationInfo = vendorAccount.VendorAccountIntegrationInfo.ToList();
                string url = "http://10.238.1.4/toswebservice_Test/";
                string SoapAction = "stateInsert";
                //string xmlns = integrationInfo.FirstOrDefault(x => x.mdVendorIntegrationInfoKey == Constant.VendorAccountIntegrationInfoKey.Xmlns)?.Value;
                #endregion

                //if (string.IsNullOrEmpty(url))
                //{
                //    throw new FurtherActionRequiredException(string.Format(ErrorMessage.MissingIntegrationInfo, Constant.VendorAccountIntegrationInfoKey.ApiUrl));
                //}
                //Uri requestUrl = new Uri(url);

                //// Call API
                //string soapAction = SoapAction + Constant.ApiUrlKey.BlockSeat;
                //Entity.BlockSeat.BlockSeatResponse response = await WebServicePostAsync<Entity.BlockSeat.BlockSeatResponse>(requestUrl, soapAction, xmlns, blockSeatRequest,
                //    new LogInformation(batchId, jobId, vendorAccount.OperatorCode, nameof(Reserve), vendorAccount.mdVendorKey, vendorAccount.Code, booking.BookingId, bookingOperator.BookingOperatorId));

                // logging process read
                logs.Add((TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone), $"StateRead", $"State Read process started"));
            }
            catch (Exception ex)
            {
                var ts = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone);
                //LogETLException.Error(ts, $"{cts}FactTicketRead", "Exception during Read phase", ex);
                throw;
            }

            // Write process logs
            //LogETLProcess.WriteAll(logs);
        }
        catch (Exception ex)
        {
            var ts = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone);
            //LogETLException.Error(ts, $"{cts}FactTicketOverall", "Unhandled exception in FactTicket() overall", ex);
            throw;
        }

        //New table last jalan bila dan masa jalan tu ada error tak
        //Obj console app proses n see if success or has any error
    }

    static void City(string sourceConn)
    {
        var logs = new List<(DateTime TimeStamp, string Project, string Message)>();
        var malaysiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");

        // Log process start
        logs.Add((TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone), $"CityStart", $"City process started"));

        try
        {
            string sqlPath = Path.Combine(
                             Directory.GetCurrentDirectory(),
                             "Model", "SQL", "City.sql");

            string sql = File.ReadAllText(sqlPath);

            using var source = new SqlConnection(sourceConn);
            source.Open();

            using var multi = source.QueryMultiple(sql);

            List<CityModel> cityList;

            // Read
            try
            {
                cityList = multi.Read<CityModel>().ToList();

                // logging process read
                logs.Add((TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone), $"CityRead", $"City Read process started"));
            }
            catch (Exception ex)
            {
                var ts = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone);
                //LogETLException.Error(ts, $"{cts}FactTicketRead", "Exception during Read phase", ex);
                throw;
            }

            // Write process logs
            //LogETLProcess.WriteAll(logs);
        }
        catch (Exception ex)
        {
            var ts = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone);
            //LogETLException.Error(ts, $"{cts}FactTicketOverall", "Unhandled exception in FactTicket() overall", ex);
            throw;
        }

        //New table last jalan bila dan masa jalan tu ada error tak
        //Obj console app proses n see if success or has any error
    }

    static void BusOperator(string sourceConn)
    {
        var logs = new List<(DateTime TimeStamp, string Project, string Message)>();
        var malaysiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");

        // Log process start
        logs.Add((TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone), $"BusOperatorStart", $"BusOperator process started"));

        try
        {
            string sqlPath = Path.Combine(
                             Directory.GetCurrentDirectory(),
                             "Model", "SQL", "BusOperator.sql");

            string sql = File.ReadAllText(sqlPath);

            using var source = new SqlConnection(sourceConn);
            source.Open();

            using var multi = source.QueryMultiple(sql);

            List<BusOperatorModel> busOperatorList;

            // Read
            try
            {
                busOperatorList = multi.Read<BusOperatorModel>().ToList();

                foreach (var bo in busOperatorList)
                {
                    if (bo.operator_logo != null)
                    {
                        bo.base64Logo = Convert.ToBase64String(bo.operator_logo);
                    }
                }

                // logging process read
                logs.Add((TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone), $"BusOperatorRead", $"BusOperator Read process started"));
            }
            catch (Exception ex)
            {
                var ts = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone);
                //LogETLException.Error(ts, $"{cts}FactTicketRead", "Exception during Read phase", ex);
                throw;
            }

            // Write process logs
            //LogETLProcess.WriteAll(logs);
        }
        catch (Exception ex)
        {
            var ts = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone);
            //LogETLException.Error(ts, $"{cts}FactTicketOverall", "Unhandled exception in FactTicket() overall", ex);
            throw;
        }

        //New table last jalan bila dan masa jalan tu ada error tak
        //Obj console app proses n see if success or has any error
    }

    static void Vehicle(string sourceConn)
    {
        var logs = new List<(DateTime TimeStamp, string Project, string Message)>();
        var malaysiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");

        // Log process start
        logs.Add((TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone), $"VehicleStart", $"Vehicle process started"));

        try
        {
            string sqlPath = Path.Combine(
                             Directory.GetCurrentDirectory(),
                             "Model", "SQL", "Vehicle.sql");

            string sql = File.ReadAllText(sqlPath);

            using var source = new SqlConnection(sourceConn);
            source.Open();

            using var multi = source.QueryMultiple(sql);

            List<VehicleModel> vehicleList;

            // Read
            try
            {
                vehicleList = multi.Read<VehicleModel>().ToList();

                // logging process read
                logs.Add((TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone), $"VehicleRead", $"Vehicle Read process started"));
            }
            catch (Exception ex)
            {
                var ts = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone);
                //LogETLException.Error(ts, $"{cts}FactTicketRead", "Exception during Read phase", ex);
                throw;
            }

            // Write process logs
            //LogETLProcess.WriteAll(logs);
        }
        catch (Exception ex)
        {
            var ts = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone);
            //LogETLException.Error(ts, $"{cts}FactTicketOverall", "Unhandled exception in FactTicket() overall", ex);
            throw;
        }

        //New table last jalan bila dan masa jalan tu ada error tak
        //Obj console app proses n see if success or has any error
    }

    static void Route(string sourceConn)
    {
        var logs = new List<(DateTime TimeStamp, string Project, string Message)>();
        var malaysiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");

        // Log process start
        logs.Add((TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone), $"RouteStart", $"Route process started"));

        try
        {
            string sqlPath = Path.Combine(
                             Directory.GetCurrentDirectory(),
                             "Model", "SQL", "Route.sql");

            string sql = File.ReadAllText(sqlPath);

            using var source = new SqlConnection(sourceConn);
            source.Open();

            using var multi = source.QueryMultiple(sql);

            List<RouteModel> routeList;
            List<RouteDetailModel> routeDetailList;

            // Read
            try
            {
                routeList = multi.Read<RouteModel>().ToList();
                routeDetailList = multi.Read<RouteDetailModel>().ToList();

                foreach (var route in routeList)
                {
                    route.route_details = routeDetailList
                        .Where(d => d.route_no == route.route_no)
                        .ToList();
                }

                // logging process read
                logs.Add((TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone), $"RouteRead", $"Route Read process started"));
            }
            catch (Exception ex)
            {
                var ts = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone);
                //LogETLException.Error(ts, $"{cts}FactTicketRead", "Exception during Read phase", ex);
                throw;
            }

            // Write process logs
            //LogETLProcess.WriteAll(logs);
        }
        catch (Exception ex)
        {
            var ts = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone);
            //LogETLException.Error(ts, $"{cts}FactTicketOverall", "Unhandled exception in FactTicket() overall", ex);
            throw;
        }

        //New table last jalan bila dan masa jalan tu ada error tak
        //Obj console app proses n see if success or has any error
    }

    static string GetXmlRootElementName<T>()
    {
        return GetXmlRootElementName(typeof(T));
    }

    static string GetXmlRootElementName(object obj)
    {
        return GetXmlRootElementName(obj.GetType());
    }

    static string GetXmlRootElementName(Type type)
    {
        var attributes = type.GetCustomAttributes(false);
        string elementName = string.Empty;
        foreach (object attribute in attributes)
        {
            XmlRootAttribute xmlRootAttribute = attribute as XmlRootAttribute;
            if (xmlRootAttribute != null)
            {
                elementName = xmlRootAttribute.ElementName;
                break;
            }
        }
        return elementName;
    }

    static async Task<T> WebServicePostAsync<T>(Uri requestUri, string soapAction, string xmlns, object requestContent) where T : class
    {
        XmlAttributeOverrides requestXmlAttributeOverrides = new XmlAttributeOverrides();

        // Envelope.
        XmlRequestEnvelope envelope = new XmlRequestEnvelope();

        // Body.
        envelope.Body.Element = requestContent;

        // Override body element name to follow content class root name.
        XmlAttributes requestBodyXmlAttributes = new XmlAttributes();
        requestBodyXmlAttributes.XmlElements.Add(new XmlElementAttribute(GetXmlRootElementName(requestContent), requestContent.GetType()) { Namespace = "http://tos.org/" }); // Empty namespace
        requestXmlAttributeOverrides.Add(typeof(XmlRequestEnvelope.Body_), nameof(XmlRequestEnvelope.Body_.Element), requestBodyXmlAttributes);

        // Serialize.
        XmlWriterSettings settings = new XmlWriterSettings();
        settings.OmitXmlDeclaration = true;

        // Define the namespaces
        XmlSerializerNamespaces nss = new XmlSerializerNamespaces();
        nss.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
        nss.Add("xsd", "http://www.w3.org/2001/XMLSchema");
        nss.Add("soap", "http://schemas.xmlsoap.org/soap/envelope/");

        string contentString;
        using (StringWriter stringWriter = new StringWriter())
        using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter, settings))
        {
            XmlSerializer requestSerializer = new XmlSerializer(envelope.GetType(), requestXmlAttributeOverrides);
            requestSerializer.Serialize(xmlWriter, envelope, nss);
            contentString = stringWriter.ToString();
        }

        HttpContent httpContent = new StringContent(contentString, Encoding.UTF8, "text/xml");

        // Post.
        DateTime beforePost = DateTime.Now, afterPost = beforePost;
        HttpResponseMessage? response = null;

        // Use an HttpClient instance. If you have an IHttpClientFactory available in your app,
        // prefer that (inject and use it). For a quick fix here we create a new HttpClient.
        using (var httpClient = new HttpClient())
        {
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));
            httpClient.DefaultRequestHeaders.Add("SOAPAction", soapAction);

            try
            {
                beforePost = DateTime.Now;
                response = await httpClient.PostAsync(requestUri, httpContent).ConfigureAwait(false);
                afterPost = DateTime.Now;
            }
            catch (Exception ex)
            {
                afterPost = DateTime.Now;
                //Plugin.Logging.LogIntegrationInventory.Information(logInformation.BatchId, logInformation.JobId, logInformation.VendorCode, logInformation.OperatorCode, logInformation.VendorAccountCode,
                //    logInformation.Type, requestUri.ToString(), string.Empty, contentString, "text/xml", string.Empty, string.Empty,
                //    beforePost, afterPost, (float)(afterPost - beforePost).TotalMilliseconds, ex.Message, logInformation.BookingId, logInformation.BookingOperatorId);
                throw;
            }
        }

        if (response == null)
            throw new InvalidOperationException("No HTTP response received.");

        // Check response.
        string responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        //Plugin.Logging.LogIntegrationInventory.Information(logInformation.BatchId, logInformation.JobId, logInformation.VendorCode, logInformation.OperatorCode, logInformation.VendorAccountCode,
        //    logInformation.Type, requestUri.ToString(), ((int)response.StatusCode).ToString(), contentString, "text/xml", responseString, "text/xml",
        //    beforePost, afterPost, (float)(afterPost - beforePost).TotalMilliseconds, logInformation.MessageTemplate, logInformation.BookingId, logInformation.BookingOperatorId);

        response.EnsureSuccessStatusCode();

        if (string.IsNullOrWhiteSpace(responseString))
            throw new XmlException("Response content is empty.");

        // Parse XML.
        XDocument xDocument = XDocument.Parse(responseString);
        if (xDocument == null)
            throw new XmlException("XDocument is null.");

        // Check for SOAP Fault
        XNamespace soapNs = "http://schemas.xmlsoap.org/soap/envelope/";
        var faultElement = xDocument.Descendants(soapNs + "Fault").FirstOrDefault();
        if (faultElement != null)
        {
            string faultCode = faultElement.Element("faultcode")?.Value;
            string faultString = faultElement.Element("faultstring")?.Value;
            throw new InvalidOperationException($"SOAP Fault: {faultCode} - {faultString}");
        }

        // Get result.
        bool isResponseElementXmlns = false;
        string responseElementName = GetXmlRootElementName<T>();
        IEnumerable<XElement> responseElements = xDocument.Descendants(responseElementName);
        if (!responseElements.Any())
        {
            responseElements = xDocument.Descendants(XNamespace.Get(xmlns) + responseElementName);
            isResponseElementXmlns = true;
        }

        XElement? responseElement = responseElements.FirstOrDefault();
        if (responseElement == null)
            throw new XmlException("Element " + responseElementName + " not found.");

        // Deserialize result to object.
        T? responseContent;
        try
        {
            XmlSerializer responseSerializer;
            if (!isResponseElementXmlns)
            {
                responseSerializer = new XmlSerializer(typeof(T));
            }
            else
            {
                responseSerializer = new XmlSerializer(typeof(T), xmlns);
            }
            responseContent = responseSerializer.Deserialize(responseElement.CreateReader()) as T;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to deserialize response to {typeof(T).Name}.", ex);
        }

        return responseContent!;
    }
}
