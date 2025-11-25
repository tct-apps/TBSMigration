using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PushMasterTOS.Model.City
{
    [XmlRoot(ElementName = "cityInsertResponse", Namespace = "http://tos.org/")]
    public class CityResponseModel
    {
        [XmlElement(ElementName = "cityInsertResult")]
        public CityInsertResult Result { get; set; }
    }

    public class CityInsertResult
    {
        [XmlElement(ElementName = "code")]
        public string Code { get; set; }

        [XmlElement(ElementName = "msg")]
        public string Message { get; set; }
    }

}
