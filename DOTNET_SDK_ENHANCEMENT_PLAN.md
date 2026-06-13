# .NET SDK Enhancement Plan

Status: implemented locally for `1.1.0`; NuGet publish is still pending and
should be done together with Elias.

## Goal

Bring the .NET SDK closer to the PHP `2.1.1` SDK while keeping the release
backward compatible for existing NuGet users.

Expected release direction: `1.0.3` to `1.1.0`.

Package: NuGet `WeBirr`

Current NuGet state checked during planning:

- latest published version: `1.0.3`
- owner shown by NuGet API: `WeBirr`
- total downloads shown by NuGet API: about `2401`
- project URL: `https://github.com/webirr/webirr-api-dotnet-client`

## PHP Reference

Use PHP SDK `2.1.1` as the behavior baseline:

| PHP method | Route | .NET status |
| --- | --- | --- |
| `createBill` | `POST /einvoice/api/bill` | existing method uses legacy `/einvoice/api/postbill` |
| `updateBill` | `PUT /einvoice/api/bill` | existing method uses legacy `/einvoice/api/postbill` |
| `deleteBill` | `DELETE /einvoice/api/bill?wbc_code=...` | existing method uses legacy `/einvoice/api/deletebill` |
| `getPaymentStatus` | `GET /einvoice/api/paymentStatus?wbc_code=...` | existing method uses legacy `/einvoice/api/getPaymentStatus` |
| `getBillByReference` | `GET /einvoice/api/bill?bill_reference=...` | missing |
| `getBillByPaymentCode` | `GET /einvoice/api/bill?wbc_code=...` | missing |
| `getPayments` | `GET /einvoice/api/payments?last_timestamp=...&limit=...` | missing |
| `getBills` | `GET /einvoice/api/bills?payment_status=...&last_timestamp=...&limit=...` | missing |
| `getStat` | `GET /merchant/stat?date_from=...&date_to=...` | missing |

The preferred base URL should match PHP:

- TestEnv: `https://api.webirr.net`
- Production: `https://api.webirr.net:8080`

The current .NET SDK still uses `api.webirr.com`, which should be changed for
the preferred constructor path.

## Backward Compatibility Rules

1. Keep existing public methods:
   - `CreateBillAsync(Bill bill)`
   - `UpdateBillAsync(Bill bill)`
   - `DeleteBillAsync(string paymentCode)`
   - `GetPaymentStatusAsync(string paymentCode)`
2. Keep the existing constructor:
   - `WeBirrClient(string apikey, bool isTestEnv)`
3. Add the preferred constructor without removing the old one:
   - `WeBirrClient(string merchantId, string apiKey, bool isTestEnv)`
4. Keep `Bill.merchantID` for wire compatibility, but prefer setting it from
   the client object when the preferred constructor is used.
5. Existing users who still set `Bill.merchantID` should continue to work.
6. Do not remove legacy public method names or change existing return types.
7. New methods may require the preferred constructor with merchant ID.

## Model Plan

Keep request and response model boundaries explicit.

### Request Model

Keep `Bill` as the create/update request model:

- `customerCode`
- `customerName`
- `customerPhone` - important gateway field; do not miss this in any SDK
- `billReference`
- `time`
- `description`
- `amount`
- `merchantID`
- `extras`

The SDK should set `bill.merchantID` from the client merchant ID before sending
create/update requests when the client was created with merchant ID.

### Response Model

Add `BillResponse`, preferably inheriting from `Bill` if it stays clean:

```csharp
public class BillResponse : Bill
{
    public string wbcCode { get; set; }
    public int paymentStatus { get; set; }
    public string updateTimeStamp { get; set; }
}
```

Do not add `updateTimeStamp`, `paymentStatus`, or `wbcCode` to the request
`Bill` model. These are response-side fields.

Do not add `detailHtml` to the .NET SDK plan unless a concrete merchant use
case is approved. Unknown server response fields can be ignored by JSON
deserialization.

### Payment Model

Extend the response model enough to match PHP bulk payment polling:

- preserve existing `Payment` for `GetPaymentStatusAsync`
- add `PaymentResponse` for timestamp-based `GetPaymentsAsync` bulk polling
- include gateway timestamp polling fields: `status`, `id`, `bankID`,
  `paymentReference`, `paymentDate`, `confirmed`, `confirmedTime`, `canceled`,
  `canceledTime`, `amount`, `wbcCode`, `updateTimeStamp`
- keep legacy single-status `Payment.data.time` support because
  `/einvoice/api/paymentStatus` still returns it for backward compatibility
- do not use old serial-number polling in new SDK methods; timestamp polling is
  the preferred API

### Stat Model

Add a stats response model for `GetStatAsync`:

- `nBills`
- `nBillsPaid`
- `nBillsUnpaid`
- `amountBills`
- `amountPaid`
- `amountUnpaid`

## Client Implementation Plan

1. Store `_merchantId`, `_apiKey`, and `_baseAddress` on `WeBirrClient`.
2. Keep old constructor and add preferred constructor.
3. Add a shared query builder that URL-encodes:
   - `api_key`
   - `merchant_id` when available or required
   - endpoint-specific parameters
4. Add a shared response decoder to avoid repeated JSON handling.
5. Prefer a single reusable `HttpClient` per `WeBirrClient` instance instead of
   constructing a new `HttpClient` per method.
6. Defer framework-native factory integration (`IHttpClientFactory`) to a later
   larger design unless we can add it without breaking constructor behavior.
7. For create/update:
   - if `_merchantId` is set, assign `bill.merchantID = _merchantId`
   - call canonical `/einvoice/api/bill`
8. For existing delete/status methods:
   - use canonical routes when `_merchantId` is available
   - preserve legacy behavior if needed for old constructor compatibility
9. For new methods, require `_merchantId` and return an `ApiResponse` error if
   it is missing.

## Planned New API Methods

```csharp
Task<ApiResponse<BillResponse>> GetBillByReferenceAsync(string billReference);
Task<ApiResponse<BillResponse>> GetBillByPaymentCodeAsync(string paymentCode);
Task<ApiResponse<List<BillResponse>>> GetBillsAsync(int paymentStatus = -1, string lastTimeStamp = "", int limit = 100);
Task<ApiResponse<List<PaymentResponse>>> GetPaymentsAsync(string lastTimeStamp = "", int limit = 100);
Task<ApiResponse<Stat>> GetStatAsync(string dateFrom, string dateTo);
```

Naming note: keep existing lower-case JSON property names in DTOs unless we
decide to add `[JsonProperty]` mappings. This reduces package behavior risk.

## Cursor Behavior For Bill Sync

`GetBillsAsync` should return bill response objects with `updateTimeStamp`.
The gateway does not return a separate top-level cursor. Callers should persist
the last processed `BillResponse.updateTimeStamp` and pass it as
`lastTimeStamp` on the next call.

Example behavior:

```csharp
var res = await client.GetBillsAsync(-1, lastTimeStamp, 100);

foreach (var bill in res.res)
{
    // Process and save bill first.
    lastTimeStamp = bill.updateTimeStamp;
}
```

Only save the new cursor after the whole batch is processed successfully.

## Test Plan

Keep two layers, matching the PHP pattern.

### Fast Tests

Command should remain:

```bash
dotnet test
```

Planned coverage:

- invalid API key returns API error for existing methods
- query construction includes `api_key` and `merchant_id`
- create/update set `Bill.merchantID` from client merchant ID
- new response DTOs deserialize expected JSON
- cursor helper/example uses `updateTimeStamp`
- old constructor remains usable

Current test issue found during planning:

- this machine has .NET SDK/runtime `10.0.101` only
- current test/example projects target unsupported `net5.0`
- `dotnet test WeBirr.sln` restores/builds but aborts test execution because
  the runner asks for an x64 .NET host on this arm64 machine

Planned fix:

- keep library target `netstandard2.0` for package compatibility
- retarget test/example projects away from `net5.0` to an available supported
  test runtime, or install the required runtime in CI/local release machine
- update test packages if needed

### Live TestEnv Smoke Tests

Add a separate command or category for live TestEnv tests. Environment variable
names should match PHP:

```bash
WEBIRR_TEST_ENV_MERCHANT_ID=...
WEBIRR_TEST_ENV_API_KEY=...
```

Planned live checks:

- create bill with GUID-style `billReference`
- assert returned payment code
- update bill
- get payment status and assert new bill is pending
- get bill by reference and verify fields
- get bill by payment code and verify fields
- get bills with cursor and find the generated bill
- get payments as smoke test
- get stats as smoke test
- delete bill and verify lookup returns not found
- cleanup in teardown if delete test does not run

No TestEnv API key or merchant credential should be committed.

## Example And README Plan

Update examples and README to match PHP `2.1.0`:

- use `WEBIRR_TEST_ENV_MERCHANT_ID`
- use `WEBIRR_TEST_ENV_API_KEY`
- assume TestEnv for examples
- remove manual `Bill.merchantID` assignment in examples using preferred
  constructor
- show create/update/delete/status
- add bill lookup/list examples
- add bulk payment polling example
- add stats example
- add webhook receiver guidance or an ASP.NET Core webhook sample
- document cursor handling with `updateTimeStamp`

## NuGet Release Prep

Current package metadata:

- package ID: `WeBirr`
- current version: `1.0.3`
- planned backward-compatible version: `1.1.0`
- owner shown by NuGet API: `WeBirr`
- total downloads shown by NuGet API: about `2401`

Planned release files:

- update `WeBirr/WeBirr.csproj` version to `1.1.0`
- keep package ID `WeBirr`
- add or update package release notes with a brief style consistent with prior
  SDK releases
- consider adding package README metadata because `dotnet pack/test` currently
  warns that the package is missing a readme

Planned release commands:

```bash
dotnet restore WeBirr.sln
dotnet test WeBirr.sln
dotnet pack WeBirr/WeBirr.csproj -c Release
dotnet nuget push WeBirr/bin/Release/WeBirr.1.1.0.nupkg --api-key "$NUGET_API_KEY" --source https://api.nuget.org/v3/index.json
```

Secrets handling:

- restricted source checked: `docs/webirr-docs/references/restricted/WeBirr Secrets.pdf`
- the restricted PDF contains NuGet-related terms, but no secret values should
  be copied into this repo, Markdown, commit messages, or chat
- at release time, use the approved secret source privately to obtain or
  confirm the NuGet API key/account
- if the NuGet API key/account is missing or expired, ask Elias before release

Public GitHub Release style:

- title should be just the version, for example `1.1.0`
- notes should be brief and similar to prior SDK releases

## Task Checklist

| ID | Status | Task |
| --- | --- | --- |
| DOTNET-SDK-001 | done | Keep release backward compatible and plan version `1.0.3` to `1.1.0`. |
| DOTNET-SDK-002 | done | Add preferred constructor with merchant ID while keeping old constructor. |
| DOTNET-SDK-003 | done | Change preferred base URL to `api.webirr.net` for test/prod parity with PHP. |
| DOTNET-SDK-004 | done | Prefer canonical REST endpoints while preserving old public methods. |
| DOTNET-SDK-005 | done | Make create/update set `Bill.merchantID` from the client merchant ID when available. |
| DOTNET-SDK-006 | done | Add `BillResponse` response DTO, preferably inheriting from `Bill`. |
| DOTNET-SDK-007 | done | Add `GetBillByReferenceAsync`. |
| DOTNET-SDK-008 | done | Add `GetBillByPaymentCodeAsync`. |
| DOTNET-SDK-009 | done | Add `GetBillsAsync` with `updateTimeStamp` cursor guidance. |
| DOTNET-SDK-010 | done | Add timestamp-based `GetPaymentsAsync` bulk polling with `PaymentResponse` DTO. |
| DOTNET-SDK-011 | done | Add `GetStatAsync` and `Stat` response DTO. |
| DOTNET-SDK-012 | done | Add fast tests for query construction, deserialization, merchant ID setting, and invalid credentials. |
| DOTNET-SDK-013 | done | Add live TestEnv smoke tests using `WEBIRR_TEST_ENV_MERCHANT_ID` and `WEBIRR_TEST_ENV_API_KEY`. |
| DOTNET-SDK-014 | done | Update examples and README to match PHP 2.1.1 coverage. |
| DOTNET-SDK-015 | done | Modernize test/example target frameworks or release-machine runtime so tests run reliably. |
| DOTNET-SDK-016 | done | Prepare NuGet release metadata, package README handling, and brief release notes. |
| DOTNET-SDK-017 | todo | Verify NuGet credentials privately from the approved restricted source before publish. |

## Implementation Decisions

1. Existing public methods now use canonical routes. The old constructor remains
   usable for create/update/delete/status; merchant-scoped new methods require
   the preferred constructor with merchant ID.
2. The library stays on `netstandard2.0`; tests and examples target `net10.0`
   so they run on the current local release machine.
3. ASP.NET Core webhook sample work remains a separate follow-up.
4. NuGet publish is intentionally not done yet; it should be completed together
   after confirming the NuGet API key/account path.
