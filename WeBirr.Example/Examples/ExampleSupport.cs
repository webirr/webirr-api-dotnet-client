using System;
using System.Collections.Generic;

namespace WeBirr.Example.Examples
{
    internal static class ExampleSupport
    {
        public const string ExampleCursor = "20251231";
        public const string ExampleCursorWithTime = "20251231235959";

        public static WeBirrClient CreateTestEnvClient()
        {
            return new WeBirrClient(MerchantId(), ApiKey(), true);
        }

        public static bool HasTestEnvCredentials()
        {
            return !string.IsNullOrEmpty(MerchantId()) && !string.IsNullOrEmpty(ApiKey());
        }

        public static void PrintMissingCredentials()
        {
            Console.WriteLine("Set WEBIRR_TEST_ENV_MERCHANT_ID and WEBIRR_TEST_ENV_API_KEY to run this example.");
        }

        public static string MerchantId()
        {
            return Environment.GetEnvironmentVariable("WEBIRR_TEST_ENV_MERCHANT_ID") ?? "";
        }

        public static string ApiKey()
        {
            return Environment.GetEnvironmentVariable("WEBIRR_TEST_ENV_API_KEY") ?? "";
        }

        public static string Env(string name, string fallback)
        {
            var value = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrEmpty(value) ? fallback : value;
        }

        public static Bill NewBill(string billReference)
        {
            return new Bill
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

        public static bool HasError<T>(ApiResponse<T> response) where T : class
        {
            return response == null || !string.IsNullOrEmpty(response.error) || !string.IsNullOrEmpty(response.errorCode);
        }

        public static void PrintError<T>(ApiResponse<T> response) where T : class
        {
            Console.WriteLine($"error: {response?.error}");
            Console.WriteLine($"errorCode: {response?.errorCode}");
        }

        public static void PrintPayment(PaymentResponse payment)
        {
            Console.WriteLine($"Payment Status: {payment.status}");
            if (payment.IsPaid)
            {
                Console.WriteLine("Payment Status Text: Paid.");
            }
            if (payment.IsReversed)
            {
                Console.WriteLine("Payment Status Text: Reversed.");
            }
            Console.WriteLine($"Bank: {payment.bankID}");
            Console.WriteLine($"Bank Reference Number: {payment.paymentReference}");
            Console.WriteLine($"Amount Paid: {payment.amount}");
            Console.WriteLine($"Payment Date: {payment.paymentDate}");
            Console.WriteLine($"Reversal/Cancel Date: {payment.canceledTime}");
            Console.WriteLine($"Update Timestamp: {payment.updateTimeStamp}");
        }
    }
}
