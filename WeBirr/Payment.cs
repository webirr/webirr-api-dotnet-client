using System;

namespace WeBirr
{
    /// Payment object returned from getPaymentStatus api call
    public class Payment
    {
        /// 0 = not paid, 1 = payment in progress,  2. paid !
        public int status { get; set; }
        public PaymentDetail data { get; set; }

        /// true if the bill is paid (payment process completed)
        public bool IsPaid => status == 2;
    }

    /// Payment Detail such as Bank Id, Bank Reference Number
    public class PaymentDetail
    {
        public int id { get; set; }
        public int status { get; set; }
        public string paymentReference { get; set; }
        public string paymentDate { get; set; }
        public bool confirmed { get; set; }
        public string confirmedTime { get; set; }
        public string bankID { get; set; }
        [Obsolete("Use paymentDate instead. This legacy alias is kept for backward compatibility.")]
        public string time
        {
            get => paymentDate;
            set => paymentDate = value;
        }
        public string amount { get; set; }
        public string wbcCode { get; set; }
        public string updateTimeStamp { get; set; }
    }

    /// Payment item returned from timestamp-based bulk payment polling.
    public class PaymentResponse
    {
        public int status { get; set; }
        public int id { get; set; }
        public string bankID { get; set; }
        public string paymentReference { get; set; }
        public string paymentDate { get; set; }
        public bool confirmed { get; set; }
        public string confirmedTime { get; set; }
        public bool canceled { get; set; }
        public string canceledTime { get; set; }
        public string amount { get; set; }
        public string wbcCode { get; set; }
        public string updateTimeStamp { get; set; }

        [Obsolete("Use paymentDate instead. This legacy alias is kept for backward compatibility.")]
        public string time
        {
            get => paymentDate;
            set => paymentDate = value;
        }

        public bool IsPaid => status == 2;
        public bool IsReversed => status == 3;
    }
}
