using CoinDashHub.Model;

namespace CoinDashHub.Mapping
{
    public static class MappingHelpers
    {
        public static Balance ToBalance(this Bybit.Net.Objects.Models.V5.BybitAssetBalance balance)
        {
            return new Balance(
                balance.Equity,
                balance.WalletBalance,
                balance.UnrealizedPnl,
                balance.RealizedPnl);
        }

        public static Position? ToPosition(this Bybit.Net.Objects.Models.V5.BybitPosition value)
        {
            if (!value.AveragePrice.HasValue)
                return null;
            var position = new Position
            {
                AveragePrice = value.AveragePrice.Value,
                Quantity = value.Quantity,
                Side = value.Side.ToPositionSide(),
                Symbol = value.Symbol,
                TradeMode = value.TradeMode.ToTradeMode(),
                CreateTime = value.CreateTime,
                UpdateTime = value.UpdateTime,
                UnrealizedPnl = value.UnrealizedPnl
            };

            if (position.UpdateTime < position.CreateTime)
                position.UpdateTime = position.CreateTime;

            return position;
        }

        public static ClosedPnlTrade ClosedPnlTrade(this Bybit.Net.Objects.Models.V5.BybitClosedPnl value)
        {
            var trade = new ClosedPnlTrade
            {
                Symbol = value.Symbol,
                CreateTime = value.CreateTime,
                UpdateTime = value.UpdateTime,
                ClosedPnl = value.ClosedPnl,
                OrderId = value.OrderId,
            };

            if (trade.UpdateTime < trade.CreateTime)
                trade.UpdateTime = trade.CreateTime;

            return trade;
        }

        public static PositionSide ToPositionSide(this Bybit.Net.Enums.PositionSide? value)
        {
            if (!value.HasValue)
                return PositionSide.None;
            switch (value)
            {
                case Bybit.Net.Enums.PositionSide.Buy:
                    return PositionSide.Buy;
                case Bybit.Net.Enums.PositionSide.Sell:
                    return PositionSide.Sell;
                case Bybit.Net.Enums.PositionSide.None:
                    return PositionSide.None;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }

        public static TradeMode ToTradeMode(this Bybit.Net.Enums.TradeMode value)
        {
            switch (value)
            {
                case Bybit.Net.Enums.TradeMode.CrossMargin:
                    return TradeMode.CrossMargin;
                case Bybit.Net.Enums.TradeMode.Isolated:
                    return TradeMode.Isolated;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }
    }
}
