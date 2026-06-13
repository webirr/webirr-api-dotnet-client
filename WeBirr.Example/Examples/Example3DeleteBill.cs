using System;
using System.Threading.Tasks;

namespace WeBirr.Example.Examples
{
    internal static class Example3DeleteBill
    {
        public static async Task RunAsync()
        {
            if (!ExampleSupport.HasTestEnvCredentials())
            {
                ExampleSupport.PrintMissingCredentials();
                return;
            }

            var api = ExampleSupport.CreateTestEnvClient();
            var paymentCode = ExampleSupport.Env("WEBIRR_TEST_PAYMENT_CODE", "379 262 100");

            Console.WriteLine("Deleting Bill...");
            var delete = await api.DeleteBillAsync(paymentCode);
            if (ExampleSupport.HasError(delete))
            {
                ExampleSupport.PrintError(delete);
                return;
            }

            Console.WriteLine("Bill is deleted successfully.");
        }
    }
}
