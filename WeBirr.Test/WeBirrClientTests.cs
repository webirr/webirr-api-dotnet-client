using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using WeBirr;

namespace WeBirr.Test
{
    public class WeBirrClientTests
    {
        const string CreatedAmount = "270.90";
        const string UpdatedAmount = "278.00";
        const string CustomerCode = "sdk-test-customer";
        const string CreatedCustomerName = "SDK Test Customer";
        const string UpdatedCustomerName = "SDK Test Customer Updated";
        const string CustomerPhone = "0911000000";
        const string Description = "SDK Test Bill";

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
        public async Task Preferred_constructor_sets_bill_merchant_id_before_sending()
        {
            var bill = SampleBill("dotnet/unit/" + Guid.NewGuid());
            var api = new WeBirrClient("merchant-from-client", "x", true);

            await api.CreateBillAsync(bill);

            Assert.That(bill.merchantID, Is.EqualTo("merchant-from-client"));
        }

        [Test]
        public async Task Merchant_scoped_methods_require_preferred_constructor()
        {
            var api = new WeBirrClient("x", true);

            var bill = await api.GetBillByReferenceAsync("missing-reference");
            var bills = await api.GetBillsAsync();
            var payments = await api.GetPaymentsAsync();
            var stat = await api.GetStatAsync("2025-01-01", "2025-01-02");

            AssertApiError(bill);
            AssertApiError(bills);
            AssertApiError(payments);
            AssertApiError(stat);
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
                    ""merchantID"": ""0305"",
                    ""wbcCode"": ""123 456 789"",
                    ""paymentStatus"": 0,
                    ""updateTimeStamp"": ""2026061210000000000""
                }
            }";

            var response = JsonConvert.DeserializeObject<ApiResponse<BillResponse>>(json);

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

            var response = JsonConvert.DeserializeObject<ApiResponse<List<PaymentResponse>>>(json);
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

            var response = JsonConvert.DeserializeObject<ApiResponse<Payment>>(json);

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

            var response = JsonConvert.DeserializeObject<ApiResponse<Stat>>(json);

            Assert.That(response.res.nBills, Is.EqualTo(10));
            Assert.That(response.res.nBillsPaid, Is.EqualTo(4));
            Assert.That(response.res.nBillsUnpaid, Is.EqualTo(6));
            Assert.That(response.res.amountBills, Is.EqualTo("100.00"));
            Assert.That(response.res.amountPaid, Is.EqualTo("40.00"));
            Assert.That(response.res.amountUnpaid, Is.EqualTo("60.00"));
        }

        [Test]
        [Category("TestEnv")]
        public async Task TestEnv_smoke_covers_bill_and_payment_endpoints()
        {
            var merchantId = Environment.GetEnvironmentVariable("WEBIRR_TEST_ENV_MERCHANT_ID") ?? "";
            var apiKey = Environment.GetEnvironmentVariable("WEBIRR_TEST_ENV_API_KEY") ?? "";

            if (string.IsNullOrEmpty(merchantId) || string.IsNullOrEmpty(apiKey))
            {
                Assert.Ignore("WEBIRR_TEST_ENV_MERCHANT_ID and WEBIRR_TEST_ENV_API_KEY are required for TestEnv smoke tests.");
            }

            var api = new WeBirrClient(merchantId, apiKey, true);
            var billReference = "dotnet/test/" + Guid.NewGuid();
            string paymentCode = null;

            try
            {
                var createBill = SampleBill(billReference);
                var create = await api.CreateBillAsync(createBill);
                AssertNoApiError(create);
                Assert.That(createBill.merchantID, Is.EqualTo(merchantId));
                paymentCode = create.res;
                Assert.That(paymentCode, Is.Not.Empty);
                Assert.That(paymentCode, Does.Match(@"^\d{3}\s\d{3}\s\d{3}$"));

                var updateBill = SampleBill(billReference);
                updateBill.amount = UpdatedAmount;
                updateBill.customerName = UpdatedCustomerName;
                var update = await api.UpdateBillAsync(updateBill);
                AssertNoApiError(update);
                Assert.That(update.res.ToLowerInvariant(), Is.EqualTo("ok"));

                var status = await api.GetPaymentStatusAsync(paymentCode);
                AssertNoApiError(status);
                Assert.That(status.res.status, Is.EqualTo(0));
                Assert.That(status.res.data, Is.Null);

                var byReference = await api.GetBillByReferenceAsync(billReference);
                AssertNoApiError(byReference);
                AssertBillMatchesExpected(byReference.res, merchantId, billReference, paymentCode);

                var byPaymentCode = await api.GetBillByPaymentCodeAsync(paymentCode);
                AssertNoApiError(byPaymentCode);
                AssertBillMatchesExpected(byPaymentCode.res, merchantId, billReference, paymentCode);

                var cursor = CursorBefore(byReference.res.updateTimeStamp);
                var bills = await api.GetBillsAsync(0, cursor, 100);
                AssertNoApiError(bills);
                Assert.That(bills.res, Is.Not.Null);
                var listedBill = bills.res.FirstOrDefault(b => b.billReference == billReference);
                Assert.That(listedBill, Is.Not.Null);
                AssertBillMatchesExpected(listedBill, merchantId, billReference, paymentCode);

                var payments = await api.GetPaymentsAsync("", 10);
                AssertNoApiError(payments);
                Assert.That(payments.res, Is.Not.Null);

                var stat = await api.GetStatAsync("2025-01-01", "2030-01-31");
                AssertNoApiError(stat);
                Assert.That(stat.res, Is.Not.Null);

                var delete = await api.DeleteBillAsync(paymentCode);
                AssertNoApiError(delete);
                Assert.That(delete.res.ToLowerInvariant(), Is.EqualTo("ok"));
                paymentCode = null;

                var deletedBill = await api.GetBillByReferenceAsync(billReference);
                AssertApiError(deletedBill);
            }
            finally
            {
                if (!string.IsNullOrEmpty(paymentCode))
                {
                    await api.DeleteBillAsync(paymentCode);
                }
            }
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

        static string CursorBefore(string updateTimeStamp)
        {
            var baseValue = (updateTimeStamp ?? "").Length >= 14 ? updateTimeStamp.Substring(0, 14) : "";
            if (!DateTime.TryParseExact(baseValue, "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.AssumeUniversal, out var date))
            {
                return "";
            }

            return date.AddSeconds(-1).ToString("yyyyMMddHHmmss");
        }
    }
}
