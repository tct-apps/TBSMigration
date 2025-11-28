using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PushTrip.Common
{
    [XmlRoot("Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    public class XmlRequestEnvelope
    {
        public XmlRequestEnvelope()
        {
            this.Body = new Body_();
        }

        [XmlElement("Body")]
        public Body_ Body { get; set; }

        public class Body_
        {
            public object Element { get; set; }
        }
    }
}
