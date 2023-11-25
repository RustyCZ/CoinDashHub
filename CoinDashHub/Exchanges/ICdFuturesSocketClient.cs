using CoinDashHub.Model;

namespace CoinDashHub.Exchanges
{
    public interface ICdFuturesSocketClient
    {
        Task<IUpdateSubscription> SubscribeToWalletUpdatesAsync(Action<Balance> handler,
            CancellationToken cancel = default);

        Task<IUpdateSubscription> SubscribeToPositionUpdatesAsync(Action<Position> handler,
            CancellationToken cancel = default);
    }
}