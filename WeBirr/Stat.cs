using Newtonsoft.Json;

namespace WeBirr
{
    public class Stat
    {
        [JsonProperty("NBills")]
        public int nBills { get; set; }

        [JsonProperty("NBillsPaid")]
        public int nBillsPaid { get; set; }

        [JsonProperty("NBillsUnpaid")]
        public int nBillsUnpaid { get; set; }

        [JsonProperty("AmountBills")]
        public string amountBills { get; set; }

        [JsonProperty("AmountPaid")]
        public string amountPaid { get; set; }

        [JsonProperty("AmountUnpaid")]
        public string amountUnpaid { get; set; }
    }
}
