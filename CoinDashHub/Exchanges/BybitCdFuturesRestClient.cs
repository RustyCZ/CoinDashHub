using Bybit.Net.Enums;
using Bybit.Net.Interfaces.Clients;
using CoinDashHub.Helpers;
using CoinDashHub.Mapping;
using CoinDashHub.Model;

namespace CoinDashHub.Exchanges
{
    public class BybitCdFuturesRestClient : ICdFuturesRestClient
    {
        private readonly IBybitRestClient m_bybitRestClient;
        private readonly Category m_category;
        private readonly ILogger<BybitCdFuturesRestClient> m_logger;

        public BybitCdFuturesRestClient(IBybitRestClient bybitRestClient, ILogger<BybitCdFuturesRestClient> logger)
        {
            m_bybitRestClient = bybitRestClient;
            m_logger = logger;
            m_category = Category.Linear;
        }

        public async Task<Balance> GetBalancesAsync(CancellationToken cancel = default)
        {
            var balance = await ExchangePolicies.RetryForever
                .ExecuteAsync(async () =>
                {
                    var balanceResult = await m_bybitRestClient.V5Api.Account.GetBalancesAsync(AccountType.Contract,
                        null,
                        cancel);
                    if (balanceResult.GetResultOrError(out var data, out var error))
                        return data;
                    throw new InvalidOperationException(error.Message);
                });
            foreach (var b in balance.List)
            {
                if (b.AccountType == AccountType.Contract)
                {
                    var asset = b.Assets.FirstOrDefault(x =>
                        string.Equals(x.Asset, Assets.QuoteAsset, StringComparison.OrdinalIgnoreCase));
                    if (asset != null)
                    {
                        var contract = asset.ToBalance();
                        return contract;
                    }
                }
            }

            return new Balance();
        }

        public async Task<Position[]> GetPositionsAsync(CancellationToken cancel = default)
        {
            var positions = await ExchangePolicies.RetryForever.ExecuteAsync(async () =>
            {
                List<Position> positions = new List<Position>();
                string? cursor = null;
                while (true)
                {
                    var positionResult = await m_bybitRestClient.V5Api.Trading.GetPositionsAsync(
                        m_category,
                        settleAsset: Assets.QuoteAsset,
                        cursor: cursor,
                        ct: cancel);
                    if (!positionResult.GetResultOrError(out var data, out var error))
                        throw new InvalidOperationException(error.Message);
                    foreach (var bybitPosition in data.List)
                    {
                        var position = bybitPosition.ToPosition();
                        if (position == null)
                            m_logger.LogWarning($"Could not convert position for symbol: {bybitPosition.Symbol}");
                        else
                            positions.Add(position);
                    }

                    if (string.IsNullOrWhiteSpace(data.NextPageCursor))
                        break;
                    else
                        await Task.Delay(1000, cancel);
                    cursor = data.NextPageCursor;
                }

                return positions.ToArray();
            });

            return positions;
        }

        public async Task<ClosedPnlTrade[]> GetClosedProfitLossAsync(DateTime startTime, CancellationToken cancel = default)
        {
            var closedTrades = await ExchangePolicies.RetryForever.ExecuteAsync(async () =>
            {
                List<ClosedPnlTrade> closedPnlTrades = new List<ClosedPnlTrade>();
                string? cursor = null;
                while (true)
                {
                    var positionResult = await m_bybitRestClient.V5Api.Trading.GetClosedProfitLossAsync(
                        m_category,
                        startTime: startTime,
                        cursor: cursor,
                        limit: 200,
                        ct: cancel);
                    if (!positionResult.GetResultOrError(out var data, out var error))
                        throw new InvalidOperationException(error.Message);
                    foreach (var closedPnl in data.List)
                    {
                        var closedPnlTrade = closedPnl.ToClosedPnlTrade();
                        closedPnlTrades.Add(closedPnlTrade);
                    }

                    if (string.IsNullOrWhiteSpace(data.NextPageCursor))
                        break;
                    else
                        await Task.Delay(1000, cancel);
                    cursor = data.NextPageCursor;
                }

                return closedPnlTrades.ToArray();
            });

            return closedTrades.ToArray();
        }
    }
}
