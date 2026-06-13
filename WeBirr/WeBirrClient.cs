using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WeBirr
{
    /// <summary>
    /// A WeBirrClient instance object can be used to create, update or delete a
    /// bill at WeBirr servers and to retrieve bill/payment information.
    /// </summary>
    public class WeBirrClient
    {
        readonly string _baseAddress;
        readonly string _merchantId;
        readonly string _apiKey;
        readonly HttpClient _client;

        public WeBirrClient(string apikey, bool isTestEnv)
            : this("", apikey, isTestEnv)
        {
        }

        public WeBirrClient(string merchantId, string apiKey, bool isTestEnv)
        {
            _merchantId = merchantId ?? "";
            _apiKey = apiKey ?? "";
            _baseAddress = isTestEnv ? "https://api.webirr.net" : "https://api.webirr.net:8080";
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// Create a new bill at WeBirr servers.
        /// </summary>
        public async Task<ApiResponse<String>> CreateBillAsync(Bill bill)
        {
            PrepareBill(bill);
            return await SendJsonAsync<string>(HttpMethod.Post, "einvoice/api/bill", bill);
        }

        /// <summary>
        /// Update an existing bill at WeBirr servers, if the bill is not paid yet.
        /// The billReference has to be the same as the original bill created.
        /// </summary>
        public async Task<ApiResponse<String>> UpdateBillAsync(Bill bill)
        {
            PrepareBill(bill);
            return await SendJsonAsync<string>(HttpMethod.Put, "einvoice/api/bill", bill);
        }

        /// <summary>
        /// Delete an existing bill at WeBirr servers, if the bill is not paid yet.
        /// </summary>
        public async Task<ApiResponse<String>> DeleteBillAsync(String paymentCode)
        {
            return await SendAsync<string>(
                HttpMethod.Delete,
                "einvoice/api/bill",
                new Dictionary<string, string> { { "wbc_code", paymentCode } });
        }

        /// <summary>
        /// Get payment status of a bill from WeBirr servers.
        /// </summary>
        public async Task<ApiResponse<Payment>> GetPaymentStatusAsync(String paymentCode)
        {
            return await SendAsync<Payment>(
                HttpMethod.Get,
                "einvoice/api/paymentStatus",
                new Dictionary<string, string> { { "wbc_code", paymentCode } });
        }

        public async Task<ApiResponse<BillResponse>> GetBillByReferenceAsync(string billReference)
        {
            var merchantError = RequireMerchantId<BillResponse>();
            if (merchantError != null) return merchantError;

            return await SendAsync<BillResponse>(
                HttpMethod.Get,
                "einvoice/api/bill",
                new Dictionary<string, string> { { "bill_reference", billReference } });
        }

        public async Task<ApiResponse<BillResponse>> GetBillByPaymentCodeAsync(string paymentCode)
        {
            var merchantError = RequireMerchantId<BillResponse>();
            if (merchantError != null) return merchantError;

            return await SendAsync<BillResponse>(
                HttpMethod.Get,
                "einvoice/api/bill",
                new Dictionary<string, string> { { "wbc_code", paymentCode } });
        }

        public async Task<ApiResponse<List<BillResponse>>> GetBillsAsync(int paymentStatus = -1, string lastTimeStamp = "", int limit = 100)
        {
            var merchantError = RequireMerchantId<List<BillResponse>>();
            if (merchantError != null) return merchantError;

            return await SendAsync<List<BillResponse>>(
                HttpMethod.Get,
                "einvoice/api/bills",
                new Dictionary<string, string>
                {
                    { "payment_status", paymentStatus.ToString() },
                    { "last_timestamp", lastTimeStamp ?? "" },
                    { "limit", limit.ToString() }
                });
        }

        public async Task<ApiResponse<List<PaymentResponse>>> GetPaymentsAsync(string lastTimeStamp = "", int limit = 100)
        {
            var merchantError = RequireMerchantId<List<PaymentResponse>>();
            if (merchantError != null) return merchantError;

            return await SendAsync<List<PaymentResponse>>(
                HttpMethod.Get,
                "einvoice/api/payments",
                new Dictionary<string, string>
                {
                    { "last_timestamp", lastTimeStamp ?? "" },
                    { "limit", limit.ToString() }
                });
        }

        public async Task<ApiResponse<Stat>> GetStatAsync(string dateFrom, string dateTo)
        {
            var merchantError = RequireMerchantId<Stat>();
            if (merchantError != null) return merchantError;

            return await SendAsync<Stat>(
                HttpMethod.Get,
                "merchant/stat",
                new Dictionary<string, string>
                {
                    { "date_from", dateFrom },
                    { "date_to", dateTo }
                });
        }

        void PrepareBill(Bill bill)
        {
            if (bill == null) return;
            if (!string.IsNullOrEmpty(_merchantId))
            {
                bill.merchantID = _merchantId;
            }
        }

        async Task<ApiResponse<T>> SendJsonAsync<T>(HttpMethod method, string path, object body) where T : class
        {
            var request = new HttpRequestMessage(method, BuildUrl(path));
            request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            return await SendAsync<T>(request);
        }

        async Task<ApiResponse<T>> SendAsync<T>(HttpMethod method, string path, IDictionary<string, string> query = null) where T : class
        {
            return await SendAsync<T>(new HttpRequestMessage(method, BuildUrl(path, query)));
        }

        async Task<ApiResponse<T>> SendAsync<T>(HttpRequestMessage request) where T : class
        {
            var resp = await _client.SendAsync(request);
            if (!resp.IsSuccessStatusCode)
            {
                return new ApiResponse<T> { error = $"http error {resp.StatusCode} {resp.ReasonPhrase}" };
            }

            var body = await resp.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ApiResponse<T>>(body);
        }

        string BuildUrl(string path, IDictionary<string, string> parameters = null)
        {
            var query = Query(parameters);
            return $"{_baseAddress}/{path}?{query}";
        }

        string Query(IDictionary<string, string> parameters = null)
        {
            var query = new Dictionary<string, string>
            {
                { "api_key", _apiKey }
            };

            if (!string.IsNullOrEmpty(_merchantId))
            {
                query["merchant_id"] = _merchantId;
            }

            if (parameters != null)
            {
                foreach (var item in parameters)
                {
                    query[item.Key] = item.Value ?? "";
                }
            }

            return string.Join("&", query.Select(item =>
                $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value)}"));
        }

        ApiResponse<T> RequireMerchantId<T>() where T : class
        {
            return string.IsNullOrEmpty(_merchantId)
                ? new ApiResponse<T> { error = "merchant_id is required. Use WeBirrClient(string merchantId, string apiKey, bool isTestEnv)." }
                : null;
        }
    }
}
