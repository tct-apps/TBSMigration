using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PushTrip.AdhocSchedule
{
    public class AdhocScheduleResponseModel
    {
        [XmlElement(ElementName = "adhocScheduleInsertResponse", Namespace = "http://tos.org/")]
        public AdhocScheduleInsertResponse AdhocScheduleInsertResponse { get; set; }
    }

    public class AdhocScheduleInsertResponse
    {
        [XmlElement(ElementName = "adhocScheduleInsertResult", Namespace = "http://tos.org/")]
        public AdhocScheduleInsertResult AdhocScheduleInsertResult { get; set; }
    }

    public class AdhocScheduleInsertResult
    {
        [XmlElement(ElementName = "insert_status", Namespace = "http://tos.org/")]
        public InsertStatus InsertStatus { get; set; }
    }

    public class InsertStatus
    {
        [XmlElement(ElementName = "adhoc", Namespace = "http://tos.org/")]
        public List<AdhocResponseItem> AdhocList { get; set; }
    }

    public class AdhocResponseItem
    {
        [XmlAttribute(AttributeName = "operator_code")]
        public string OperatorCode { get; set; }

        [XmlAttribute(AttributeName = "route_no")]
        public string RouteNo { get; set; }

        [XmlAttribute(AttributeName = "trip_no")]
        public string TripNo { get; set; }

        [XmlAttribute(AttributeName = "code")]
        public string Code { get; set; }  // e.g. SUCCESS / FAILED

        [XmlAttribute(AttributeName = "msg")]
        public string Message { get; set; }

        [XmlAttribute(AttributeName = "scheduleid")]
        public long ScheduleId { get; set; }

        [XmlAttribute(AttributeName = "position")]
        public int Position { get; set; }

        [XmlAttribute(AttributeName = "trip_date")]
        public string TripDate { get; set; }

        [XmlAttribute(AttributeName = "bay")]
        public string Bay { get; set; }

        [XmlAttribute(AttributeName = "gate")]
        public string Gate { get; set; }
    }

}
