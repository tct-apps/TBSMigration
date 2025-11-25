using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PushMasterTOS.Model.Vehicle
{
    [XmlRoot(ElementName = "vehicleInsert", Namespace = "http://tos.org/")]
    public class VehicleRequestModel
{
        [XmlElement(ElementName = "plate_no")]
        public string PlateNo { get; set; }

        [XmlElement(ElementName = "operator_code")]
        public string OperatorCode { get; set; }
    }
}
