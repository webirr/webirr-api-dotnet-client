using System;
using System.Threading.Tasks;

namespace WeBirr.Example.Examples
{
    internal static class Example8SupportedBanks
    {
        public static async Task RunAsync()
        {
            if (!ExampleSupport.HasTestEnvCredentials())
            {
                ExampleSupport.PrintMissingCredentials();
                return;
            }

            var api = ExampleSupport.CreateTestEnvClient();

            Console.WriteLine("Getting supported banks...");
            var response = await api.GetSupportedBanksAsync();

            if (ExampleSupport.HasError(response))
            {
                ExampleSupport.PrintError(response);
                return;
            }

            foreach (var bank in response.res)
            {
                Console.WriteLine($"{bank.bankID} - {bank.name}");
            }

            Console.WriteLine("Use only these merchant-specific banks when showing checkout payment instructions.");
        }
    }
}
