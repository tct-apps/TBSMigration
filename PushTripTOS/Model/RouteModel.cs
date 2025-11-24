using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PushTripTOS.Model
{
    public class RouteModel
    {
        public string operator_code { get; set; }
        public string route_no { get; set; }
        public string route_name { get; set; }
        public string origin_city { get; set; }
        public string destination_city { get; set; }
        public List<RouteDetailModel> route_details { get; set; }
    }

    public class RouteDetailModel
    {
        public string operator_code { get; set; }
        public string route_no { get; set; }
        public string display { get; set; }
        public string via_city { get; set; }
        public int stage_no { get; set; }
    }
}
