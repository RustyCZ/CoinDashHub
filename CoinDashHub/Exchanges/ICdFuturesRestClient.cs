using CoinDashHub.Model;

namespace CoinDashHub.Exchanges
{
    public interface ICdFuturesRestClient
    {
        Task<Balance> GetBalancesAsync(CancellationToken cancel = default);
        Task<Position[]> GetPositionsAsync(CancellationToken cancel = default);
        Task<ClosedPnlTrade[]> GetClosedProfitLossAsync(DateTime startTime, CancellationToken cancel = default);
    }
}