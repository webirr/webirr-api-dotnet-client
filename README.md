Official .NET Client Library for WeBirr Payment Gateway APIs

This library provides convenient access to WeBirr Payment Gateway APIs from
.NET applications.

## Install

```bash
dotnet add package WeBirr
```

Package Manager:

```powershell
Install-Package WeBirr
```

## Usage

Create the client with merchant ID, API key, and environment once. The client
automatically sets `Bill.merchantID` before sending bill create/update requests,
so application code and examples should not set `merchantID` on the bill object.

Examples assume the WeBirr TestEnv and read credentials from environment
variables:

```bash
export WEBIRR_TEST_ENV_MERCHANT_ID="YOUR_TEST_MERCHANT_ID"
export WEBIRR_TEST_ENV_API_KEY="YOUR_TEST_API_KEY"
```

## Create And Update A Bill

```csharp
using WeBirr;

var merchantId = Environment.GetEnvironmentVariable("WEBIRR_TEST_ENV_MERCHANT_ID") ?? "";
var apiKey = Environment.GetEnvironmentVariable("WEBIRR_TEST_ENV_API_KEY") ?? "";
var api = new WeBirrClient(merchantId, apiKey, isTestEnv: true);

var bill = new Bill
{
    amount = "270.90",
    customerCode = "cc01",
    customerName = "SDK Test Customer",
    customerPhone = "0911000000", // optional; used for SMS notification when enabled for the merchant
    time = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm"),
    description = "hotel booking",
    billReference = "dotnet/example/" + Guid.NewGuid()
};

var create = await api.CreateBillAsync(bill);
if (string.IsNullOrEmpty(create.error))
{
    var paymentCode = create.res;
    Console.WriteLine($"Payment Code = {paymentCode}");
}

bill.amount = "278.00";
bill.customerName = "SDK Test Customer Updated";
var update = await api.UpdateBillAsync(bill);
```

## Get A Bill And List Bills

```csharp
var billByReference = await api.GetBillByReferenceAsync(bill.billReference);
var billByPaymentCode = await api.GetBillByPaymentCodeAsync("123 456 789");

var paymentStatus = -1; // -1 all, 0 pending, 1 unconfirmed payment, 2 paid.
var lastTimeStamp = ""; // Empty string starts from the beginning.
var bills = await api.GetBillsAsync(paymentStatus, lastTimeStamp, limit: 100);
```

`GetBillsAsync` returns `BillResponse` items. `BillResponse` inherits from
`Bill` and adds response-only fields: `wbcCode`, `paymentStatus`, and
`updateTimeStamp`.

## Single Payment Status

```csharp
var status = await api.GetPaymentStatusAsync("123 456 789");

if (status.res?.IsPaid ?? false)
{
    var payment = status.res.data;
    Console.WriteLine($"Bank: {payment.bankID}");
    Console.WriteLine($"Bank Reference Number: {payment.paymentReference}");
    Console.WriteLine($"Amount Paid: {payment.amount}");
    Console.WriteLine($"Payment Date: {payment.paymentDate}");
}
```

Use `paymentDate` as the payment time field. `time` remains available as a
deprecated backward-compatible alias.

## Bulk Payment Polling

```csharp
var payments = await api.GetPaymentsAsync(lastTimeStamp: "", limit: 100);

foreach (var payment in payments.res)
{
    Console.WriteLine($"{payment.paymentReference}: {payment.amount} at {payment.paymentDate}");
    lastTimeStamp = payment.updateTimeStamp;
}
```

For bulk polling, persist `updateTimeStamp` as the next polling cursor. Do not
use `paymentDate` or `time` as the cursor.

## Merchant Stats

```csharp
var stat = await api.GetStatAsync("2025-01-01", "2030-01-31");

Console.WriteLine($"Bills: {stat.res.nBills}");
Console.WriteLine($"Paid: {stat.res.nBillsPaid}");
Console.WriteLine($"Amount paid: {stat.res.amountPaid}");
```

## Delete A Bill

```csharp
var delete = await api.DeleteBillAsync("123 456 789");
```

## Backward Compatibility

The old constructor remains available:

```csharp
var api = new WeBirrClient(apiKey, isTestEnv: true);
```

For new merchant-scoped methods such as bill lookup, bill listing, payment bulk
polling, and stats, use the preferred constructor with merchant ID.
