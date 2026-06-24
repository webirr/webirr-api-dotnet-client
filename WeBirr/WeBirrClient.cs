using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WeBirr
{
    /// <summary>
    /// A WeBirrClient instance object can be used to create, update or delete a
    /// bill at WeBirr servers and to retrieve bill/payment information.
    /// </summary>
    public class WeBirrClient
    {
        const string TestBaseAddress = "https://api.webirr.dev";
        const string ProdBaseAddress = "https://api.webirr.net:8080";

        readonly string _baseAddress;
        readonly string _merchantId;
        readonly string _apiKey;
        readonly HttpClient _client;
        static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public WeBirrClient(string merchantId, string apiKey, bool isTestEnv)
            : this(merchantId, apiKey, isTestEnv, new HttpClient())
        {
        }

        public WeBirrClient(string merchantId, string apiKey, bool isTestEnv, HttpClient httpClient)
        {
            _merchantId = merchantId ?? "";
            _apiKey = apiKey ?? "";
            _baseAddress = ResolveBaseAddress(isTestEnv);
            _client = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            EnsureJsonAcceptHeader(_client);
        }

        static string ResolveBaseAddress(bool isTestEnv)
        {
            if (!isTestEnv)
            {
                return ProdBaseAddress;
            }

            var gatewayUrl = Environment.GetEnvironmentVariable("GATEWAY_URL");
            if (!string.IsNullOrWhiteSpace(gatewayUrl))
            {
                return gatewayUrl.Trim().TrimEnd('/');
            }

            return TestBaseAddress;
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
            return await SendAsync<BillResponse>(
                HttpMethod.Get,
                "einvoice/api/bill",
                new Dictionary<string, string> { { "bill_reference", billReference } });
        }

        public async Task<ApiResponse<BillResponse>> GetBillByPaymentCodeAsync(string paymentCode)
        {
            return await SendAsync<BillResponse>(
                HttpMethod.Get,
                "einvoice/api/bill",
                new Dictionary<string, string> { { "wbc_code", paymentCode } });
        }

        public async Task<ApiResponse<List<BillResponse>>> GetBillsAsync(int paymentStatus = -1, string lastTimeStamp = "", int limit = 100)
        {
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
            return await SendAsync<Stat>(
                HttpMethod.Get,
                "merchant/stat",
                new Dictionary<string, string>
                {
                    { "date_from", dateFrom },
                    { "date_to", dateTo }
                });
        }

        public async Task<ApiResponse<List<SupportedBank>>> GetSupportedBanksAsync()
        {
            return await SendAsync<List<SupportedBank>>(HttpMethod.Get, "einvoice/api/banks");
        }

        void PrepareBill(Bill bill)
        {
            if (bill == null) return;
            bill.merchantID = _merchantId;
        }

        async Task<ApiResponse<T>> SendJsonAsync<T>(HttpMethod method, string path, object body) where T : class
        {
            var request = new HttpRequestMessage(method, BuildUrl(path));
            request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
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
            return JsonSerializer.Deserialize<ApiResponse<T>>(body, JsonOptions)
                ?? new ApiResponse<T> { error = "empty response" };
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
                { "api_key", _apiKey },
                { "merchant_id", _merchantId }
            };

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

        static void EnsureJsonAcceptHeader(HttpClient client)
        {
            if (!client.DefaultRequestHeaders.Accept.Any(header =>
                string.Equals(header.MediaType, "application/json", StringComparison.OrdinalIgnoreCase)))
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }
        }
    }
}
