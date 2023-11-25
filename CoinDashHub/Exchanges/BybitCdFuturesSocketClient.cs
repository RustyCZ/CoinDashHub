using Bybit.Net.Enums;
using Bybit.Net.Interfaces.Clients;
using Bybit.Net.Objects.Models.V5;
using CoinDashHub.Helpers;
using CoinDashHub.Mapping;
using CoinDashHub.Model;

namespace CoinDashHub.Exchanges
{
    public class BybitCdFuturesSocketClient : ICdFuturesSocketClient
    {
        private readonly IBybitSocketClient m_bybitSocketClient;
        private const string c_asset = Assets.QuoteAsset;
        private readonly Category m_category;

        public BybitCdFuturesSocketClient(IBybitSocketClient bybitSocketClient)
        {
            m_category = Category.Linear;
            m_bybitSocketClient = bybitSocketClient;
        }

        public async Task<IUpdateSubscription> SubscribeToWalletUpdatesAsync(Action<Balance> handler, CancellationToken cancel = default)
        {
            var subscription = await ExchangePolicies.RetryForever
                .ExecuteAsync(async () =>
                {
                    var subscriptionResult = await m_bybitSocketClient.V5PrivateApi
                        .SubscribeToWalletUpdatesAsync(walletUpdateEvent =>
                        {
                            foreach (BybitBalance bybitBalance in walletUpdateEvent.Data)
                            {
                                if (bybitBalance.AccountType == AccountType.Contract)
                                {
                                    var asset = bybitBalance.Assets.FirstOrDefault(x => string.Equals(x.Asset, c_asset, StringComparison.OrdinalIgnoreCase));
                                    if (asset != null)
                                    {
                                        var contractBalance = asset.ToBalance();
                                        handler(contractBalance);
                                    }
                                }
                            }
                        }, cancel);
                    if (subscriptionResult.GetResultOrError(out var data, out var error))
                        return data;
                    throw new InvalidOperationException(error.Message);
                });
            return new BybitUpdateSubscription(subscription);
        }

        public async Task<IUpdateSubscription> SubscribeToPositionUpdatesAsync(Action<Position> handler,
            CancellationToken cancel = default)
        {
            var subscription = await ExchangePolicies.RetryForever
                .ExecuteAsync(async () =>
                {
                    var subscriptionResult = await m_bybitSocketClient.V5PrivateApi
                        .SubscribeToPositionUpdatesAsync(positionUpdateEvent =>
                        {
                            foreach (BybitPositionUpdate bybitPosition in positionUpdateEvent.Data)
                            {
                                if (bybitPosition.Category == m_category)
                                {
                                    var position = bybitPosition.ToPosition();
                                    if (position != null)
                                        handler(position);
                                }
                            }
                        }, cancel);
                    if (subscriptionResult.GetResultOrError(out var data, out var error))
                        return data;
                    throw new InvalidOperationException(error.Message);
                });
            return new BybitUpdateSubscription(subscription);
        }
    }
}