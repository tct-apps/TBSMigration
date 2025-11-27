using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PushMaster.Route
{
    public class RouteModel
    {
        public string OperatorCode { get; set; }
        public string RouteNo { get; set; }
        public string RouteName { get; set; }
        public string OriginCity { get; set; }
        public string DestinationCity { get; set; }
        public List<RouteDetailModel> RouteDetails { get; set; }
    }

    public class RouteDetailModel
    {
        public string OperatorCode { get; set; }
        public string RouteNo { get; set; }
        public string Display { get; set; }
        public string ViaCity { get; set; }
        public int StageNo { get; set; }
    }
}
