using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace XMRemoteMonitor.Models
{
    public class ResponseAJAX
    {
        public int categoryid;
        public string method;
        public bool issuccess;
        public string message;
        public bool debug;
        public List<string> xdata;
    }
}
