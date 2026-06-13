Official .NET Client Library for WeBirr Payment Gateway APIs

This Client Library provides convenient access to WeBirr Payment Gateway APIs from .NET/Xamarin Apps.


## Install

run the following command to install webirr client library

With .NET CLI

```bash
$ dotnet add package WeBirr
```

With Package Manager

```powershell
PM>  Install-Package WeBirr
```

## Usage

The library needs to be configured with a *merchant Id* & *API key*. You can get it by contacting [webirr.com](https://webirr.net)

> You can use this library for production or test environments. you will need to set isTestEnv=true for test, and false for production apps when creating objects of class WeBirrClient

Examples assume the WeBirr TestEnv and read credentials from environment variables:

```bash
export WEBIRR_TEST_ENV_MERCHANT_ID="YOUR_TEST_MERCHANT_ID"
export WEBIRR_TEST_ENV_API_KEY="YOUR_TEST_API_KEY"
```

Create the client with merchant ID, API key, and environment once. The client automatically sets `Bill.merchantID` before sending bill create/update requests, so application code and examples should not set `merchantID` on the bill object.

## Example

The examples below keep the original .NET README flow: create the client, call the API, check `error`, handle the success branch, and print `errorCode` on failure.

### Creating a new Bill / Updating an existing Bill on WeBirr Servers

```C#
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WeBirr;

namespace WeBirr.Example
{
    class Program
    {
        static readonly string apiKey = Environment.GetEnvironmentVariable("WEBIRR_TEST_ENV_API_KEY") ?? "";
        static readonly string merchantId = Environment.GetEnvironmentVariable("WEBIRR_TEST_ENV_MERCHANT_ID") ?? "";

        //static readonly string apiKey = "YOUR_API_KEY";
        //static readonly string merchantId = "YOUR_MERCHANT_ID";

        /// Creating a new Bill / Updating an existing Bill on WeBirr Servers
        public static async Task CreateAndUpdateBillAsync()
        {
            var api = new WeBirrClient(merchantId, apiKey, isTestEnv: true);

            var bill = new Bill
            {
                amount = "270.90",
                customerCode = "cc01",  // it can be email address or phone number if you dont have customer code
                customerName = "Elias Haileselassie",
                customerPhone = "0911000000", // optional; used for SMS notification when enabled for the merchant
                time = "2021-07-22 22:14", // your bill time, always in this format
                description = "hotel booking",
                billReference = "dotnet/2021/132",  // your unique reference number
                extras = new Dictionary<string, string>()
            };

            // The SDK sets bill.merchantID from the client merchantId before sending.
            //bill.extras["newfield"] = "new value";  // extras is reserved for future use
            //bill.extras["newcommand"] = "new value";

            Console.WriteLine("Creating Bill...");

            var res = await api.CreateBillAsync(bill);

            if (res.error == null)
            {
                // success
                var paymentCode = res.res ?? ""; // returns paymentcode such as 429 723 975
                Console.WriteLine($"Payment Code = {paymentCode}"); // we may want to save payment code in local db.

            }
            else
            {
                // fail
                Console.WriteLine($"error: {res.error}");
                Console.WriteLine($"errorCode: {res.errorCode}"); // can be used to handle specific busines error such as ERROR_INVLAID_INPUT_DUP_REF
            }

            // update existing bill if it is not paid
            bill.amount = "278.00";
            bill.customerName = "Elias dotnet";
            //bill.billReference = "WE CAN NOT CHANGE THIS";


            Console.WriteLine("Updating Bill...");
            res = await api.UpdateBillAsync(bill);

            if (res.error == null)
            {
                // success
                Console.WriteLine("bill is updated successfully"); //res.res will be 'OK'  no need to check here!
            }
            else
            {
                // fail
                Console.WriteLine($"error: {res.error}");
                Console.WriteLine($"errorCode: {res.errorCode}"); // can be used to handle specific busines error such as ERROR_INVLAID_INPUT
            }

        }
    }
}

```

### Getting a Bill and Listing Bills

```C#
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WeBirr;

namespace WeBirr.Example
{
    class Program
    {
        static readonly string apiKey = Environment.GetEnvironmentVariable("WEBIRR_TEST_ENV_API_KEY") ?? "";
        static readonly string merchantId = Environment.GetEnvironmentVariable("WEBIRR_TEST_ENV_MERCHANT_ID") ?? "";

        /// Getting a Bill and Listing Bills from WeBirr Servers
        public static async Task GetBillAndListBillsAsync()
        {
            var api = new WeBirrClient(merchantId, apiKey, isTestEnv: true);

            var billReference = "dotnet/2021/132"; // BILL_REFERENCE_YOU_SAVED_AFTER_CREATING_A_NEW_BILL
            var paymentCode = "PAYMENT_CODE_YOU_SAVED_AFTER_CREATING_A_NEW_BILL"; // such as '141 263 782'

            Console.WriteLine("Getting bill by reference...");
            var billByReference = await api.GetBillByReferenceAsync(billReference);

            if (billByReference.error == null)
            {
                // success
                Console.WriteLine($"Bill Reference: {billByReference.res?.billReference}");
                Console.WriteLine($"Payment Code: {billByReference.res?.wbcCode}");
                Console.WriteLine($"Amount: {billByReference.res?.amount}");
                Console.WriteLine($"Payment Status: {billByReference.res?.paymentStatus}");
                Console.WriteLine($"Update Timestamp: {billByReference.res?.updateTimeStamp}");
            }
            else
            {
                // fail
                Console.WriteLine($"error: {billByReference.error}");
                Console.WriteLine($"errorCode: {billByReference.errorCode}");
            }

            Console.WriteLine("Getting bill by payment code...");
            var billByPaymentCode = await api.GetBillByPaymentCodeAsync(paymentCode);

            if (billByPaymentCode.error == null)
            {
                // success
                Console.WriteLine($"Bill Reference: {billByPaymentCode.res?.billReference}");
                Console.WriteLine($"Payment Code: {billByPaymentCode.res?.wbcCode}");
            }
            else
            {
                // fail
                Console.WriteLine($"error: {billByPaymentCode.error}");
                Console.WriteLine($"errorCode: {billByPaymentCode.errorCode}");
            }

            Console.WriteLine("Listing bills...");
            var paymentStatus = -1; // -1 all, 0 pending, 1 unconfirmed payment, 2 paid.
            var lastTimeStamp = "20251231"; // Date-only cursor; use "20251231235959" when you need time precision.
            var bills = await api.GetBillsAsync(paymentStatus, lastTimeStamp, limit: 100);

            if (bills.error == null)
            {
                // success
                Console.WriteLine($"Bills returned: {bills.res?.Count ?? 0}");
                foreach (var bill in bills.res ?? new List<BillResponse>())
                {
                    Console.WriteLine("-----------------------------");
                    Console.WriteLine($"Bill Reference: {bill.billReference}");
                    Console.WriteLine($"Payment Code: {bill.wbcCode}");
                    Console.WriteLine($"Amount: {bill.amount}");
                    Console.WriteLine($"Payment Status: {bill.paymentStatus}");
                    Console.WriteLine($"Update Timestamp: {bill.updateTimeStamp}");
                }
            }
            else
            {
                // fail
                Console.WriteLine($"error: {bills.error}");
                Console.WriteLine($"errorCode: {bills.errorCode}");
            }
        }
    }
}

```

Timestamp cursors can be date-only (`yyyyMMdd`) or include time (`yyyyMMddHHmmss`). Use empty string only when you intentionally want all history from the beginning.

### Getting Payment status of an existing Bill from WeBirr Servers

```C#
using System;
using System.Threading.Tasks;
using WeBirr;

namespace WeBirr.Example
{
    class Program
    {
        static readonly string apiKey = Environment.GetEnvironmentVariable("WEBIRR_TEST_ENV_API_KEY") ?? "";
        static readonly string merchantId = Environment.GetEnvironmentVariable("WEBIRR_TEST_ENV_MERCHANT_ID") ?? "";

        /// Getting Payment status of an existing Bill from WeBirr Servers
        public static async Task GetPaymentStatusAsync()
        {
            var api = new WeBirrClient(merchantId, apiKey, isTestEnv: true);

            var paymentCode = "PAYMENT_CODE_YOU_SAVED_AFTER_CREATING_A_NEW_BILL"; // suchas as '141 263 782';

            Console.WriteLine("Getting Payment Status...");
            var r = await api.GetPaymentStatusAsync(paymentCode);

            if (r.error == null)
            {
                // success
                if (r.res?.IsPaid ?? false)
                {
                    Console.WriteLine("bill is paid");
                    Console.WriteLine("bill payment detail");
                    Console.WriteLine($"Bank: {r.res?.data?.bankID}");
                    Console.WriteLine($"Bank Reference Number: {r.res?.data?.paymentReference}");
                    Console.WriteLine($"Amount Paid: {r.res?.data?.amount}");
                    Console.WriteLine($"Payment Date: {r.res?.data?.paymentDate}");
                }
                else
                    Console.WriteLine("bill is pending payment");
            }
            else
            {
                // fail
                Console.WriteLine($"error: {r.error}");
                Console.WriteLine($"errorCode: {r.errorCode}"); // can be used to handle specific busines error such as ERROR_INVLAID_INPUT
            }

        }
    }
}

```

*Sample object returned from getPaymentStatus()*

```C#
var sample = new Payment
{
    status = 2, // 0. Pending, 1. Payment in Progress, 2. Paid
    data = new PaymentDetail
    {
        status = 2,
        bankID = "test-bank",
        paymentReference = "TX-1",
        amount = "278.00",
        paymentDate = "2026-06-12 10:11:12",
        wbcCode = "141 263 782",
        updateTimeStamp = "2026061210121200000"
    }
};
```

Use `paymentDate` as the payment time field. `time` remains available as a deprecated backward-compatible alias.

### Deleting an existing Bill from WeBirr Servers (if it is not paid)

```C#
using System;
using System.Threading.Tasks;
using WeBirr;

namespace WeBirr.Example
{
    class Program
    {
        static readonly string apiKey = Environment.GetEnvironmentVariable("WEBIRR_TEST_ENV_API_KEY") ?? "";
        static readonly string merchantId = Environment.GetEnvironmentVariable("WEBIRR_TEST_ENV_MERCHANT_ID") ?? "";

        /// Deleting an existing Bill from WeBirr Servers (if it is not paid)
        public static async Task DeleteBillAsync()
        {
            var api = new WeBirrClient(merchantId, apiKey, isTestEnv: true);

            var paymentCode = "PAYMENT_CODE_YOU_SAVED_AFTER_CREATING_A_NEW_BILL"; // suchas as '141 263 782';

            Console.WriteLine("Deleting Bill...");
            var res = await api.DeleteBillAsync(paymentCode);

            if (res.error == null)
            {
                // success
                Console.WriteLine("bill is deleted successfully"); //res.res will be 'OK'  no need to check here!
            }
            else
            {
                // fail
                Console.WriteLine($"error: {res.error}");
                Console.WriteLine($"errorCode: {res.errorCode}"); // can be used to handle specific busines error such as ERROR_INVLAID_INPUT
            }

        }
    }
}

```

### Getting list of Payments and process them with Bulk Polling Consumer

```C#
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WeBirr;

namespace WeBirr.Example
{
    class Program
    {
        static readonly string apiKey = Environment.GetEnvironmentVariable("WEBIRR_TEST_ENV_API_KEY") ?? "";
        static readonly string merchantId = Environment.GetEnvironmentVariable("WEBIRR_TEST_ENV_MERCHANT_ID") ?? "";

        /// Getting list of Payments and process them with Bulk Polling Consumer
        public static async Task GetPaymentsAsync()
        {
            var api = new WeBirrClient(merchantId, apiKey, isTestEnv: true);

            var lastTimeStamp = "20251231"; // Date-only cursor; use "20251231235959" when you need time precision.
            var limit = 100;

            Console.WriteLine("Retrieving Payments...");
            var response = await api.GetPaymentsAsync(lastTimeStamp, limit);

            if (response.error == null)
            {
                // success
                foreach (var payment in response.res ?? new List<PaymentResponse>())
                {
                    Console.WriteLine("-----------------------------");
                    Console.WriteLine($"Payment Status: {payment.status}");
                    if (payment.IsPaid)
                        Console.WriteLine("Payment Status Text: Paid.");
                    if (payment.IsReversed)
                        Console.WriteLine("Payment Status Text: Reversed.");
                    Console.WriteLine($"Bank: {payment.bankID}");
                    Console.WriteLine($"Bank Reference Number: {payment.paymentReference}");
                    Console.WriteLine($"Amount Paid: {payment.amount}");
                    Console.WriteLine($"Payment Date: {payment.paymentDate}");
                    Console.WriteLine($"Reversal/Cancel Date: {payment.canceledTime}");
                    Console.WriteLine($"Update Timestamp: {payment.updateTimeStamp}");
                    lastTimeStamp = payment.updateTimeStamp; // save updateTimeStamp to your database for the next getPayments() call
                }
            }
            else
            {
                // fail
                Console.WriteLine($"error: {response.error}");
                Console.WriteLine($"errorCode: {response.errorCode}"); // can be used to handle specific business error such as ERROR_INVALID_INPUT
            }
        }
    }
}

```

### Webhooks - Payment processing using Webhook Callbacks

```C#
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WeBirr;

// Webhook handler for processing payment updates from WeBirr.
// This endpoint should be hosted on a secure server with HTTPS enabled.
public class Webhook
{
    static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    /// Handle incoming webhook POST requests.
    /// Validates request method and checks authentication using authKey from the query string.
    public WebhookResult HandleRequest(string method, string providedAuthKey, string rawPayload)
    {
        if (!string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase))
        {
            return WebhookResult.Json(405, @"{""error"":""Method Not Allowed. POST required.""}");
        }

        if (!IsAuthenticated(providedAuthKey))
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

        // Process the payment asynchronously or enqueue it to a background worker.
        ProcessPayment(payload.data);

        // Return JSON success response. Empty body with 200 OK is also acceptable.
        return WebhookResult.Json(200, @"{""success"":true,""message"":""Payment received and queued for processing""}");
    }

    private bool IsAuthenticated(string providedAuthKey)
    {
        // TODO: Replace this with your own auth key, preferably from an environment variable.
        var expectedAuthKey = "please-change-me-to-secure-key-5114831AFD5D4646901DCDAC58B92F8E";

        if (string.IsNullOrEmpty(providedAuthKey) || string.IsNullOrEmpty(expectedAuthKey))
            return false;

        var provided = Encoding.UTF8.GetBytes(providedAuthKey);
        var expected = Encoding.UTF8.GetBytes(expectedAuthKey);
        return provided.Length == expected.Length && CryptographicOperations.FixedTimeEquals(provided, expected);
    }

    /// Process Payment should be implemented as idempotent operation for production use cases.
    /// This method and logic can be shared among all payment processing consumers:
    /// 1. bulk polling, 2. webhook, 3. single payment polling.
    private void ProcessPayment(PaymentResponse payment)
    {
        Console.WriteLine($"Payment Status: {payment.status}");
        if (payment.IsPaid)
            Console.WriteLine("Payment Status Text: Paid.");
        if (payment.IsReversed)
            Console.WriteLine("Payment Status Text: Reversed.");
        Console.WriteLine($"Bank: {payment.bankID}");
        Console.WriteLine($"Bank Reference Number: {payment.paymentReference}");
        Console.WriteLine($"Amount Paid: {payment.amount}");
        Console.WriteLine($"Payment Date: {payment.paymentDate}");
        Console.WriteLine($"Reversal/Cancel Date: {payment.canceledTime}");
        Console.WriteLine($"Update Timestamp: {payment.updateTimeStamp}");
    }

    private sealed class WebhookPayload
    {
        public PaymentResponse data { get; set; }
    }
}

public sealed class WebhookResult
{
    public int StatusCode { get; set; }
    public string ContentType { get; set; }
    public string Body { get; set; }

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

// Once hosted, the webhook URL needs to be shared with WeBirr for configuration.
```

### Gettting basic Statistics about bills created and payments received for a date range

```C#
using System;
using System.Threading.Tasks;
using WeBirr;

namespace WeBirr.Example
{
    class Program
    {
        static readonly string apiKey = Environment.GetEnvironmentVariable("WEBIRR_TEST_ENV_API_KEY") ?? "";
        static readonly string merchantId = Environment.GetEnvironmentVariable("WEBIRR_TEST_ENV_MERCHANT_ID") ?? "";

        /// Get basic statistics about bills created and payments received for a date range
        public static async Task GetStatAsync()
        {
            var api = new WeBirrClient(merchantId, apiKey, isTestEnv: true);

            var dateFrom = "2025-01-01"; // YYYY-MM-DD
            var dateTo = "2030-01-31"; // YYYY-MM-DD

            Console.WriteLine("Retrieving Statistics...");
            Console.WriteLine($"Date From: {dateFrom}");
            Console.WriteLine($"Date To: {dateTo}");

            var response = await api.GetStatAsync(dateFrom, dateTo);

            if (response.error == null)
            {
                // success
                var stat = response.res;
                Console.WriteLine($"Number of Bills Created: {stat.nBills}");
                Console.WriteLine($"Number of Paid Bills: {stat.nBillsPaid}");
                Console.WriteLine($"Number of Unpaid Bills: {stat.nBillsUnpaid}");
                Console.WriteLine($"Amount of Bills: {stat.amountBills}");
                Console.WriteLine($"Amount Paid: {stat.amountPaid}");
                Console.WriteLine($"Amount Unpaid: {stat.amountUnpaid}");
            }
            else
            {
                // fail
                Console.WriteLine($"error: {response.error}");
                Console.WriteLine($"errorCode: {response.errorCode}");
            }
        }
    }
}

```

## Examples

The `WeBirr.Example` project includes separate workflows matching the PHP SDK examples:

```bash
dotnet run --project WeBirr.Example -- create-update-bill
dotnet run --project WeBirr.Example -- payment-status
dotnet run --project WeBirr.Example -- delete-bill
dotnet run --project WeBirr.Example -- bulk-payment-polling
dotnet run --project WeBirr.Example -- stat-report
dotnet run --project WeBirr.Example -- webhook
dotnet run --project WeBirr.Example -- get-bill-and-list-bills
```

Running `dotnet run --project WeBirr.Example` without a command runs the end-to-end TestEnv sample that creates, updates, reads, lists, polls, reports, and deletes a bill.

## Backward Compatibility

The old constructor remains available:

```csharp
var api = new WeBirrClient(apiKey, isTestEnv: true);
```

For new merchant-scoped methods such as bill lookup, bill listing, payment bulk polling, and stats, use the preferred constructor with merchant ID.
