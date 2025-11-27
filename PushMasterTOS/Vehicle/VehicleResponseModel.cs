using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PushMaster.Vehicle
{
    [XmlRoot(ElementName = "vehicleInsertResponse", Namespace = "http://tos.org/")]
    public class VehicleResponseModel
{
        [XmlElement(ElementName = "vehicleInsertResult")]
        public VehicleInsertResult Result { get; set; }
    }

    public class VehicleInsertResult
    {
        [XmlElement(ElementName = "code")]
        public string Code { get; set; }

        [XmlElement(ElementName = "msg")]
        public string Message { get; set; }
    }
}
