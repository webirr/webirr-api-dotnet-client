using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WeBirr.Example.Examples
{
    internal static class Example6PaymentStatusWebhook
    {
        static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public static WebhookResult HandleRequest(string method, string providedAuthKey, string expectedAuthKey, string rawPayload)
        {
            if (!string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase))
            {
                return WebhookResult.Json(405, @"{""error"":""Method Not Allowed. POST required.""}");
            }

            if (!IsAuthenticated(providedAuthKey, expectedAuthKey))
            {
                return WebhookResult.Json(403, @"{""error"":""Unauthorized access. Invalid authKey.""}");
            }

            if (string.IsNullOrWhiteSpace(rawPayload))
            {
                return WebhookResult.Json(400, @"{""error"":""Empty request body.""}");
            }

            WebhookPayload payload;
            try
            {
                payload = JsonSerializer.Deserialize<WebhookPayload>(rawPayload, JsonOptions);
            }
            catch (JsonException)
            {
                return WebhookResult.Json(400, @"{""error"":""Invalid JSON format.""}");
            }

            if (payload?.data == null)
            {
                return WebhookResult.Json(400, @"{""error"":""Invalid payment data.""}");
            }

            ProcessPayment(payload.data);
            return WebhookResult.Json(200, @"{""success"":true,""message"":""Payment received and queued for processing""}");
        }

        static bool IsAuthenticated(string providedAuthKey, string expectedAuthKey)
        {
            if (string.IsNullOrEmpty(providedAuthKey) || string.IsNullOrEmpty(expectedAuthKey))
            {
                return false;
            }

            var provided = Encoding.UTF8.GetBytes(providedAuthKey);
            var expected = Encoding.UTF8.GetBytes(expectedAuthKey);
            return provided.Length == expected.Length && CryptographicOperations.FixedTimeEquals(provided, expected);
        }

        static void ProcessPayment(PaymentResponse payment)
        {
            ExampleSupport.PrintPayment(payment);
        }

        sealed class WebhookPayload
        {
            public PaymentResponse data { get; set; }
        }
    }

    internal sealed class WebhookResult
    {
        public int StatusCode { get; private set; }
        public string ContentType { get; private set; }
        public string Body { get; private set; }

        public static WebhookResult Json(int statusCode, string body)
        {
            return new WebhookResult
            {
                StatusCode = statusCode,
                ContentType = "application/json",
                Body = body
            };
        }
    }
}
