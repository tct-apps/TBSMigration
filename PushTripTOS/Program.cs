using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PushMasterTOS.Model;
using PushMasterTOS.Model.BusOperator;
using PushMasterTOS.Model.City;
using PushMasterTOS.Model.Common;
using PushMasterTOS.Model.Route;
using PushMasterTOS.Model.Vehicle;
using PushMasterTOS.Model.State;
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

    static async Task State(string sourceConn)
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
                // Prepare connection.
                #region Get value 
                string url = "http://10.238.1.4/toswebservice_Test/toswebservice.asmx";
                string SoapAction = "http://tos.org";
                string xmlns = "http://tos.org/";
                #endregion

                if (string.IsNullOrEmpty(url))
                {
                    throw new FurtherActionRequiredException(string.Format(ErrorMessage.MissingIntegrationInfo,"ApiUrl"));
                }
                Uri requestUrl = new Uri(url);
                string soapAction = SoapAction + ApiUrlKey.StateInsert;

                stateList = multi.Read<StateModel>().ToList();

                foreach (var state in stateList)
                {
                    StateRequestModel requestContent = new StateRequestModel()
                    {
                        StateCode = state.StateCode,
                        StateName = state.StateName
                    };

                    // Call API
                    StateResponseModel response = await WebServicePostAsync<StateResponseModel>(requestUrl, soapAction, xmlns, requestContent);
                }
                

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

    static async void City(string sourceConn)
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
                // Prepare connection.
                #region Get value 
                string url = "http://10.238.1.4/toswebservice_Test/toswebservice.asmx";
                string SoapAction = "http://tos.org";
                string xmlns = "http://tos.org/";
                #endregion

                if (string.IsNullOrEmpty(url))
                {
                    throw new FurtherActionRequiredException(string.Format(ErrorMessage.MissingIntegrationInfo, "ApiUrl"));
                }
                Uri requestUrl = new Uri(url);
                string soapAction = SoapAction + ApiUrlKey.CityInsert;

                cityList = multi.Read<CityModel>().ToList();

                foreach (var city in cityList)
                {
                    CityRequestModel requestContent = new CityRequestModel
                    {
                        CityCode = city.CityCode,
                        CityName = city.CityName,
                        StateCode = city.StateCode
                    };

                    // Call API
                    CityResponseModel response = await WebServicePostAsync<CityResponseModel>(requestUrl, soapAction, xmlns, requestContent);

                }

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

    static async void BusOperator(string sourceConn)
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
                // Prepare connection.
                #region Get value 
                string url = "http://10.238.1.4/toswebservice_Test/toswebservice.asmx";
                string SoapAction = "http://tos.org";
                string xmlns = "http://tos.org/";
                #endregion

                if (string.IsNullOrEmpty(url))
                {
                    throw new FurtherActionRequiredException(string.Format(ErrorMessage.MissingIntegrationInfo, "ApiUrl"));
                }
                Uri requestUrl = new Uri(url);
                string soapAction = SoapAction + ApiUrlKey.BusOperatorInsert;

                busOperatorList = multi.Read<BusOperatorModel>().ToList();

                foreach (var bo in busOperatorList)
                {
                    if (bo.OperatorLogo != null)
                    {
                        bo.HexLogo = BitConverter.ToString(bo.OperatorLogo).Replace("-", "");
                    }

                    BusOperatorRequestModel requestContent = new BusOperatorRequestModel
                    {
                        OperatorCode = bo.OperatorCode,
                        OperatorName = bo.OperatorName,
                        OperatorLogo = bo.HexLogo,
                        ContactPerson = bo.ContactPerson,
                        Address1 = bo.Address1,
                        Address2 = bo.Address2,
                        Address3 = bo.Address3,
                        ContactNumber1 = bo.ContactNumber1,
                        ContactNumber2 = bo.ContactNumber2,
                        FaxNumber = bo.FaxNumber,
                        EmailId = bo.EmailId,
                        Website = bo.Website,
                        Description = bo.Description,
                        RegisterNo = bo.RegisterNo,
                    };

                    // Call API
                    BusOperatorResponseModel response = await WebServicePostAsync<BusOperatorResponseModel>(requestUrl, soapAction, xmlns, requestContent);
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

    static async void Vehicle(string sourceConn)
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
                // Prepare connection.
                #region Get value 
                string url = "http://10.238.1.4/toswebservice_Test/toswebservice.asmx";
                string SoapAction = "http://tos.org";
                string xmlns = "http://tos.org/";
                #endregion

                if (string.IsNullOrEmpty(url))
                {
                    throw new FurtherActionRequiredException(string.Format(ErrorMessage.MissingIntegrationInfo, "ApiUrl"));
                }
                Uri requestUrl = new Uri(url);
                string soapAction = SoapAction + ApiUrlKey.VehicleInsert;

                vehicleList = multi.Read<VehicleModel>().ToList();

                foreach (var vehicle in vehicleList)
                {
                    VehicleRequestModel requestContent = new VehicleRequestModel
                    {
                        PlateNo = vehicle.PlateNo,
                        OperatorCode = vehicle.OperatorCode
                    };

                    // Call API
                    VehicleResponseModel response = await WebServicePostAsync<VehicleResponseModel>(requestUrl, soapAction, xmlns, requestContent);

                }

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

    static async void Route(string sourceConn)
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
                // Prepare connection.
                #region Get value 
                string url = "http://10.238.1.4/toswebservice_Test/toswebservice.asmx";
                string SoapAction = "http://tos.org";
                string xmlns = "http://tos.org/";
                #endregion

                if (string.IsNullOrEmpty(url))
                {
                    throw new FurtherActionRequiredException(string.Format(ErrorMessage.MissingIntegrationInfo, "ApiUrl"));
                }
                Uri requestUrl = new Uri(url);
                string soapAction = SoapAction + ApiUrlKey.RouteInsert;

                routeList = multi.Read<RouteModel>().ToList();
                routeDetailList = multi.Read<RouteDetailModel>().ToList();

                foreach (var route in routeList)
                {
                    route.RouteDetails = routeDetailList
                        .Where(d => d.RouteNo == route.RouteNo)
                        .ToList();

                    RouteRequestModel requestContent = new RouteRequestModel
                    {
                        OperatorCode = route.OperatorCode,
                        RouteNo = route.RouteNo,
                        RouteName = route.RouteName,
                        OriginCity = route.OriginCity,
                        DestinationCity = route.DestinationCity,
                        RouteDetails = route.RouteDetails.Select(d => new RouteDetail
                        {
                            OperatorCode = d.OperatorCode,
                            RouteNo = d.RouteNo,
                            Display = d.Display,
                            ViaCity = d.ViaCity,
                            StageNo = d.StageNo
                        }).ToList()
                    };

                    // Call API
                    RouteResponseModel response = await WebServicePostAsync<RouteResponseModel>(requestUrl, soapAction, xmlns, requestContent);
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
