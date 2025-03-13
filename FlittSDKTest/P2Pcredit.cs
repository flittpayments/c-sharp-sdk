using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FlittSDK;
using FlittSDK.P2pcredit;

namespace FlittSDKTest
{
    [TestClass]
    public class P2PcreditTest
    {
        public int MerchantId = 1549901;
        public string SecretKey = "test";
        public string CreditKey = "testcredit";
        public string ContentType = "form";
        public string Endpoint = "pay.flitt.com";
        public string card_number = "4444555511116666";

        [TestMethod]
        public void P2PTest()
        {
            Config.MerchantId = MerchantId;
            Config.SecretKey = SecretKey;
            Config.CreditKey = CreditKey;
            Config.ContentType = ContentType;
            Config.Endpoint(Endpoint);
            string oID = Guid.NewGuid().ToString();
            var req = new P2PcreditRequest()
            {
                order_id = oID,
                amount = 10000,
                order_desc = "Checking! checkout tests",
                currency = "GEL",
                receiver_card_number = card_number
            };
            var resp = new P2Pcredit().Post(req);

            Assert.IsNotNull(resp);
            Assert.AreEqual(oID, resp.order_id);
            Assert.IsNotNull(resp.order_status);
        }
    }
}