namespace WeBirr
{
    public class BillResponse : Bill
    {
        public string wbcCode { get; set; }
        public int paymentStatus { get; set; }
        public string updateTimeStamp { get; set; }
    }
}
