using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using WeBirr;

namespace WeBirr.Test
{
    [NonParallelizable]
    public class WeBirrClientTests
    {
        const string CreatedAmount = "270.90";
        const string UpdatedAmount = "278.00";
        const string CustomerCode = "sdk-test-customer";
        const string CreatedCustomerName = "SDK Test Customer";
        const string UpdatedCustomerName = "SDK Test Customer Updated";
        const string CustomerPhone = "0911000000";
        const string Description = "SDK Test Bill";
        const string ExampleCursor = "20251231";

        static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        static WeBirrClient _testEnvApi;
        static string _testEnvMerchantId;
        static string _testEnvPaymentCode;
        static string _testEnvBillReference;
        static string _testEnvBillUpdateTimeStamp;
        static bool _testEnvBillCreated;
        static bool _testEnvBillUpdated;
        static bool _testEnvDeleted;

        [OneTimeTearDown]
        public async Task CleanupTestEnvBill()
        {
            if (_testEnvApi != null && !_testEnvDeleted && !string.IsNullOrEmpty(_testEnvPaymentCode))
            {
                await _testEnvApi.DeleteBillAsync(_testEnvPaymentCode);
            }
        }

        [Test]
        public async Task CreateBill_should_get_error_from_WebService_on_invalid_api_key_TestEnv()
        {
            var bill = SampleBill("dotnet/unit/" + Guid.NewGuid());
            var api = new WeBirrClient("x", true);

            var res = await api.CreateBillAsync(bill);

            AssertApiError(res);
        }

        [Test]
        public async Task CreateBill_should_get_error_from_WebService_on_invalid_api_key_ProdEnv()
        {
            var bill = SampleBill("dotnet/unit/" + Guid.NewGuid());
            var api = new WeBirrClient("x", false);

            var res = await api.CreateBillAsync(bill);

            AssertApiError(res);
        }

        [Test]
        public async Task UpdateBill_should_get_error_from_WebService_on_invalid_api_key()
        {
            var bill = SampleBill("dotnet/unit/" + Guid.NewGuid());
            var api = new WeBirrClient("x", true);

            var res = await api.UpdateBillAsync(bill);

            AssertApiError(res);
        }

        [Test]
        public async Task DeleteBill_should_get_error_from_WebService_on_invalid_api_key()
        {
            var api = new WeBirrClient("x", true);

            var res = await api.DeleteBillAsync("xxxx");

            AssertApiError(res);
        }

        [Test]
        public async Task GetPaymentStatus_should_get_error_from_WebService_on_invalid_api_key()
        {
            var api = new WeBirrClient("x", true);

            var res = await api.GetPaymentStatusAsync("xxxx");

            AssertApiError(res);
        }

        [Test]
        public async Task GetBillByReference_should_get_error_from_WebService_on_invalid_api_key()
        {
            var api = new WeBirrClient("x", "x", true);

            var res = await api.GetBillByReferenceAsync("missing-reference");

            AssertApiError(res);
        }

        [Test]
        public async Task GetBillByPaymentCode_should_get_error_from_WebService_on_invalid_api_key()
        {
            var api = new WeBirrClient("x", "x", true);

            var res = await api.GetBillByPaymentCodeAsync("xxxx");

            AssertApiError(res);
        }

        [Test]
        public async Task GetBills_should_get_error_from_WebService_on_invalid_api_key()
        {
            var api = new WeBirrClient("x", "x", true);

            var res = await api.GetBillsAsync(-1, ExampleCursor, 10);

            AssertApiError(res);
        }

        [Test]
        public async Task GetPayments_should_get_error_from_WebService_on_invalid_api_key()
        {
            var api = new WeBirrClient("x", "x", true);

            var res = await api.GetPaymentsAsync(ExampleCursor, 10);

            AssertApiError(res);
        }

        [Test]
        public async Task GetStat_should_get_error_from_WebService_on_invalid_api_key()
        {
            var api = new WeBirrClient("x", "x", true);

            var res = await api.GetStatAsync("2025-01-01", "2025-01-02");

            AssertApiError(res);
        }

        [Test]
        public async Task GetSupportedBanks_should_get_error_from_WebService_on_invalid_api_key()
        {
            var api = new WeBirrClient("x", "x", true);

            var res = await api.GetSupportedBanksAsync();

            AssertApiError(res);
        }

        [Test]
        public async Task Preferred_constructor_sets_bill_merchant_id_before_sending()
        {
            var bill = SampleBill("dotnet/unit/" + Guid.NewGuid());
            var api = new WeBirrClient("merchant-from-client", "x", true);

            await api.CreateBillAsync(bill);

            Assert.That(bill.merchantID, Is.EqualTo("merchant-from-client"));
        }

        [Test]
        public async Task Constructor_can_use_injected_http_client_for_requests()
        {
            var handler = new StubHttpMessageHandler(@"{""error"":null,""res"":""OK""}");
            var httpClient = new HttpClient(handler);
            var api = new WeBirrClient("merchant-from-client", "x", true, httpClient);

            var response = await api.DeleteBillAsync("123456789");

            AssertNoApiError(response);
            Assert.That(response.res, Is.EqualTo("OK"));
            Assert.That(handler.Requests, Has.Count.EqualTo(1));
            Assert.That(handler.Requests[0].RequestUri.ToString(), Does.Contain("merchant_id=merchant-from-client"));
            Assert.That(handler.Requests[0].RequestUri.ToString(), Does.Contain("wbc_code=123456789"));
        }

        [Test]
        public void Constructor_rejects_null_injected_http_client()
        {
            Assert.Throws<ArgumentNullException>(() => new WeBirrClient("merchant-from-client", "x", true, null));
        }

        [Test]
        public void Constructor_preserves_injected_http_client_accept_headers()
        {
            var httpClient = new HttpClient(new StubHttpMessageHandler(@"{""error"":null,""res"":""OK""}"));
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));

            _ = new WeBirrClient("merchant-from-client", "x", true, httpClient);

            Assert.That(httpClient.DefaultRequestHeaders.Accept.Any(header => header.MediaType == "text/plain"), Is.True);
            Assert.That(httpClient.DefaultRequestHeaders.Accept.Any(header => header.MediaType == "application/json"), Is.True);
        }

        [Test]
        public void TestEnv_defaults_to_dev_gateway()
        {
            WithGatewayUrl(null, () =>
            {
                var api = new WeBirrClient("merchant-from-client", "x", true);

                var url = BuildUrl(api, "einvoice/api/bill", null);

                Assert.That(url, Does.StartWith("https://api.webirr.dev/einvoice/api/bill?"));
            });
        }

        [Test]
        public void TestEnv_can_use_internal_gateway_url_override()
        {
            WithGatewayUrl("https://local-gateway.example/", () =>
            {
                var api = new WeBirrClient("merchant-from-client", "x", true);

                var url = BuildUrl(api, "einvoice/api/bill", null);

                Assert.That(url, Does.StartWith("https://local-gateway.example/einvoice/api/bill?"));
            });
        }

        [Test]
        public void ProdEnv_ignores_internal_gateway_url_override()
        {
            WithGatewayUrl("https://local-gateway.example/", () =>
            {
                var api = new WeBirrClient("merchant-from-client", "x", false);

                var url = BuildUrl(api, "einvoice/api/bill", null);

                Assert.That(url, Does.StartWith("https://api.webirr.net:8080/einvoice/api/bill?"));
            });
        }

        [Test]
        public void Legacy_constructor_does_not_overwrite_existing_bill_merchant_id_with_empty_client_merchant_id()
        {
            var bill = SampleBill("dotnet/unit/" + Guid.NewGuid());
            bill.merchantID = "merchant-on-bill";
            var api = new WeBirrClient("x", true);

            InvokePrepareBill(api, bill);

            Assert.That(bill.merchantID, Is.EqualTo("merchant-on-bill"));
        }

        [TestCaseSource(nameof(SdkEndpointQueryCases))]
        public void Url_builder_includes_merchant_id_for_all_endpoint_parameter_shapes_when_configured(string endpoint, string path, Dictionary<string, string> parameters)
        {
            var api = new WeBirrClient("merchant-from-client", "x", true);

            var url = BuildUrl(api, path, parameters);

            Assert.That(url, Does.Contain("merchant_id=merchant-from-client"), endpoint);
        }

        [TestCaseSource(nameof(SdkEndpointQueryCases))]
        public void Url_builder_omits_merchant_id_for_all_endpoint_parameter_shapes_when_client_merchant_id_is_empty(string endpoint, string path, Dictionary<string, string> parameters)
        {
            var api = new WeBirrClient("x", true);

            var url = BuildUrl(api, path, parameters);

            Assert.That(url, Does.Not.Contain("merchant_id="), endpoint);
        }

        [Test]
        public async Task Merchant_scoped_methods_require_preferred_constructor()
        {
            var api = new WeBirrClient("x", true);

            var bill = await api.GetBillByReferenceAsync("missing-reference");
            var bills = await api.GetBillsAsync();
            var payments = await api.GetPaymentsAsync();
            var stat = await api.GetStatAsync("2025-01-01", "2025-01-02");
            var supportedBanks = await api.GetSupportedBanksAsync();

            AssertApiError(bill);
            AssertApiError(bills);
            AssertApiError(payments);
            AssertApiError(stat);
            AssertApiError(supportedBanks);
        }

        [Test]
        public void Bill_serializes_customer_phone()
        {
            var bill = SampleBill("dotnet/unit/" + Guid.NewGuid());
            var element = SerializeBill(bill);

            Assert.That(element.GetProperty("customerPhone").GetString(), Is.EqualTo(CustomerPhone));
        }

        [Test]
        public void Bill_serializes_without_customer_phone_as_empty_string()
        {
            var bill = SampleBillWithoutCustomerPhone("dotnet/unit/" + Guid.NewGuid());
            var element = SerializeBill(bill);

            Assert.That(element.GetProperty("customerPhone").GetString(), Is.EqualTo(""));
        }

        [Test]
        public void Bill_serializes_empty_extras_as_json_object()
        {
            var bill = SampleBill("dotnet/unit/" + Guid.NewGuid());
            var element = SerializeBill(bill);
            var extras = element.GetProperty("extras");

            Assert.That(extras.ValueKind, Is.EqualTo(JsonValueKind.Object));
            Assert.That(extras.EnumerateObject().Any(), Is.False);
        }

        [Test]
        public void Bill_serializes_populated_extras_as_json_object()
        {
            var bill = SampleBill("dotnet/unit/" + Guid.NewGuid());
            bill.extras = new Dictionary<string, string> { { "source", "unit-test" } };
            var element = SerializeBill(bill);
            var extras = element.GetProperty("extras");

            Assert.That(extras.ValueKind, Is.EqualTo(JsonValueKind.Object));
            Assert.That(extras.GetProperty("source").GetString(), Is.EqualTo("unit-test"));
        }

        [Test]
        public void BillResponse_deserializes_retrieval_only_fields()
        {
            var json = @"{
                ""error"": null,
                ""res"": {
                    ""customerCode"": ""SDK-TEST-CUSTOMER"",
                    ""customerName"": ""SDK Test Customer"",
                    ""customerPhone"": ""0911000000"",
                    ""billReference"": ""dotnet/unit/1"",
                    ""time"": ""2026-06-12 10:00"",
                    ""description"": ""SDK Test Bill"",
                    ""amount"": ""278.00"",
                    ""merchantID"": ""merchant-from-client"",
                    ""wbcCode"": ""123 456 789"",
                    ""paymentStatus"": 0,
                    ""updateTimeStamp"": ""2026061210000000000""
                }
            }";

            var response = JsonSerializer.Deserialize<ApiResponse<BillResponse>>(json, JsonOptions);

            AssertNoApiError(response);
            Assert.That(response.res.customerPhone, Is.EqualTo(CustomerPhone));
            Assert.That(response.res.wbcCode, Is.EqualTo("123 456 789"));
            Assert.That(response.res.paymentStatus, Is.EqualTo(0));
            Assert.That(response.res.updateTimeStamp, Is.EqualTo("2026061210000000000"));
        }

        [Test]
        public void PaymentResponse_uses_paymentDate_as_time_alias()
        {
            var json = @"{
                ""error"": null,
                ""res"": [{
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
                }]
            }";

            var response = JsonSerializer.Deserialize<ApiResponse<List<PaymentResponse>>>(json, JsonOptions);
            var payment = response.res.Single();

            Assert.That(payment.paymentDate, Is.EqualTo("2026-06-12 10:11:12"));
#pragma warning disable CS0618
            Assert.That(payment.time, Is.EqualTo(payment.paymentDate));
#pragma warning restore CS0618
            Assert.That(payment.IsPaid, Is.True);
            Assert.That(payment.updateTimeStamp, Is.EqualTo("2026061210121200000"));
        }

        [Test]
        public void PaymentDetail_keeps_legacy_time_as_paymentDate_alias()
        {
            var json = @"{
                ""error"": null,
                ""res"": {
                    ""status"": 2,
                    ""data"": {
                        ""status"": 2,
                        ""id"": 101,
                        ""bankID"": ""test-bank"",
                        ""paymentReference"": ""TX-1"",
                        ""time"": ""2026-06-12 10:11:12"",
                        ""confirmed"": true,
                        ""confirmedTime"": ""2026-06-12 10:12:12"",
                        ""amount"": ""278.00"",
                        ""wbcCode"": ""123 456 789"",
                        ""updateTimeStamp"": ""2026061210121200000""
                    }
                }
            }";

            var response = JsonSerializer.Deserialize<ApiResponse<Payment>>(json, JsonOptions);

            Assert.That(response.res.data.paymentDate, Is.EqualTo("2026-06-12 10:11:12"));
#pragma warning disable CS0618
            Assert.That(response.res.data.time, Is.EqualTo(response.res.data.paymentDate));
#pragma warning restore CS0618
            Assert.That(response.res.IsPaid, Is.True);
        }

        [Test]
        public void Stat_deserializes_gateway_pascal_case_fields()
        {
            var json = @"{
                ""error"": null,
                ""res"": {
                    ""NBills"": 10,
                    ""NBillsPaid"": 4,
                    ""NBillsUnpaid"": 6,
                    ""AmountBills"": ""100.00"",
                    ""AmountPaid"": ""40.00"",
                    ""AmountUnpaid"": ""60.00""
                }
            }";

            var response = JsonSerializer.Deserialize<ApiResponse<Stat>>(json, JsonOptions);

            Assert.That(response.res.nBills, Is.EqualTo(10));
            Assert.That(response.res.nBillsPaid, Is.EqualTo(4));
            Assert.That(response.res.nBillsUnpaid, Is.EqualTo(6));
            Assert.That(response.res.amountBills, Is.EqualTo("100.00"));
            Assert.That(response.res.amountPaid, Is.EqualTo("40.00"));
            Assert.That(response.res.amountUnpaid, Is.EqualTo("60.00"));
        }

        [Test]
        public void SupportedBank_deserializes_gateway_fields()
        {
            var json = @"{
                ""error"": null,
                ""res"": [{
                    ""bankID"": ""cbe_mobile"",
                    ""name"": ""CBE Mobile Banking""
                }]
            }";

            var response = JsonSerializer.Deserialize<ApiResponse<List<SupportedBank>>>(json, JsonOptions);
            var bank = response.res.Single();

            Assert.That(bank.bankID, Is.EqualTo("cbe_mobile"));
            Assert.That(bank.name, Is.EqualTo("CBE Mobile Banking"));
        }

        [Test]
        [Category("TestEnv")]
        [Order(1)]
        public async Task TestEnv_CreateBill_without_manual_merchant_id()
        {
            await EnsureTestEnvBillCreatedAsync();

            Assert.That(_testEnvPaymentCode, Is.Not.Empty);
            Assert.That(_testEnvPaymentCode, Does.Match(@"^\d{3}\s\d{3}\s\d{3}$"));
        }

        [Test]
        [Category("TestEnv")]
        [Order(2)]
        public async Task TestEnv_UpdateBill_without_manual_merchant_id()
        {
            await EnsureTestEnvBillUpdatedAsync();

            Assert.That(_testEnvBillUpdated, Is.True);
        }

        [Test]
        [Category("TestEnv")]
        [Order(3)]
        public async Task TestEnv_GetPaymentStatus_returns_pending_for_new_bill()
        {
            await EnsureTestEnvBillCreatedAsync();

            var status = await TestEnvApi().GetPaymentStatusAsync(_testEnvPaymentCode);

            AssertNoApiError(status);
            Assert.That(status.res.status, Is.EqualTo(0));
            Assert.That(status.res.data, Is.Null);
        }

        [Test]
        [Category("TestEnv")]
        [Order(4)]
        public async Task TestEnv_GetBillByReference_returns_created_bill()
        {
            await EnsureTestEnvBillUpdatedAsync();

            var byReference = await TestEnvApi().GetBillByReferenceAsync(_testEnvBillReference);

            AssertNoApiError(byReference);
            AssertBillMatchesExpected(byReference.res, _testEnvMerchantId, _testEnvBillReference, _testEnvPaymentCode);
            _testEnvBillUpdateTimeStamp = byReference.res.updateTimeStamp;
        }

        [Test]
        [Category("TestEnv")]
        [Order(5)]
        public async Task TestEnv_GetBillByPaymentCode_returns_created_bill()
        {
            await EnsureTestEnvBillUpdatedAsync();

            var byPaymentCode = await TestEnvApi().GetBillByPaymentCodeAsync(_testEnvPaymentCode);

            AssertNoApiError(byPaymentCode);
            AssertBillMatchesExpected(byPaymentCode.res, _testEnvMerchantId, _testEnvBillReference, _testEnvPaymentCode);
        }

        [Test]
        [Category("TestEnv")]
        [Order(6)]
        public async Task TestEnv_GetBills_finds_created_bill()
        {
            await EnsureTestEnvBillLoadedAsync();

            var cursor = CursorBefore(_testEnvBillUpdateTimeStamp);
            var bills = await TestEnvApi().GetBillsAsync(0, cursor, 100);

            AssertNoApiError(bills);
            Assert.That(bills.res, Is.Not.Null);
            var listedBill = bills.res.FirstOrDefault(b => b.billReference == _testEnvBillReference);
            Assert.That(listedBill, Is.Not.Null);
            AssertBillMatchesExpected(listedBill, _testEnvMerchantId, _testEnvBillReference, _testEnvPaymentCode);
        }

        [Test]
        [Category("TestEnv")]
        [Order(7)]
        public async Task TestEnv_GetPayments_returns_payment_array()
        {
            var payments = await TestEnvApi().GetPaymentsAsync(ExampleCursor, 10);

            AssertNoApiError(payments);
            Assert.That(payments.res, Is.Not.Null);
        }

        [Test]
        [Category("TestEnv")]
        [Order(8)]
        public async Task TestEnv_GetStat_returns_stat_object()
        {
            var stat = await TestEnvApi().GetStatAsync("2025-01-01", "2030-01-31");

            AssertNoApiError(stat);
            Assert.That(stat.res, Is.Not.Null);
        }

        [Test]
        [Category("TestEnv")]
        [Order(9)]
        public async Task TestEnv_GetSupportedBanks_returns_merchant_scoped_bank_array()
        {
            var supportedBanks = await TestEnvApi().GetSupportedBanksAsync();

            AssertNoApiError(supportedBanks);
            Assert.That(supportedBanks.res, Is.Not.Null);
            Assert.That(supportedBanks.res, Is.Not.Empty);
            foreach (var bank in supportedBanks.res)
            {
                Assert.That(bank.bankID, Is.Not.Empty);
                Assert.That(bank.name, Is.Not.Empty);
            }
        }

        [Test]
        [Category("TestEnv")]
        [Order(99)]
        public async Task TestEnv_DeleteBill_removes_created_bill()
        {
            await EnsureTestEnvBillUpdatedAsync();

            var delete = await TestEnvApi().DeleteBillAsync(_testEnvPaymentCode);
            AssertNoApiError(delete);
            Assert.That(delete.res.ToLowerInvariant(), Is.EqualTo("ok"));
            _testEnvDeleted = true;

            var deletedBill = await TestEnvApi().GetBillByReferenceAsync(_testEnvBillReference);
            AssertApiError(deletedBill);
        }

        static WeBirrClient TestEnvApi()
        {
            var merchantId = Environment.GetEnvironmentVariable("WEBIRR_TEST_ENV_MERCHANT_ID") ?? "";
            var apiKey = Environment.GetEnvironmentVariable("WEBIRR_TEST_ENV_API_KEY") ?? "";

            if (string.IsNullOrEmpty(merchantId) || string.IsNullOrEmpty(apiKey))
            {
                Assert.Ignore("WEBIRR_TEST_ENV_MERCHANT_ID and WEBIRR_TEST_ENV_API_KEY are required for TestEnv smoke tests.");
            }

            if (_testEnvApi == null)
            {
                _testEnvMerchantId = merchantId;
                _testEnvBillReference = "dotnet/test/" + Guid.NewGuid();
                _testEnvApi = new WeBirrClient(merchantId, apiKey, true);
            }

            return _testEnvApi;
        }

        static async Task EnsureTestEnvBillCreatedAsync()
        {
            if (_testEnvBillCreated) return;

            var api = TestEnvApi();
            var createBill = SampleBill(_testEnvBillReference);
            var create = await api.CreateBillAsync(createBill);

            AssertNoApiError(create);
            Assert.That(createBill.merchantID, Is.EqualTo(_testEnvMerchantId));
            _testEnvPaymentCode = create.res;
            _testEnvBillCreated = true;
        }

        static async Task EnsureTestEnvBillUpdatedAsync()
        {
            await EnsureTestEnvBillCreatedAsync();
            if (_testEnvBillUpdated) return;

            var updateBill = SampleBill(_testEnvBillReference);
            updateBill.amount = UpdatedAmount;
            updateBill.customerName = UpdatedCustomerName;
            var update = await TestEnvApi().UpdateBillAsync(updateBill);

            AssertNoApiError(update);
            Assert.That(updateBill.merchantID, Is.EqualTo(_testEnvMerchantId));
            Assert.That(update.res.ToLowerInvariant(), Is.EqualTo("ok"));
            _testEnvBillUpdated = true;
        }

        static async Task EnsureTestEnvBillLoadedAsync()
        {
            if (!string.IsNullOrEmpty(_testEnvBillUpdateTimeStamp)) return;

            await EnsureTestEnvBillUpdatedAsync();
            var byReference = await TestEnvApi().GetBillByReferenceAsync(_testEnvBillReference);

            AssertNoApiError(byReference);
            AssertBillMatchesExpected(byReference.res, _testEnvMerchantId, _testEnvBillReference, _testEnvPaymentCode);
            _testEnvBillUpdateTimeStamp = byReference.res.updateTimeStamp;
        }

        static Bill SampleBill(string billReference) => new Bill
        {
            amount = CreatedAmount,
            customerCode = CustomerCode,
            customerName = CreatedCustomerName,
            customerPhone = CustomerPhone,
            time = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm"),
            description = Description,
            billReference = billReference,
            merchantID = "x",
            extras = new Dictionary<string, string>()
        };

        static Bill SampleBillWithoutCustomerPhone(string billReference) => new Bill
        {
            amount = CreatedAmount,
            customerCode = CustomerCode,
            customerName = CreatedCustomerName,
            time = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm"),
            description = Description,
            billReference = billReference,
            extras = new Dictionary<string, string>()
        };

        static JsonElement SerializeBill(Bill bill)
        {
            using (var doc = JsonDocument.Parse(JsonSerializer.Serialize(bill, JsonOptions)))
            {
                return doc.RootElement.Clone();
            }
        }

        static void AssertNoApiError<T>(ApiResponse<T> res) where T : class
        {
            Assert.That(res, Is.Not.Null);
            Assert.That(res.error, Is.Null.Or.Empty, res.error);
            Assert.That(res.errorCode, Is.Null.Or.Empty, res.errorCode);
        }

        static void AssertApiError<T>(ApiResponse<T> res) where T : class
        {
            Assert.That(res, Is.Not.Null);
            Assert.That(!string.IsNullOrEmpty(res.error) || !string.IsNullOrEmpty(res.errorCode), Is.True);
        }

        static void AssertBillMatchesExpected(BillResponse bill, string merchantId, string billReference, string paymentCode)
        {
            Assert.That(bill.billReference, Is.EqualTo(billReference));
            Assert.That(bill.merchantID, Is.EqualTo(merchantId));
            Assert.That(bill.customerCode, Is.EqualTo(CustomerCode.ToUpperInvariant()));
            Assert.That(bill.customerName, Is.EqualTo(UpdatedCustomerName));
            Assert.That(bill.customerPhone, Is.EqualTo(CustomerPhone));
            Assert.That(bill.description, Is.EqualTo(Description));
            Assert.That(bill.paymentStatus, Is.EqualTo(0));
            Assert.That(NormalizePaymentCode(bill.wbcCode), Is.EqualTo(NormalizePaymentCode(paymentCode)));
            Assert.That(decimal.Parse(bill.amount), Is.EqualTo(decimal.Parse(UpdatedAmount)).Within(0.001m));
            Assert.That(bill.updateTimeStamp, Is.Not.Empty);
        }

        static string NormalizePaymentCode(string paymentCode) => Regex.Replace(paymentCode ?? "", @"\D+", "");

        static IEnumerable<object[]> SdkEndpointQueryCases()
        {
            yield return new object[] { "CreateBillAsync", "einvoice/api/bill", null };
            yield return new object[] { "UpdateBillAsync", "einvoice/api/bill", null };
            yield return new object[] { "DeleteBillAsync", "einvoice/api/bill", new Dictionary<string, string> { { "wbc_code", "123 456 789" } } };
            yield return new object[] { "GetPaymentStatusAsync", "einvoice/api/paymentStatus", new Dictionary<string, string> { { "wbc_code", "123 456 789" } } };
            yield return new object[] { "GetBillByReferenceAsync", "einvoice/api/bill", new Dictionary<string, string> { { "bill_reference", "dotnet/unit/1" } } };
            yield return new object[] { "GetBillByPaymentCodeAsync", "einvoice/api/bill", new Dictionary<string, string> { { "wbc_code", "123 456 789" } } };
            yield return new object[] { "GetBillsAsync", "einvoice/api/bills", new Dictionary<string, string> { { "payment_status", "-1" }, { "last_timestamp", "20251231" }, { "limit", "10" } } };
            yield return new object[] { "GetPaymentsAsync", "einvoice/api/payments", new Dictionary<string, string> { { "last_timestamp", "20251231" }, { "limit", "10" } } };
            yield return new object[] { "GetStatAsync", "merchant/stat", new Dictionary<string, string> { { "date_from", "2025-01-01" }, { "date_to", "2025-01-02" } } };
            yield return new object[] { "GetSupportedBanksAsync", "einvoice/api/banks", null };
        }

        static string BuildUrl(WeBirrClient api, string path, Dictionary<string, string> parameters)
        {
            var method = typeof(WeBirrClient).GetMethod("BuildUrl", BindingFlags.Instance | BindingFlags.NonPublic);
            return (string)method.Invoke(api, new object[] { path, parameters });
        }

        static void InvokePrepareBill(WeBirrClient api, Bill bill)
        {
            var method = typeof(WeBirrClient).GetMethod("PrepareBill", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(api, new object[] { bill });
        }

        static void WithGatewayUrl(string value, Action action)
        {
            var previous = Environment.GetEnvironmentVariable("GATEWAY_URL");
            Environment.SetEnvironmentVariable("GATEWAY_URL", value);

            try
            {
                action();
            }
            finally
            {
                Environment.SetEnvironmentVariable("GATEWAY_URL", previous);
            }
        }

        sealed class StubHttpMessageHandler : HttpMessageHandler
        {
            readonly string _responseBody;

            public StubHttpMessageHandler(string responseBody)
            {
                _responseBody = responseBody;
            }

            public List<HttpRequestMessage> Requests { get; } = new List<HttpRequestMessage>();

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Requests.Add(request);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
                });
            }
        }

        static string CursorBefore(string updateTimeStamp)
        {
            var baseValue = (updateTimeStamp ?? "").Length >= 14 ? updateTimeStamp.Substring(0, 14) : "";
            if (!DateTime.TryParseExact(baseValue, "yyyyMMddHHmmss", null, DateTimeStyles.AssumeUniversal, out var date))
            {
                return "";
            }

            return date.AddSeconds(-1).ToString("yyyyMMddHHmmss");
        }
    }
}
