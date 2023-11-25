using CoinDashHub.Accounts;
using CoinDashHub.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CoinDashHub.Pages
{
    public class ProfitAndLossModel : PageModel
    {
        private IAccountDataProvider[] m_accountDataProviders;

        public ProfitAndLossModel(IEnumerable<IAccountDataProvider> accountDataProviders)
        {
            m_accountDataProviders = accountDataProviders.ToArray();
        }

        public DailyPnlViewModel[] ProfitAndLoss { get; set; } = Array.Empty<DailyPnlViewModel>();

        public async void OnGet()
        {
            var profitAndLoss = new List<DailyPnlViewModel>();
            foreach (var accountDataProvider in m_accountDataProviders)
            {
                var dailyProfitAndLosses = await accountDataProvider.GetDailyPnlAsync();
                foreach (var dailyPnl in dailyProfitAndLosses)
                {
                    var dailyPnlViewModel = new DailyPnlViewModel
                    {
                        AccountName = accountDataProvider.AccountName,
                        DailyPnl = dailyPnl
                    };
                    profitAndLoss.Add(dailyPnlViewModel);
                }
            }
            ProfitAndLoss = profitAndLoss
                .OrderByDescending(x => x.DailyPnl.Date)
                .ThenBy(x => x.AccountName)
                .ToArray();

            var total = new Dictionary<DateTime, DailyPnl>();
            foreach (var dailyPnlViewModel in ProfitAndLoss)
            {
                total.TryAdd(dailyPnlViewModel.DailyPnl.Date, new DailyPnl
                {
                    Date = dailyPnlViewModel.DailyPnl.Date
                });
                total[dailyPnlViewModel.DailyPnl.Date].Pnl += dailyPnlViewModel.DailyPnl.Pnl;
            }
            ProfitAndLoss = ProfitAndLoss
                .Concat(total.Select(x => new DailyPnlViewModel
                {
                    AccountName = "_Total",
                    DailyPnl = x.Value
                }))
                .OrderByDescending(x => x.DailyPnl.Date)
                .ThenBy(x => x.AccountName)
                .ToArray();
        }

        public class DailyPnlViewModel
        {
            public string AccountName { get; set; } = string.Empty;
            public DailyPnl DailyPnl { get; set; } = new DailyPnl();
        }
    }
}
