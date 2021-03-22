using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ProtoBuf.Grpc.Client;
using Service.Bitgo.WithdrawalProcessor.Client;
using Service.Bitgo.WithdrawalProcessor.Grpc.Models;

namespace TestApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            GrpcClientFactory.AllowUnencryptedHttp2 = true;

            Console.Write("Press enter to start");
            Console.ReadLine();

            var factory = new BitgoWithdrawalProcessorClientFactory("http://localhost:80");
            var client = factory.GetCryptoWithdrawalService();

            var resp = await  client.ValidateAddressAsync(new ValidateAddressRequest()
            {
                BrokerId = "jetwallet",
                AssetSymbol = "BTC",
                Address = "123123"
            });
            Console.WriteLine(JsonConvert.SerializeObject(resp));


            Console.WriteLine();
            Console.WriteLine();

            var withdrawal = new CryptoWithdrawalRequest()
            {
                BrokerId = "jetwallet",
                ClientId = "alex",
                WalletId = "SP-alex",
                AssetSymbol = "BTC",
                Amount = 0.0001,
                ToAddress = "2N2VajawMvfKjhDnaPw1LLNUDVZRyzemXCC",
                RequestId = Guid.NewGuid().ToString("N")
            };

            var cashout = await client.CryptoWithdrawalAsync(withdrawal);
            Console.WriteLine(JsonConvert.SerializeObject(cashout));

            Console.WriteLine();
            Console.WriteLine();

            cashout = await client.CryptoWithdrawalAsync(withdrawal);
            Console.WriteLine(JsonConvert.SerializeObject(cashout));

            Console.WriteLine("End");
            Console.ReadLine();
        }

        
    }
}
