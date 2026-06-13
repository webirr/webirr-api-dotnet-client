using System.Text.Json.Serialization;

namespace WeBirr
{
    public class Stat
    {
        [JsonPropertyName("NBills")]
        public int nBills { get; set; }

        [JsonPropertyName("NBillsPaid")]
        public int nBillsPaid { get; set; }

        [JsonPropertyName("NBillsUnpaid")]
        public int nBillsUnpaid { get; set; }

        [JsonPropertyName("AmountBills")]
        public string amountBills { get; set; }

        [JsonPropertyName("AmountPaid")]
        public string amountPaid { get; set; }

        [JsonPropertyName("AmountUnpaid")]
        public string amountUnpaid { get; set; }
    }
}
