using System;
using System.Threading.Tasks;

namespace WeBirr.Example.Examples
{
    internal static class Example2PaymentStatusSinglePoll
    {
        public static async Task RunAsync()
        {
            if (!ExampleSupport.HasTestEnvCredentials())
            {
                ExampleSupport.PrintMissingCredentials();
                return;
            }

            var api = ExampleSupport.CreateTestEnvClient();
            var paymentCode = ExampleSupport.Env("WEBIRR_TEST_PAYMENT_CODE", "032 822 352");

            Console.WriteLine("Getting Payment Status...");
            var status = await api.GetPaymentStatusAsync(paymentCode);
            if (ExampleSupport.HasError(status))
            {
                ExampleSupport.PrintError(status);
                return;
            }

            if (status.res?.IsPaid ?? false)
            {
                Console.WriteLine("Bill is paid.");
                Console.WriteLine($"Bank: {status.res.data?.bankID}");
                Console.WriteLine($"Bank Reference Number: {status.res.data?.paymentReference}");
                Console.WriteLine($"Amount Paid: {status.res.data?.amount}");
                Console.WriteLine($"Payment Date: {status.res.data?.paymentDate}");
            }
            else
            {
                Console.WriteLine("Bill is pending payment.");
            }
        }
    }
}
