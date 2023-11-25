using CoinDashHub.Accounts;
using CoinDashHub.Model;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CoinDashHub.Pages
{
    public class BalancesModel : PageModel
    {
        private IAccountDataProvider[] m_accountDataProviders;

        public BalancesModel(IEnumerable<IAccountDataProvider> accountDataProviders)
        {
            m_accountDataProviders = accountDataProviders.ToArray();
        }

        public BalancePerAccount[] Balances { get; set; } = Array.Empty<BalancePerAccount>();

        public async void OnGet()
        {
            var balances = new List<BalancePerAccount>();
            foreach (var accountDataProvider in m_accountDataProviders)
            {
                var balance = await accountDataProvider.GetBalancesAsync();
                balances.Add(new BalancePerAccount
                {
                    Name = accountDataProvider.AccountName, 
                    Balance = balance
                });
            }

            var totalEquity = balances.Select(x => x.Balance.Equity).Sum();
            var totalBalance = balances.Select(x => x.Balance.WalletBalance).Sum();
            var totalProfit = balances.Select(x => x.Balance.RealizedPnl).Sum();
            var totalUnrealizedProfit = balances.Select(x => x.Balance.UnrealizedPnl).Sum();
            balances.Add(new BalancePerAccount
            {
                Name = "Total",
                Balance = new Balance
                {
                    Equity = totalEquity,
                    WalletBalance = totalBalance,
                    RealizedPnl = totalProfit,
                    UnrealizedPnl = totalUnrealizedProfit
                }
            });

            Balances = balances.ToArray();
        }

        public class BalancePerAccount
        {
            public string Name { get; set; } = string.Empty;
            public Balance Balance { get; set; }
        }
    }
}