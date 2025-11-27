using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PushMaster.State
{
    [XmlRoot(ElementName = "stateInsertResponse", Namespace = "http://tos.org/")]
    public class StateResponseModel
    {
        [XmlElement(ElementName = "stateInsertResult")]
        public StateInsertResult StateInsertResult { get; set; }
    }

    public class StateInsertResult
    {
        [XmlElement(ElementName = "code")]
        public int Code { get; set; }

        [XmlElement(ElementName = "msg")]
        public string Msg { get; set; }
    }
}
