using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PushMasterTOS.Model.Route
{
    [XmlRoot(ElementName = "routeInsertResponse", Namespace = "http://tos.org/")]
    public class RouteResponseModel
    {
        [XmlElement(ElementName = "routeInsertResult")]
        public RouteInsertResult Result { get; set; }
    }

    public class RouteInsertResult
    {
        [XmlElement(ElementName = "code")]
        public string Code { get; set; }

        [XmlElement(ElementName = "msg")]
        public string Message { get; set; }
    }

}
