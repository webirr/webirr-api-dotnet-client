using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WeBirr.Example.Examples
{
    internal sealed class Example4PaymentStatusBulkPoll
    {
        readonly WeBirrClient _api;
        string _lastTimeStamp;

        public Example4PaymentStatusBulkPoll()
        {
            _api = ExampleSupport.CreateTestEnvClient();
            _lastTimeStamp = ExampleSupport.ExampleCursorWithTime;
        }

        public static async Task RunAsync()
        {
            if (!ExampleSupport.HasTestEnvCredentials())
            {
                ExampleSupport.PrintMissingCredentials();
                return;
            }

            await new Example4PaymentStatusBulkPoll().FetchAndProcessPaymentsAsync();
        }

        public async Task FetchAndProcessPaymentsAsync()
        {
            const int limit = 100;
            Console.WriteLine("Retrieving Payments...");
            var response = await _api.GetPaymentsAsync(_lastTimeStamp, limit);

            if (ExampleSupport.HasError(response))
            {
                ExampleSupport.PrintError(response);
                return;
            }

            var payments = response.res ?? new List<PaymentResponse>();
            if (payments.Count == 0)
            {
                Console.WriteLine("No new payments found.");
                return;
            }

            foreach (var payment in payments)
            {
                ProcessPayment(payment);
                Console.WriteLine("-----------------------------");
            }

            _lastTimeStamp = payments[payments.Count - 1].updateTimeStamp;
            Console.WriteLine($"Last Timestamp: {_lastTimeStamp}");
        }

        static void ProcessPayment(PaymentResponse payment)
        {
            ExampleSupport.PrintPayment(payment);
        }
    }
}
