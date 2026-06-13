using System;
using System.Threading.Tasks;

namespace WeBirr.Example.Examples
{
    internal static class Example7GetBillAndListBills
    {
        public static async Task RunAsync()
        {
            if (!ExampleSupport.HasTestEnvCredentials())
            {
                ExampleSupport.PrintMissingCredentials();
                return;
            }

            var api = ExampleSupport.CreateTestEnvClient();
            var billReference = ExampleSupport.Env("WEBIRR_TEST_BILL_REFERENCE", "YOUR_BILL_REFERENCE");
            var paymentCode = ExampleSupport.Env("WEBIRR_TEST_PAYMENT_CODE", "YOUR_PAYMENT_CODE");

            Console.WriteLine("Getting bill by reference...");
            var byReference = await api.GetBillByReferenceAsync(billReference);
            if (ExampleSupport.HasError(byReference))
            {
                ExampleSupport.PrintError(byReference);
            }
            else
            {
                Console.WriteLine($"Bill found by reference: {byReference.res.billReference}");
            }

            Console.WriteLine("Getting bill by payment code...");
            var byPaymentCode = await api.GetBillByPaymentCodeAsync(paymentCode);
            if (ExampleSupport.HasError(byPaymentCode))
            {
                ExampleSupport.PrintError(byPaymentCode);
            }
            else
            {
                Console.WriteLine($"Bill found by payment code: {byPaymentCode.res.wbcCode}");
            }

            Console.WriteLine("Listing bills...");
            var bills = await api.GetBillsAsync(paymentStatus: -1, lastTimeStamp: ExampleSupport.ExampleCursor, limit: 10);
            if (ExampleSupport.HasError(bills))
            {
                ExampleSupport.PrintError(bills);
                return;
            }

            Console.WriteLine($"Bills returned: {bills.res.Count}");
            foreach (var bill in bills.res)
            {
                Console.WriteLine("-----------------------------");
                Console.WriteLine($"{bill.billReference}: {bill.amount} / {bill.paymentStatus}");
            }
        }
    }
}
