using Bybit.Net.Clients;
using CoinDashHub.Accounts;
using CoinDashHub.Configuration;
using CoinDashHub.Exchanges;
using CoinDashHub.Helpers;
using CoinDashHub.Services;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Hosting;

namespace CoinDashHub
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddEnvironmentVariables("CD_");

            var configuration = builder.Configuration.GetSection("CoinDashHub").Get<Configuration.CoinDashHub>();
            if (configuration == null)
                throw new InvalidOperationException("Missing configuration.");

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddHostedService<CoinDashDataService>();
            builder.Services.AddSingleton<IEnumerable<IAccountDataProvider>>(sp =>
            {
                List<IAccountDataProvider> accountDataProviders = new List<IAccountDataProvider>();
                foreach (var account in configuration.Accounts)
                {
                    var accountDataProvider = CreateAccountDataProvider(account.Name, account, sp);
                    accountDataProviders.Add(accountDataProvider);
                }
                return accountDataProviders;
            });

            builder.Services.AddLogging(options =>
            {
                options.AddSimpleConsole(o =>
                {
                    o.UseUtcTimestamp = true;
                    o.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
                });
            });

            var app = builder.Build();

            var lf = app.Services.GetRequiredService<ILoggerFactory>();
            ApplicationLogging.LoggerFactory = lf;

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();
            app.MapRazorPages();
            app.Run();
        }

        private static AccountDataProvider CreateAccountDataProvider(string name, Account account, IServiceProvider services)
        {
            BybitRestClient client = new BybitRestClient(options =>
            {
                options.V5Options.RateLimitingBehaviour = RateLimitingBehaviour.Wait;
                options.V5Options.ApiCredentials = new ApiCredentials(account.ApiKey, account.ApiSecret);
                options.ReceiveWindow = TimeSpan.FromSeconds(10);
                options.AutoTimestamp = true;
                options.TimestampRecalculationInterval = TimeSpan.FromSeconds(10);
            });
            BybitSocketClient bybitSocketClient = new BybitSocketClient(options =>
            {
                options.V5Options.ApiCredentials = new ApiCredentials(account.ApiKey, account.ApiSecret);
            });
            var bybitCdFuturesRestClientLogger = services.GetRequiredService<ILogger<BybitCdFuturesRestClient>>();
            var cdFuturesRestClient = new BybitCdFuturesRestClient(client, bybitCdFuturesRestClientLogger);
            var cdFuturesSocketClient = new BybitCdFuturesSocketClient(bybitSocketClient);
            var lf = services.GetRequiredService<ILoggerFactory>();
            var accountDataProvider = new AccountDataProvider(name, cdFuturesRestClient, cdFuturesSocketClient, lf.CreateLogger<AccountDataProvider>());
            return accountDataProvider;
        }
    }
}