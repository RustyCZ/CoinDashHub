using CoinDashHub.Exchanges;
using CoinDashHub.Model;
using Nito.AsyncEx;

namespace CoinDashHub.Accounts
{
    public class AccountDataProvider : IAccountDataProvider
    {
        private readonly ICdFuturesRestClient m_restClient;
        private readonly ICdFuturesSocketClient m_socketClient;
        private CancellationTokenSource? m_cancellationTokenSource;
        private Task? m_updateTask;
        private readonly ILogger<AccountDataProvider> m_logger;
        private readonly List<IUpdateSubscription> m_subscriptions;
        private readonly AsyncLock m_lock;
        private readonly Dictionary<SymbolPosition, Position> m_positions;
        private readonly Dictionary<string, ClosedPnlTrade> m_closedTrades;
        private readonly TimeSpan m_oldestClosedTrade = TimeSpan.FromDays(5);

        public AccountDataProvider(string accountName, ICdFuturesRestClient restClient,
            ICdFuturesSocketClient socketClient, ILogger<AccountDataProvider> logger)
        {
            m_lock = new AsyncLock();
            m_subscriptions = new List<IUpdateSubscription>();
            m_positions = new Dictionary<SymbolPosition, Position>();
            m_restClient = restClient;
            m_socketClient = socketClient;
            m_logger = logger;
            AccountName = accountName;
            Positions = Array.Empty<Position>();
            m_closedTrades = new Dictionary<string, ClosedPnlTrade>();
        }

        public string AccountName { get; }

        protected Balance Contract { get; private set; }

        protected Position[] Positions { get; private set; }

        public Task StartAsync(CancellationToken cancel)
        {
            m_cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancel);
            m_updateTask = Task.Run(async () =>
            {
                var localCancel = m_cancellationTokenSource.Token;
                var subscription = await m_socketClient.SubscribeToWalletUpdatesAsync(OnWalletUpdate, localCancel);
                subscription.AutoReconnect(m_logger);
                subscription.ConnectionLost += async () =>
                {
                    await InitializeBalanceAsync(cancel);
                };
                m_subscriptions.Add(subscription);

                var positionUpdateSubscription = await m_socketClient.SubscribeToPositionUpdatesAsync(
                    OnPositionUpdate,
                    localCancel);
                positionUpdateSubscription.AutoReconnect(m_logger);
                positionUpdateSubscription.ConnectionLost += async () =>
                {
                    await InitializePositionsAsync(cancel);
                };

                await InitializeBalanceAsync(cancel);
                await InitializePositionsAsync(cancel);
                await InitializeClosedTradesAsync(cancel);

                while (!cancel.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), cancel);
                    try
                    {
                        await UpdateClosedTradesAsync(cancel);
                    }
                    catch (Exception e)
                    {
                        m_logger.LogWarning(e, "Failed to update closed trades");
                    }
                }

            }, cancel);

            return Task.CompletedTask;
        }

        private async Task InitializePositionsAsync(CancellationToken cancel)
        {
            var positions = await m_restClient.GetPositionsAsync(cancel);
            using var _ = await m_lock.LockAsync();
            foreach (Position position in positions)
                m_positions[new SymbolPosition(position.Symbol, position.Side)] = position;
            Positions = positions;
        }

        private async Task InitializeBalanceAsync(CancellationToken cancel)
        {
            var contract = await m_restClient.GetBalancesAsync(cancel);
            using var _ = await m_lock.LockAsync();
            Contract = contract;
        }

        private async Task InitializeClosedTradesAsync(CancellationToken cancel)
        {
            var closedTrades =
                await m_restClient.GetClosedProfitLossAsync(DateTime.UtcNow.Date.Add(-m_oldestClosedTrade), cancel);
            using var _ = await m_lock.LockAsync();
            foreach (var closedTrade in closedTrades)
                m_closedTrades[closedTrade.OrderId] = closedTrade;
            RemoveExpiredClosedTrades();
        }

        private async Task UpdateClosedTradesAsync(CancellationToken cancel)
        {
            DateTime startTime = DateTime.UtcNow.Date.Add(-m_oldestClosedTrade);
            using (await m_lock.LockAsync())
            {
                if (m_closedTrades.Count > 0)
                {
                    startTime = m_closedTrades.Values.OrderByDescending(x => x.CreateTime)
                        .Select(x => x.CreateTime).First();
                }
            }
            var closedTrades =
                await m_restClient.GetClosedProfitLossAsync(startTime, cancel);
            using var _ = await m_lock.LockAsync();
            foreach (var closedTrade in closedTrades)
                m_closedTrades[closedTrade.OrderId] = closedTrade;
            RemoveExpiredClosedTrades();
        }

        private void OnPositionUpdate(Position obj)
        {
            using var _ = m_lock.Lock();
            if (obj.Quantity <= 0)
                m_positions.Remove(new SymbolPosition(obj.Symbol, obj.Side));
            else
                m_positions[new SymbolPosition(obj.Symbol, obj.Side)] = obj;

            Positions = m_positions.Values.ToArray();
        }

        public async Task StopAsync(CancellationToken cancel)
        {
            using var _ = m_lock.Lock();
            foreach (var subscription in m_subscriptions)
                await subscription.CloseAsync();
            m_subscriptions.Clear();
            m_cancellationTokenSource?.Cancel();
            m_cancellationTokenSource?.Dispose();
        }

        public Task<Balance> GetBalancesAsync(CancellationToken cancel = default)
        {
            using var _ = m_lock.Lock();
            return Task.FromResult(Contract);
        }

        public Task<Position[]> GetPositionsAsync(CancellationToken cancel = default)
        {
            using var _ = m_lock.Lock();
            return Task.FromResult(Positions);
        }

        public Task<DailyPnl[]> GetDailyPnlAsync(CancellationToken cancel = default)
        {
            using var _ = m_lock.Lock();
            var closedTrades = m_closedTrades.Values.ToArray();
            var dailyPnl = closedTrades.GroupBy(x => x.UpdateTime.Date)
                .Select(x => new DailyPnl
                {
                    Date = x.Key,
                    Pnl = x.Sum(y => y.ClosedPnl)
                }).ToArray();
            return Task.FromResult(dailyPnl);
        }

        private void OnWalletUpdate(Balance obj)
        {
            using var _ = m_lock.Lock();
            Contract = obj;
        }

        private void RemoveExpiredClosedTrades()
        {
            var now = DateTime.UtcNow;
            var expired = now.Date.Add(-m_oldestClosedTrade);
            var expiredTrades = m_closedTrades.Where(x => x.Value.UpdateTime < expired).ToArray();
            foreach (var expiredTrade in expiredTrades)
                m_closedTrades.Remove(expiredTrade.Key);
        }

        // ReSharper disable NotAccessedPositionalProperty.Local
        private readonly record struct SymbolPosition(string Symbol, PositionSide Side);
        // ReSharper restore NotAccessedPositionalProperty.Local
    }
}