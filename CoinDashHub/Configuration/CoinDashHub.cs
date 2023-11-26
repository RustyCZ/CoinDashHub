namespace CoinDashHub.Configuration
{
    public class CoinDashHub
    {
        public DashboardLogin DashboardLogin { get; set; } = new DashboardLogin();
        public List<Account> Accounts { get; set; } = new List<Account>();
    }
}