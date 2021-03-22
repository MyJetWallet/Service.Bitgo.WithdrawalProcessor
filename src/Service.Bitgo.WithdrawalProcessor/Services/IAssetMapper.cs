using MyNoSqlServer.Abstractions;
using Service.Bitgo.WithdrawalProcessor.NoSql;

namespace Service.Bitgo.WithdrawalProcessor.Services
{
    public interface IAssetMapper
    {
        (string, string) AssetToBitgoCoinAndWalletAsync(string brokerId, string assetSymbol);
        long ConvertAmountToBitgo(string coin, double amount);
    }

    public class AssetMapper : IAssetMapper
    {
        private readonly IMyNoSqlServerDataReader<BitgoAssetMapEntity> _assetMap;
        private readonly IMyNoSqlServerDataReader<BitgoCoinEntity> _bitgoCoins;

        public AssetMapper(IMyNoSqlServerDataReader<BitgoAssetMapEntity> assetMap, IMyNoSqlServerDataReader<BitgoCoinEntity> bitgoCoins)
        {
            _assetMap = assetMap;
            _bitgoCoins = bitgoCoins;
        }

        public (string, string) AssetToBitgoCoinAndWalletAsync(string brokerId, string assetSymbol)
        {
            var map = _assetMap.Get(BitgoAssetMapEntity.GeneratePartitionKey(brokerId), BitgoAssetMapEntity.GenerateRowKey(assetSymbol));

            if (map == null)
            {
                return (string.Empty, string.Empty);
            }

            return (map.BitgoCoin, map.BitgoWalletId);
        }

        public long ConvertAmountToBitgo(string coin, double amount)
        {
            var coinSettings = _bitgoCoins.Get(BitgoCoinEntity.GeneratePartitionKey(), BitgoCoinEntity.GenerateRowKey(coin));

            if (coinSettings == null)
            {
                throw new System.Exception($"Do not found settings for bitgo coin {coin} in nosql table {BitgoCoinEntity.TableName}");
            }

            return coinSettings.AmountToAbsoluteValue(amount);
        }
    }
}