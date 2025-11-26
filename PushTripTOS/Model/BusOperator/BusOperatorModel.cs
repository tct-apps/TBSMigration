using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PushMasterTOS.Model.BusOperator
{
    public class BusOperatorModel
    {
        public string OperatorCode { get; set; }
        public string OperatorName { get; set; }
        public byte[] OperatorLogo { get; set; }
        public string ContactPerson { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string ContactNumber1 { get; set; }
        public string ContactNumber2 { get; set; }
        public string FaxNumber { get; set; }
        public string EmailId { get; set; }
        public string Website { get; set; }
        public string Description { get; set; }
        public string RegisterNo { get; set; }
        public string HexLogo { get; set; }
    }
}
