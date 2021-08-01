using System;
using System.Threading.Tasks;

namespace WeBirr.Example
{
    class Program
    {
        const string apikey = "YOUR_API_KEY";
        const string merchantId = "YOUR_MERCHANT_ID";

        static async Task Main(string[] args)
        {
            await CreateAndUpdateBill();
        }

        /// <summary>
        /// Creating a new Bill / Updating an existing Bill on WeBirr Servers
        /// </summary>
        public static async Task CreateAndUpdateBill()
        {
            var api = new WeBirrClient(apikey: apikey, isTestEnv: true);

            var bill = new Bill
            {
                amount = "270.90",
                customerCode = "cc01",  // it can be email address or phone number if you dont have customer code
                customerName = "Elias Haileselassie",
                time = "2021-07-22 22:14", // your bill time, always in this format
                description = "hotel booking",
                billReference = "dotnet/2021/130",  // your unique reference number
                merchantID = merchantId,
            };

            System.Console.WriteLine("Creating Bill...");

            var res = await api.CreateBill(bill);

            var paymentCode = "";

            if (res.error == null)
            {
                // success
                paymentCode = res.res ?? ""; // returns paymentcode such as 429 723 975
                System.Console.WriteLine($"Payment Code = {paymentCode}"); // we may want to save payment code in local db.

            }
            else
            {
                // fail
                System.Console.WriteLine($"error: {res.error}");
                System.Console.WriteLine($"errorCode: {res.errorCode}"); // can be used to handle specific busines error such as ERROR_INVLAID_INPUT_DUP_REF
            }

            // update existing bill if it is not paid
            bill.amount = "278.00";
            bill.customerName = "Elias dotnet";
            //bill.billReference = "WE CAN NOT CHANGE THIS";

            /*
            System.Console.WriteLine("Updating Bill...");
            res = await api.UpdateBill(bill);

            if (res.error == null)
            {
                // success
                print(
                    'bill is updated succesfully'); //res.res will be 'OK'  no need to check here!
            }
            else
            {
                // fail
                print('error: ${res.error}');
                print(
                    'errorCode: ${res.errorCode}'); // can be used to handle specific busines error such as ERROR_INVLAID_INPUT
            }
            */

        }

    }
}
