using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WeBirr.Example.Examples;

namespace WeBirr.Example
{
    class Program
    {
        static readonly string ApiKey = Environment.GetEnvironmentVariable("WEBIRR_TEST_ENV_API_KEY") ?? "";
        static readonly string MerchantId = Environment.GetEnvironmentVariable("WEBIRR_TEST_ENV_MERCHANT_ID") ?? "";

        static readonly Dictionary<string, Func<Task>> Commands = new Dictionary<string, Func<Task>>(StringComparer.OrdinalIgnoreCase)
        {
            { "create-update-bill", Example1CreateUpdateBill.RunAsync },
            { "payment-status", Example2PaymentStatusSinglePoll.RunAsync },
            { "delete-bill", Example3DeleteBill.RunAsync },
            { "bulk-payment-polling", Example4PaymentStatusBulkPoll.RunAsync },
            { "stat-report", Example5StatReport.RunAsync },
            { "webhook", RunWebhookExampleAsync },
            { "get-bill-and-list-bills", Example7GetBillAndListBills.RunAsync },
            { "supported-banks", Example8SupportedBanks.RunAsync },
            { "end-to-end", RunEndToEndAsync }
        };

        static async Task Main(string[] args)
        {
            if (args.Length > 0)
            {
                if (Commands.TryGetValue(args[0], out var command))
                {
                    await command();
                    return;
                }

                PrintCommands();
                return;
            }

            await RunEndToEndAsync();
        }

        static void PrintCommands()
        {
            Console.WriteLine("Available examples:");
            foreach (var command in Commands.Keys)
            {
                Console.WriteLine($"  dotnet run -- {command}");
            }
        }

        static Task RunWebhookExampleAsync()
        {
            const string authKey = "please-change-me";
            var payload = @"{
                ""data"": {
                    ""status"": 2,
                    ""id"": 101,
                    ""bankID"": ""test-bank"",
                    ""paymentReference"": ""TX-1"",
                    ""paymentDate"": ""2026-06-12 10:11:12"",
                    ""confirmed"": true,
                    ""confirmedTime"": ""2026-06-12 10:12:12"",
                    ""canceled"": false,
                    ""canceledTime"": ""0001-01-01 00:00:00"",
                    ""amount"": ""278.00"",
                    ""wbcCode"": ""123 456 789"",
                    ""updateTimeStamp"": ""2026061210121200000""
                }
            }";

            var result = Example6PaymentStatusWebhook.HandleRequest("POST", authKey, authKey, payload);
            Console.WriteLine($"Status: {result.StatusCode}");
            Console.WriteLine(result.Body);
            return Task.CompletedTask;
        }

        static async Task RunEndToEndAsync()
        {
            if (string.IsNullOrEmpty(ApiKey) || string.IsNullOrEmpty(MerchantId))
            {
                Console.WriteLine("Set WEBIRR_TEST_ENV_MERCHANT_ID and WEBIRR_TEST_ENV_API_KEY to run the example.");
                return;
            }

            var api = new WeBirrClient(MerchantId, ApiKey, true);
            var billReference = "dotnet/2021/" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
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
                bill.customerName = "Elias dotnet";

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

                var lastTimeStamp = "20251231"; // Date-only cursor; use "20251231235959" when you need time precision.

                var bills = await api.GetBillsAsync(paymentStatus: -1, lastTimeStamp: lastTimeStamp, limit: 10);
                Console.WriteLine($"Bills returned: {bills.res?.Count ?? 0}");

                var payments = await api.GetPaymentsAsync(lastTimeStamp: lastTimeStamp, limit: 10);
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
            customerName = "Elias Haileselassie",
            customerPhone = "0911000000",
            time = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm"),
            description = "hotel booking",
            billReference = billReference,
            extras = new Dictionary<string, string>()
        };
    }
}
