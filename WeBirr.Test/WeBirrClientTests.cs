using NUnit.Framework;
using System.Threading.Tasks;
using WeBirr;

namespace WeBirr.Test
{
    public class WeBirrClientTests
    {
        [Test]
        public async Task CreateBill_should_get_error_from_WebService_on_invalid_api_key_TestEnv()
        {
            var bill = sampleBill();
            var api = new WeBirrClient("x", true);

            var res = await api.CreateBill(bill);
            Assert.IsTrue(res.error.Length > 0);

        }

        Bill sampleBill() => new Bill
        {
            amount = "270.90",
            customerCode = "cc01",
            customerName = "Elias Haileselassie",
            time = "2021-07-22 22:14",
            description = "hotel booking",
            billReference = "dotnet/2021/130",
            merchantID = "x",
        };

    }
}