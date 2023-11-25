using Binance.Net.Interfaces.Clients;
using CoinDashHub.Model;

namespace CoinDashHub.Exchanges
{
    public class BinanceCdFuturesSocketClient : ICdFuturesSocketClient
    {
        private readonly IBinanceSocketClient m_client;

        public BinanceCdFuturesSocketClient(IBinanceSocketClient client)
        {
            m_client = client;
        }

        public Task<IUpdateSubscription> SubscribeToWalletUpdatesAsync(Action<Balance> handler,
            CancellationToken cancel = default)
        {
            return Task.FromResult<IUpdateSubscription>(new DummyUpdateSubscription());
        }

        public Task<IUpdateSubscription> SubscribeToPositionUpdatesAsync(Action<Position> handler,
            CancellationToken cancel = default)
        {
            return Task.FromResult<IUpdateSubscription>(new DummyUpdateSubscription());
        }

        private class DummyUpdateSubscription : IUpdateSubscription
        {
            public event Action? ConnectionLost;

            public void AutoReconnect(ILogger logger)
            {
            }

            public Task CloseAsync()
            {
                return Task.CompletedTask;
            }
        }
    }
}