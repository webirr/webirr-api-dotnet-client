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
            else return new ApiResponse<String>  { error = $"http error {resp.StatusCode} {resp.ReasonPhrase}" };
            
        }

    }

}
