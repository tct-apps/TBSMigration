using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PushTrip.AdhocSchedule
{
    [XmlRoot(ElementName = "adhocScheduleInsert", Namespace = "http://tos.org/")]
    public class AdhocScheduleRequestModel
    {
        [XmlElement(ElementName = "insert_list", Namespace = "http://tos.org/")]
        public InsertList InsertList { get; set; }
    }

    public class InsertList
    {
        [XmlElement(ElementName = "schedule", Namespace = "http://tos.org/")]
        public Schedule Schedule { get; set; }
    }

    public class Schedule
    {
        [XmlElement(ElementName = "adhoc", Namespace = "http://tos.org/")]
        public List<Adhoc> AdhocList { get; set; }
    }

    public class Adhoc
    {
        [XmlAttribute(AttributeName = "operator_code")]
        public string OperatorCode { get; set; }

        [XmlAttribute(AttributeName = "route_no")]
        public string RouteNo { get; set; }

        [XmlAttribute(AttributeName = "trip_no")]
        public string TripNo { get; set; }

        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; } // DEP / ARR

        [XmlAttribute(AttributeName = "date")]
        public string Date { get; set; } // format: yyyy-MM-dd

        [XmlAttribute(AttributeName = "time")]
        public string Time { get; set; } // format: HH:mm

        [XmlAttribute(AttributeName = "plate_no")]
        public string PlateNo { get; set; }

        [XmlAttribute(AttributeName = "remark")]
        public string Remark { get; set; }

        [XmlAttribute(AttributeName = "position")]
        public int Position { get; set; }

        [XmlAttribute(AttributeName = "trip_date")]
        public string TripDate { get; set; } // format: yyyy-MM-dd
    }

}
