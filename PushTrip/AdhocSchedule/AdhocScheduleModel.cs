using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PushTrip.AdhocSchedule
{
    public class AdhocScheduleModel
    {
        public string OperatorCode { get; set; }
        public string RouteNo { get; set; }
        public string TripNo {  get; set; }
        public string Type { get; set; }
        public DateTime TripDate { get; set; }
        public DateTime Date {  get; set; }
        public string Time { get; set; }
        public string PlateNo { get; set; }
        public int Position { get; set; }
        public string Remark { get; set; }
        public int AdhocArr {  get; set; }
    }
}
