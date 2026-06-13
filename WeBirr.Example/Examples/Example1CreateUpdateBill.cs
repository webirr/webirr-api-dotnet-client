using System;
using System.Threading.Tasks;

namespace WeBirr.Example.Examples
{
    internal static class Example1CreateUpdateBill
    {
        public static async Task RunAsync()
        {
            if (!ExampleSupport.HasTestEnvCredentials())
            {
                ExampleSupport.PrintMissingCredentials();
                return;
            }

            var api = ExampleSupport.CreateTestEnvClient();
            var bill = ExampleSupport.NewBill("dotnet/2021/" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"));

            Console.WriteLine("Creating Bill...");
            var create = await api.CreateBillAsync(bill);
            if (ExampleSupport.HasError(create))
            {
                ExampleSupport.PrintError(create);
                return;
            }

            Console.WriteLine($"Payment Code = {create.res}");

            bill.amount = "278.00";
            bill.customerName = "Elias dotnet";

            Console.WriteLine("Updating Bill...");
            var update = await api.UpdateBillAsync(bill);
            if (ExampleSupport.HasError(update))
            {
                ExampleSupport.PrintError(update);
                return;
            }

            Console.WriteLine("Bill is updated successfully.");
        }
    }
}
