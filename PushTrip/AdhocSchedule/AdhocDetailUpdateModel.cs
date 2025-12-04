using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PushTrip.AdhocSchedule
{
    public class AdhocDetailUpdateModel
    {
        public class Request
        {
            public string TripNo { get; set; }
            public string GateNo { get; set; }
            public string GateNo2 { get; set; }
            public string TripDate { get; set; }
            public long AdhocId { get; set; }
            public int Position { get; set; }
            public string CompanyCode { get; set; }
        }
    }
}
