using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PushMaster.State
{
    [XmlRoot(ElementName = "stateInsert", Namespace = "http://tos.org/")]
    public class StateRequestModel
    {
        [XmlElement(ElementName = "state_code")]
        public string StateCode { get; set; }

        [XmlElement(ElementName = "state_name")]
        public string StateName { get; set; }
    }
}
