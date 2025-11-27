using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PushMaster.State
{
    public class StateModel
    {
       public string StateCode { get; set; }
       public string StateName { get; set; }
    }
}
