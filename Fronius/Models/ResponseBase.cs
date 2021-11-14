using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Models
{
    public abstract class ResponseBase : BindableBase
    {
        private int statusCode;
        public int StatusCode
        {
            get => statusCode;
            set => Set(ref statusCode, value);
        }

        private string reason = string.Empty;
        public string Reason
        {
            get => reason;
            set => Set(ref reason, value);
        }

        private string userMessage = string.Empty;
        public string UserMessage
        {
            get => userMessage;
            set => Set(ref userMessage, value);
        }

        private DateTime timestamp=DateTime.MinValue;
        public DateTime Timestamp
        {
            get => timestamp;
            set => Set(ref timestamp, value);
        }

        public abstract string DisplayName{get;}
        public override string ToString()=>DisplayName;
    }
}
