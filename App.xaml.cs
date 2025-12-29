using System.Windows;
using FlashMemo.View;
using FlashMemo.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace FlashMemo
{
    public partial class App : Application
    {
        public static ServiceProvider SP { get; private set; } = null!;
        private static string DbPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            dbFileName
        );
        private const string dbFileName = "FlashMemo.db";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = ConfigureServices();
            services.BuildServiceProvider();

            SP.GetRequiredService<MainWindow>().Show();
        }

        private static ServiceCollection ConfigureServices()
        {
            ServiceCollection sc = new();

            // ==== WINDOWS ====
            sc.AddSingleton<MainWindow>();
            sc.AddTransient<BrowseWindow>();
            sc.AddTransient<OptionsWindow>();

            // ==== VIEWMODELS ====
            sc.AddTransient<MainVM>();
            sc.AddTransient<OptionsVM>();
            sc.AddTransient<BrowseVM>();
            
            
            // ==== DB CONTEXT ====
            sc.AddSqlite<FlashMemoDbContext>($"Data Source={DbPath}");

            return sc;
        }
    }

}
