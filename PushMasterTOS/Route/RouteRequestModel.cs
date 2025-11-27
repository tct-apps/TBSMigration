using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PushMaster.Route
{
    [XmlRoot(ElementName = "routeInsert", Namespace = "http://tos.org/")]
    public class RouteRequestModel
    {
        [XmlElement(ElementName = "operator_code")]
        public string OperatorCode { get; set; }

        [XmlElement(ElementName = "route_no")]
        public string RouteNo { get; set; }

        [XmlElement(ElementName = "route_name")]
        public string RouteName { get; set; }

        [XmlElement(ElementName = "origin_city")]
        public string OriginCity { get; set; }

        [XmlElement(ElementName = "destination_city")]
        public string DestinationCity { get; set; }

        [XmlArray(ElementName = "route_details")]
        [XmlArrayItem(ElementName = "details")]
        public List<RouteDetail> RouteDetails { get; set; }
    }

    public class RouteDetail
    {
        [XmlAttribute(AttributeName = "operator_code")]
        public string OperatorCode { get; set; }

        [XmlAttribute(AttributeName = "route_no")]
        public string RouteNo { get; set; }

        [XmlAttribute(AttributeName = "display")]
        public string Display { get; set; }

        [XmlAttribute(AttributeName = "via_city")]
        public string ViaCity { get; set; }

        [XmlAttribute(AttributeName = "stage_no")]
        public int StageNo { get; set; }
    }

}
