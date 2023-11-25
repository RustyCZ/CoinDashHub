namespace CoinDashHub.Model
{
    public class ClosedPnlTrade
    {
        public string Symbol { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public decimal ClosedPnl { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }
    }
}