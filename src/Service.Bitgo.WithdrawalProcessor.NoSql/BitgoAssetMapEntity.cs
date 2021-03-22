using System.Dynamic;
using MyNoSqlServer.Abstractions;

namespace Service.Bitgo.WithdrawalProcessor.NoSql
{
    public class BitgoAssetMapEntity: MyNoSqlDbEntity
    {
        public const string TableName = "myjetwallet-map-asset-to-bitgo";

        public static string GeneratePartitionKey(string brokerId) => $"broker:{brokerId}";
        public static string GenerateRowKey(string assetSymbol) => $"asset:{assetSymbol}";

        public string BrokerId { get; set; }
        public string AssetSymbol { get; set; }
        public string BitgoWalletId { get; set; }
        public string BitgoCoin { get; set; }

        public static BitgoAssetMapEntity Create(string brokerId, string assetSymbol, string bitgoWalletId, string bitgoCoin)
        {
            var entity = new BitgoAssetMapEntity()
            {
                PartitionKey = GeneratePartitionKey(brokerId),
                RowKey = GenerateRowKey(assetSymbol),
                BrokerId = brokerId,
                AssetSymbol = assetSymbol,
                BitgoCoin = bitgoCoin,
                BitgoWalletId = bitgoWalletId
            };

            return entity;
        }
    }
}
