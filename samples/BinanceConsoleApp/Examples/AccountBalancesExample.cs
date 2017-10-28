﻿using Binance;
using Binance.Accounts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BinanceConsoleApp.Examples
{
    /// <summary>
    /// Demonstrate how to get current account balances, maintain a local cache
    /// and respond to real-time changes in account balances.
    /// 
    /// Usage: Change the 'Build Action' property to 'C# compiler' for this
    ///        class and change the sampe property to 'None' for Program.cs.
    /// </summary>
    class AccountBalancesExample
    {
        public static async Task Main(string[] args)
        {
            try
            {
                // Load configuration.
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                    .AddUserSecrets<AccountBalancesExample>()
                    .Build();

                // Get API key.
                var key = configuration["BinanceApiKey"] // user secrets configuration.
                    ?? configuration.GetSection("User")["ApiKey"]; // appsettings.json configuration.

                // Get API secret.
                var secret = configuration["BinanceApiSecret"] // user secrets configuration.
                    ?? configuration.GetSection("User")["ApiSecret"]; // appsettings.json configuration.

                // Configure services.
                var services = new ServiceCollection()
                    .AddBinance().BuildServiceProvider();

                using (var user = new BinanceUser(key, secret))
                using (var api = services.GetService<IBinanceApi>())
                using (var cache = services.GetService<IAccountCache>())
                using (var cts = new CancellationTokenSource())
                {
                    // Query and display current account balance.
                    var account = await api.GetAccountAsync(user);

                    var asset = Asset.BTC;

                    Display(account.GetBalance(asset));

                    // Display updated account balance.
                    var task = Task.Run(() =>
                        cache.SubscribeAsync(user, (e) => Display(e.Account.GetBalance(asset)), cts.Token));

                    Console.WriteLine("...press any key to exit.");
                    Console.ReadKey(true);

                    cts.Cancel();
                    await task;
                }
            }
            catch (Exception e) { Console.WriteLine(e.Message); }
        }

        private static void Display(AccountBalance balance)
        {
            Console.WriteLine();
            if (balance == null)
                Console.WriteLine($"  [None]");
            else
                Console.WriteLine($"  {balance.Asset}:  {balance.Free} (free)   {balance.Locked} (locked)");
            Console.WriteLine();
        }
    }
}