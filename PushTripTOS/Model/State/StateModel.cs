using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PushMasterTOS.Model
{
    [XmlRoot("stateInsert")]
    public class StateModel
    {
       public string state_code { get; set; }
       public string state_name { get; set; }
    }
}
