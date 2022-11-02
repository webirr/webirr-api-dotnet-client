using System;
using System.Collections.Generic;

namespace WeBirr
{
    public class Bill
    {
        public string customerCode { get; set; }
        public string customerName { get; set; }
        public String customerPhone { get; set; }
        public string billReference { get; set; }
        public string time { get; set; }
        public string description { get; set; }
        public string amount { get; set; }
        public string merchantID { get; set; }
        public Dictionary<string, string> extras { get; set; } = new Dictionary<string, string>();

    }
}
