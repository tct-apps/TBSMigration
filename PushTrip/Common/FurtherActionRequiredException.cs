using System;
using System.Collections.Generic;
using System.Text;

namespace PushTrip.Common
{
    public class FurtherActionRequiredException : System.Exception
    {
        public string Type { get; set; }
        public string Code { get; set; }
        public FurtherActionRequiredException()
        {
        }

        public FurtherActionRequiredException(string message) : base(message)
        {
        }

        public FurtherActionRequiredException(string message, string action, string code, params object?[] args) : base(formatMessage(message, code))
        {
            this.Type = action;
            this.Code = code;
        }



        private static string formatMessage(string message, string code)
        {
            return string.Format(message, code); ;
        }
    }
}
