using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeBirr.Example
{
    class Program
    {
        static readonly string ApiKey = Environment.GetEnvironmentVariable("WEBIRR_TEST_ENV_API_KEY") ?? "";
        static readonly string MerchantId = Environment.GetEnvironmentVariable("WEBIRR_TEST_ENV_MERCHANT_ID") ?? "";

        static async Task Main(string[] args)
        {
            if (string.IsNullOrEmpty(ApiKey) || string.IsNullOrEmpty(MerchantId))
            {
                Console.WriteLine("Set WEBIRR_TEST_ENV_MERCHANT_ID and WEBIRR_TEST_ENV_API_KEY to run the example.");
                return;
            }

            var api = new WeBirrClient(MerchantId, ApiKey, true);
            var billReference = "dotnet/example/" + Guid.NewGuid();
            string paymentCode = null;

            try
            {
                var bill = NewBill(billReference);

                Console.WriteLine("Creating Bill...");
                var create = await api.CreateBillAsync(bill);
                if (!string.IsNullOrEmpty(create.error))
                {
                    Console.WriteLine($"error: {create.error}");
                    Console.WriteLine($"errorCode: {create.errorCode}");
                    return;
                }

                paymentCode = create.res;
                Console.WriteLine($"Payment Code = {paymentCode}");

                bill.amount = "278.00";
                bill.customerName = "SDK Test Customer Updated";

                Console.WriteLine("Updating Bill...");
                var update = await api.UpdateBillAsync(bill);
                Console.WriteLine(string.IsNullOrEmpty(update.error) ? "bill is updated successfully" : $"error: {update.error}");

                Console.WriteLine("Getting Payment Status...");
                var status = await api.GetPaymentStatusAsync(paymentCode);
                if (status.res?.IsPaid ?? false)
                {
                    Console.WriteLine("bill is paid");
                    Console.WriteLine($"Bank: {status.res.data?.bankID}");
                    Console.WriteLine($"Bank Reference Number: {status.res.data?.paymentReference}");
                    Console.WriteLine($"Amount Paid: {status.res.data?.amount}");
                    Console.WriteLine($"Payment Date: {status.res.data?.paymentDate}");
                }
                else
                {
                    Console.WriteLine("bill is pending payment");
                }

                var billByReference = await api.GetBillByReferenceAsync(billReference);
                Console.WriteLine($"Bill by reference found: {billByReference.res?.billReference}");

                var billByPaymentCode = await api.GetBillByPaymentCodeAsync(paymentCode);
                Console.WriteLine($"Bill by payment code found: {billByPaymentCode.res?.wbcCode}");

                var bills = await api.GetBillsAsync(paymentStatus: -1, lastTimeStamp: "", limit: 10);
                Console.WriteLine($"Bills returned: {bills.res?.Count ?? 0}");

                var payments = await api.GetPaymentsAsync(lastTimeStamp: "", limit: 10);
                foreach (var payment in payments.res ?? new List<PaymentResponse>())
                {
                    Console.WriteLine($"Payment {payment.paymentReference}: {payment.amount} at {payment.paymentDate}");
                    Console.WriteLine($"Next polling cursor candidate: {payment.updateTimeStamp}");
                }

                var stat = await api.GetStatAsync("2025-01-01", "2030-01-31");
                Console.WriteLine($"Bills: {stat.res?.nBills}, Paid: {stat.res?.nBillsPaid}, Amount paid: {stat.res?.amountPaid}");
            }
            finally
            {
                if (!string.IsNullOrEmpty(paymentCode))
                {
                    Console.WriteLine("Deleting Bill...");
                    await api.DeleteBillAsync(paymentCode);
                }
            }
        }

        static Bill NewBill(string billReference) => new Bill
        {
            amount = "270.90",
            customerCode = "cc01",
            customerName = "SDK Test Customer",
            customerPhone = "0911000000",
            time = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm"),
            description = "hotel booking",
            billReference = billReference,
            extras = new Dictionary<string, string>()
        };
    }
}
