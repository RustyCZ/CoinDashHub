using CoinDashHub.Mapping;
using CryptoExchange.Net.Sockets;

namespace CoinDashHub.Exchanges
{
    public class BybitUpdateSubscription : IUpdateSubscription
    {
        private readonly UpdateSubscription m_subscription;

        public event Action? ConnectionLost;

        public BybitUpdateSubscription(UpdateSubscription subscription)
        {
            m_subscription = subscription;
            m_subscription.ConnectionLost += OnConnectionLost;
            m_subscription.Exception += _ => OnConnectionLost();
        }

        public void AutoReconnect(ILogger logger)
        {
            m_subscription.AutoReconnect(logger);
        }

        public async Task CloseAsync()
        {
            await m_subscription.CloseAsync();
        }

        protected void OnConnectionLost()
        {
            ConnectionLost?.Invoke();
        }
    }
}