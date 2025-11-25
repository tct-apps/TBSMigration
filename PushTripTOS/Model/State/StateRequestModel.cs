using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PushMasterTOS.Model.State
{
    public class StateRequestModel
    {
        [XmlElement(ElementName = "stateInsert", Namespace = "http://tos.org/")]
        public StateInsertRequest StateInsert { get; set; }
    }

    public class StateInsertRequest
    {
        [XmlElement(ElementName = "state_code")]
        public string StateCode { get; set; }

        [XmlElement(ElementName = "state_name")]
        public string StateName { get; set; }
    }
}
