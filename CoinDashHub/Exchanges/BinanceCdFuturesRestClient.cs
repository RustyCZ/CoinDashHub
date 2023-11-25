using Binance.Net.Enums;
using Binance.Net.Interfaces.Clients;
using Bybit.Net.Enums;
using CoinDashHub.Helpers;
using CoinDashHub.Mapping;
using CoinDashHub.Model;

namespace CoinDashHub.Exchanges
{
    public class BinanceCdFuturesRestClient : ICdFuturesRestClient
    {
        private readonly IBinanceRestClient m_client;

        public BinanceCdFuturesRestClient(IBinanceRestClient client)
        {
            m_client = client;
        }

        public async Task<Balance> GetBalancesAsync(CancellationToken cancel = default)
        {
            var balance = await ExchangePolicies.RetryForever
                .ExecuteAsync(async () =>
                {
                    var balanceResult = await m_client.UsdFuturesApi.Account.GetBalancesAsync(null, cancel);
                    if (balanceResult.GetResultOrError(out var data, out var error))
                        return data;
                    throw new InvalidOperationException(error.Message);
                });
            foreach (var b in balance)
            {
                if (b.Asset == Assets.QuoteAsset)
                {
                    var contract = b.ToBalance();
                    return contract;
                }
            }

            return new Balance(0, 0, 0, 0);
        }

        public async Task<Position[]> GetPositionsAsync(CancellationToken cancel = default)
        {
            var positions = await ExchangePolicies.RetryForever.ExecuteAsync(async () =>
            {
                List<Position> binancePositions = new List<Position>();
                var positionResult =
                    await m_client.UsdFuturesApi.Account.GetPositionInformationAsync(null, null, cancel);
                if (!positionResult.GetResultOrError(out var data, out var error))
                    throw new InvalidOperationException(error.Message);
                foreach (var binancePosition in data)
                {
                    if (binancePosition.Quantity <= 0)
                        continue;
                    var position = binancePosition.ToPosition();
                    binancePositions.Add(position);
                }

                return binancePositions.ToArray();
            });

            return positions;
        }

        public async Task<ClosedPnlTrade[]> GetClosedProfitLossAsync(DateTime startTime,
            CancellationToken cancel = default)
        {
            Dictionary<string, ClosedPnlTrade> closedPnlTrades = new Dictionary<string, ClosedPnlTrade>();
            bool moreData = true;
            while (moreData)
            {
                var closedTrades = await ExchangePolicies.RetryForever.ExecuteAsync(async () =>
                {
                    Dictionary<string, ClosedPnlTrade> innerTrades = new Dictionary<string, ClosedPnlTrade>();
                    var incomeRecordsResult =
                        await m_client.UsdFuturesApi.Account.GetIncomeHistoryAsync(startTime: startTime, limit: 1000,
                            ct: cancel);
                    if (!incomeRecordsResult.GetResultOrError(out var data, out var error))
                        throw new InvalidOperationException(error.Message);
                    var dataArr = data.ToArray();
                    foreach (var incomeRecord in dataArr)
                    {
                        if (!incomeRecord.IncomeType.HasValue)
                            continue;
                        if (incomeRecord.IncomeType.Value == IncomeType.RealizedPnl 
                            || incomeRecord.IncomeType.Value == IncomeType.FundingFee
                            || incomeRecord.IncomeType.Value == IncomeType.Commission)
                        {
                            var closedTrade = incomeRecord.ToClosedPnlTrade();
                            if (closedTrade != null)
                                innerTrades[closedTrade.OrderId] = closedTrade;
                        }
                    }
                    var needMoreData = dataArr.Length >= 1000;

                    return (innerTrades, needMoreData);
                });
                foreach (var closedTrade in closedTrades.Item1)
                    closedPnlTrades[closedTrade.Key] = closedTrade.Value;
                moreData = closedTrades.Item2;
                if (moreData)
                {
                    startTime = closedPnlTrades.MaxBy(x => x.Value.UpdateTime).Value.UpdateTime;
                }
            }

            return closedPnlTrades.Values.ToArray();
        }
    }
}