using System;
using System.Threading.Tasks;

namespace WeBirr.Example.Examples
{
    internal static class Example5StatReport
    {
        public static async Task RunAsync()
        {
            if (!ExampleSupport.HasTestEnvCredentials())
            {
                ExampleSupport.PrintMissingCredentials();
                return;
            }

            var api = ExampleSupport.CreateTestEnvClient();
            var dateFrom = "2025-01-01";
            var dateTo = "2030-01-31";

            Console.WriteLine("Retrieving Statistics...");
            Console.WriteLine($"Date From: {dateFrom}");
            Console.WriteLine($"Date To: {dateTo}");

            var response = await api.GetStatAsync(dateFrom, dateTo);
            if (ExampleSupport.HasError(response))
            {
                ExampleSupport.PrintError(response);
                return;
            }

            var stat = response.res;
            Console.WriteLine($"Number of Bills Created: {stat.nBills}");
            Console.WriteLine($"Number of Paid Bills: {stat.nBillsPaid}");
            Console.WriteLine($"Number of Unpaid Bills: {stat.nBillsUnpaid}");
            Console.WriteLine($"Amount of Bills: {stat.amountBills}");
            Console.WriteLine($"Amount Paid: {stat.amountPaid}");
            Console.WriteLine($"Amount Unpaid: {stat.amountUnpaid}");
        }
    }
}
