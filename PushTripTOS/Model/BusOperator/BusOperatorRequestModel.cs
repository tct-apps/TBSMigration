using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PushMasterTOS.Model.BusOperator
{
    [XmlRoot(ElementName = "busOperatorInsert", Namespace = "http://tos.org/")]
    public class BusOperatorRequestModel
    {
        [XmlElement(ElementName = "operator_code")]
        public string OperatorCode { get; set; }

        [XmlElement(ElementName = "operator_name")]
        public string OperatorName { get; set; }

        [XmlElement(ElementName = "operator_logo")]
        public string OperatorLogo { get; set; }

        [XmlElement(ElementName = "register_no")]
        public string RegisterNo { get; set; }

        [XmlElement(ElementName = "contact_person")]
        public string ContactPerson { get; set; }

        [XmlElement(ElementName = "address1")]
        public string Address1 { get; set; }

        [XmlElement(ElementName = "address2")]
        public string Address2 { get; set; }

        [XmlElement(ElementName = "address3")]
        public string Address3 { get; set; }

        [XmlElement(ElementName = "contact_number1")]
        public string ContactNumber1 { get; set; }

        [XmlElement(ElementName = "contact_number2")]
        public string ContactNumber2 { get; set; }

        [XmlElement(ElementName = "fax_number")]
        public string FaxNumber { get; set; }

        [XmlElement(ElementName = "email_id")]
        public string EmailId { get; set; }

        [XmlElement(ElementName = "website")]
        public string Website { get; set; }

        [XmlElement(ElementName = "description")]
        public string Description { get; set; }
    }

}
