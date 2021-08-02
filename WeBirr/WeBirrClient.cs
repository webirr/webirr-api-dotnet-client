using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace WeBirr
{
    /// <summary>
    /// A WeBirrClient instance object can be used to
    /// Create, Update or Delete a Bill at WeBirr Servers and also to
    /// Get the Payment Status of a bill.
    /// It is a wrapper for the REST Web Service API.
    /// </summary>
    public class WeBirrClient
    {
        string _baseAddress;
        string _apiKey;

        public WeBirrClient(string apikey, bool isTestEnv)
        {
            _apiKey = apikey;
            _baseAddress = isTestEnv ? "https://api.webirr.com" : "https://api.webirr.com:8080";
        }
        /// <summary>
        /// Create a new bill at WeBirr Servers.
        /// Check if(ApiResponse.error == null) to see if there are errors.
        /// ApiResponse.res will have the value of the returned PaymentCode on success.
        /// </summary>
        /// <param name="bill">Bill object to be created</param>
        /// <returns></returns>
        public async Task<ApiResponse<String>> CreateBill(Bill bill)
        {
            var client = new HttpClient();

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var resp = await client.PostAsJsonAsync($"{_baseAddress}/einvoice/api/postbill?api_key={_apiKey}", bill);

            if (resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ApiResponse<string>>(body);
            }
            else return new ApiResponse<String> { error = $"http error {resp.StatusCode} {resp.ReasonPhrase}" };

        }

        /// Update an existing bill at WeBirr Servers, if the bill is not paid yet.
        /// The billReference has to be the same as the original bill created.
        /// Check if(ApiResponse.error == null) to see if there are errors.
        /// ApiResponse.res will have the value of "OK" on success.
        public async Task<ApiResponse<String>> UpdateBill(Bill bill)
        {
            var client = new HttpClient();

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var resp = await client.PutAsJsonAsync($"{_baseAddress}/einvoice/api/postbill?api_key={_apiKey}", bill);

            if (resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ApiResponse<string>>(body);
            }
            else return new ApiResponse<String> { error = $"http error {resp.StatusCode} {resp.ReasonPhrase}" };

        }

        /// Delete an existing bill at WeBirr Servers, if the bill is not paid yet.
        /// [paymentCode] is the number that WeBirr Payment Gateway returns on createBill.
        /// Check if(ApiResponse.error == null) to see if there are errors.
        /// ApiResponse.res will have the value of "OK" on success.
        public async Task<ApiResponse<String>> DeleteBill(String paymentCode)
        {
            var client = new HttpClient();

            var resp = await client.PutAsync($"{_baseAddress}/einvoice/api/deletebill?api_key={_apiKey}&wbc_code={paymentCode}", null);

            if (resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ApiResponse<string>>(body);
            }
            else return new ApiResponse<String> { error = $"http error {resp.StatusCode} {resp.ReasonPhrase}" };

        }

        /// Get Payment Status of a bill from WeBirr Servers
        /// [paymentCode] is the number that WeBirr Payment Gateway returns on createBill.
        /// Check if(ApiResponse.error == null) to see if there are errors.
        /// ApiResponse.res will have `Payment` object on success (will be null otherwise!)
        /// ApiResponse.res?.isPaid ?? false -> will return true if the bill is paid (payment completed)
        /// ApiResponse.res?.data ?? null -> will have `PaymentDetail` object
        public async Task<ApiResponse<Payment>> GetPaymentStatus(String paymentCode)
        {
            var client = new HttpClient();

            var resp = await client.GetAsync($"{_baseAddress}/einvoice/api/getPaymentStatus?api_key={_apiKey}&wbc_code={paymentCode}");

            if (resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ApiResponse<Payment>>(body);
            }
            else return new ApiResponse<Payment> { error = $"http error {resp.StatusCode} {resp.ReasonPhrase}" };
        }

    }

}
