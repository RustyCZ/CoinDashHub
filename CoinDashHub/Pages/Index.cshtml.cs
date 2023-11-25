using CoinDashHub.Accounts;
using CoinDashHub.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CoinDashHub.Pages
{
    public class IndexModel : PageModel
    {
        private IAccountDataProvider[] m_accountDataProviders;

        public IndexModel(IEnumerable<IAccountDataProvider> accountDataProviders)
        {
            m_accountDataProviders = accountDataProviders.ToArray();
        }

        public PositionViewModel[] Positions { get; set; } = Array.Empty<PositionViewModel>();

        public AccountExposureViewModel[] AccountExposures { get; set; } = Array.Empty<AccountExposureViewModel>();

        public decimal TotalWalletExposure { get; set; }

        public async void OnGet()
        {
            var positions = new List<PositionViewModel>();
            var accountExposures = new List<AccountExposureViewModel>();
            var totalWalletBalance = 0m;
            var totalPositionValue = 0m;
            foreach (var accountDataProvider in m_accountDataProviders)
            {
                var accountPositionViewModels = new List<PositionViewModel>();
                var balance = await accountDataProvider.GetBalancesAsync();
                var accountPositionValue = 0m;
                if(balance.WalletBalance.HasValue)
                    totalWalletBalance += balance.WalletBalance.Value;
                var accountPositions = await accountDataProvider.GetPositionsAsync();
                foreach (var position in accountPositions)
                {
                    var positionValue = position.Quantity * position.AveragePrice;
                    accountPositionValue += positionValue;
                    accountPositionViewModels.Add(new PositionViewModel
                    {
                        Position = position,
                        AccountName = accountDataProvider.AccountName,
                        PositionValue = positionValue,
                        WalletExposure = balance.WalletBalance > 0
                            ? Math.Round((positionValue / balance.WalletBalance.Value) * 100, 1)
                            : 0,
                    });
                    totalPositionValue += positionValue;
                }
                accountExposures.Add(new AccountExposureViewModel
                {
                    AccountName = accountDataProvider.AccountName,
                    WalletExposure = balance.WalletBalance > 0
                        ? Math.Round((accountPositionValue / balance.WalletBalance.Value) * 100, 1)
                        : 0,
                });
                positions.AddRange(accountPositionViewModels.OrderByDescending(x => x.WalletExposure));
            }
            TotalWalletExposure = totalWalletBalance > 0
                ? Math.Round((totalPositionValue / totalWalletBalance) * 100, 1)
                : 0;

            Positions = positions.ToArray();
            AccountExposures = accountExposures.ToArray();
        }

        public class PositionViewModel
        {
            public Position Position { get; set; } = new Position();
            public string AccountName { get; set; } = string.Empty;
            public decimal PositionValue { get; set; }
            public decimal WalletExposure { get; set; }
        }

        public class AccountExposureViewModel
        {
            public string AccountName { get; set; } = string.Empty;
            public decimal WalletExposure { get; set; }
        }
    }
}