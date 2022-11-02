using System;
using System.Threading.Tasks;

namespace WeBirr.Example
{
    class Program
    {
        const string apikey = "YOUR_API_KEY";
        const string merchantId = "YOUR_MERCHANT_ID";

        //static readonly string apikey = Environment.GetEnvironmentVariable("wb_apikey_1") ?? "";
        //static readonly string merchantId = Environment.GetEnvironmentVariable("wb_merchid_1") ?? "";

        static async Task Main(string[] args)
        {
            await CreateAndUpdateBillAsync();
            await GetPaymentStatusAsync();
            await DeleteBillAsync();
        }

        /// Creating a new Bill / Updating an existing Bill on WeBirr Servers
        public static async Task CreateAndUpdateBillAsync()
        {
            var api = new WeBirrClient(apikey: apikey, isTestEnv: true);

            var bill = new Bill
            {
                amount = "270.90",
                customerCode = "cc01",  // it can be email address or phone number if you dont have customer code
                customerName = "Elias Haileselassie",
                time = "2021-07-22 22:14", // your bill time, always in this format
                description = "hotel booking",
                billReference = "dotnet/2021/132",  // your unique reference number
                merchantID = merchantId,
               // customerPhone = "0968817878" // to use sms notification merchant needs to be activated at WeBirr
            };

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

        /// Deleting an existing Bill from WeBirr Servers (if it is not paid)
        public static async Task DeleteBillAsync()
        {
            var api = new WeBirrClient(apikey: apikey, isTestEnv: true);

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

        /// Getting Payment status of an existing Bill from WeBirr Servers
        public static async Task GetPaymentStatusAsync()
        {
            var api = new WeBirrClient(apikey: apikey, isTestEnv: true);

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
