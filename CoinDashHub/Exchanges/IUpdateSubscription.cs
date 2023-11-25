namespace CoinDashHub.Exchanges
{
    public interface IUpdateSubscription
    {
        event Action? ConnectionLost;
        void AutoReconnect(ILogger logger);
        Task CloseAsync();
    }
}