using CoinDashHub.Model;

namespace CoinDashHub.Accounts
{
    public interface IAccountDataProvider
    {
        string AccountName { get; }

        Task StartAsync(CancellationToken cancel);

        Task StopAsync(CancellationToken cancel);

        Task<Balance> GetBalancesAsync(CancellationToken cancel = default);

        Task<Position[]> GetPositionsAsync(CancellationToken cancel = default);

        Task<DailyPnl[]> GetDailyPnlAsync(CancellationToken cancel = default);
    }
}
