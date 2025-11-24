using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PushTripTOS.Model
{
    public class BusOperatorModel
    {
        public string operator_code { get; set; }
        public string operator_name { get; set; }
        public string operator_logo { get; set; }
        public string contact_person { get; set; }
        public string address1 { get; set; }
        public string address2 { get; set; }
        public string address3 { get; set; }
        public string contact_number1 { get; set; }
        public string contact_number2 { get; set; }
        public string fax_number { get; set; }
        public string email_id { get; set; }
        public string website {  get; set; }
        public string description { get; set; }
        public string register_no { get; set; }
    }
}
