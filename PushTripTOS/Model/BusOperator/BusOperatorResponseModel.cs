using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PushMasterTOS.Model.BusOperator
{
    [XmlRoot(ElementName = "busOperatorInsertResponse", Namespace = "http://tos.org/")]
    public class BusOperatorResponseModel
    {
        [XmlElement(ElementName = "busOperatorInsertResult")]
        public BusOperatorInsertResult Result { get; set; }
    }

    public class BusOperatorInsertResult
    {
        [XmlElement(ElementName = "code")]
        public string Code { get; set; }

        [XmlElement(ElementName = "msg")]
        public string Message { get; set; }
    }

}
