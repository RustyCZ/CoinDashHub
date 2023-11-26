using CoinDashHub.Model;

namespace CoinDashHub.Exchanges
{
    public class BinanceCdFuturesSocketClient : ICdFuturesSocketClient
    {
        private readonly ICdFuturesRestClient m_cdRestClient;
        private readonly ILogger<BinanceCdFuturesSocketClient> m_logger;

        public BinanceCdFuturesSocketClient(ICdFuturesRestClient cdRestClient, 
            ILogger<BinanceCdFuturesSocketClient> logger)
        {
            m_cdRestClient = cdRestClient;
            m_logger = logger;
        }

        public Task<IUpdateSubscription> SubscribeToWalletUpdatesAsync(Action<Balance> handler,
            CancellationToken cancel = default)
        {
            // fake it with rest api
            CancellationTokenSource cts = new CancellationTokenSource();
            var ctsCancel = cts.Token;
            Task updateTask = Task.Run(async () =>
            {
                while (!ctsCancel.IsCancellationRequested)
                {
                    try
                    {
                        var balance = await m_cdRestClient.GetBalancesAsync(ctsCancel);
                        handler(balance);
                    }
                    catch (Exception e)
                    {
                        m_logger.LogError(e, "Failed to get balance");
                    }
                    await Task.Delay(10000, ctsCancel);
                }
            }, ctsCancel);
            return Task.FromResult<IUpdateSubscription>(new DummyUpdateSubscription(updateTask, cts));
        }

        public Task<IUpdateSubscription> SubscribeToPositionUpdatesAsync(Action<Position> handler,
            CancellationToken cancel = default)
        {
            // fake it with rest api
            CancellationTokenSource cts = new CancellationTokenSource();
            var ctsCancel = cts.Token;
            Task updateTask = Task.Run(async () =>
            {
                while (!ctsCancel.IsCancellationRequested)
                {
                    try
                    {
                        var positions = await m_cdRestClient.GetPositionsAsync(ctsCancel);
                        foreach (var position in positions)
                            handler(position);
                    }
                    catch (Exception e)
                    {
                        m_logger.LogError(e, "Failed to get position");
                    }
                    await Task.Delay(10000, ctsCancel);
                }
            }, ctsCancel);
            return Task.FromResult<IUpdateSubscription>(new DummyUpdateSubscription(updateTask, cts));
        }

        private class DummyUpdateSubscription : IUpdateSubscription
        {
            public event Action? ConnectionLost;
            private readonly Task m_updateTask;
            private readonly CancellationTokenSource m_cts;

            public DummyUpdateSubscription(Task updateTask, CancellationTokenSource cts)
            {
                m_updateTask = updateTask;
                m_cts = cts;
            }

            public void AutoReconnect(ILogger logger)
            {
            }

            public async Task CloseAsync()
            {
                m_cts.Cancel();
                try
                {
                    await m_updateTask;
                }
                catch (OperationCanceledException)
                {
                }
            }
        }
    }
}