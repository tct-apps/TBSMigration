using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PushMaster.City
{
    [XmlRoot(ElementName = "cityInsert", Namespace = "http://tos.org/")]
    public class CityRequestModel
    {
        [XmlElement(ElementName = "city_code")]
        public string CityCode { get; set; }

        [XmlElement(ElementName = "city_name")]
        public string CityName { get; set; }

        [XmlElement(ElementName = "state_code")]
        public string StateCode { get; set; }
    }
}
