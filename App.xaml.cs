using System.Windows;
using FlashMemo.View;
using FlashMemo.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using FlashMemo.Model.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo
{
    public partial class App : Application
    {
        private static ServiceProvider SP { get; } = null!;
        public static string DbPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FlashMemo", "Data",
            dbFileName
        );
        private const string dbFileName = "flashmemo.db";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = ConfigureServices();
            services.BuildServiceProvider();

            SP.GetRequiredService<MainWindow>().Show(); // resolving the MainWindow and setting off the "ctor chain reaction"
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
            
            sc.AddDbContextFactory<AppDbContext>();
            
            // ==== DB CONTEXT ====
            sc.AddDbContext<AppDbContext>(o => 
                o.UseSqlite($"Data Source={DbPath}"));

            return sc;
        }
    }

}
