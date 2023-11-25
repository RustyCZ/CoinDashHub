using CoinDashHub.Accounts;

namespace CoinDashHub.Services
{
    public class CoinDashDataService : IHostedService
    {
        private readonly IAccountDataProvider[] m_accountDataProviders;

        public CoinDashDataService(IEnumerable<IAccountDataProvider> accountDataProviders)
        {
            m_accountDataProviders = accountDataProviders.ToArray();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var accountDataProvider in m_accountDataProviders)
                await accountDataProvider.StartAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var accountDataProvider in m_accountDataProviders)
                await accountDataProvider.StopAsync(cancellationToken);
        }
    }
}
