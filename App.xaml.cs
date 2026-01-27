using System.Windows;
using FlashMemo.View;
using FlashMemo.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using FlashMemo.Model.Persistence;
using Microsoft.EntityFrameworkCore;
using FlashMemo.Services;
using FlashMemo.Repositories;
using FlashMemo.ViewModel.WindowVMs;

namespace FlashMemo;
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
            sc.AddTransient<EditWindow>();

            // ==== VIEWMODELS ====
            sc.AddSingleton<MainVM>();
            sc.AddTransient<ReviewVM>();
            sc.AddTransient<StatsVM>();
            sc.AddTransient<EditCardVM>();
            sc.AddTransient<CreateCardVM>();
            sc.AddTransient<DeckOptionsVM>();
            sc.AddTransient<DecksVM>();
            sc.AddTransient<UserOptionsVM>();
            sc.AddTransient<BrowseVM>();
            sc.AddTransient<FiltersVM>();

            // ==== SERVICES ====
            sc.AddSingleton<WindowService>();
            sc.AddSingleton<NavigationService>();
            sc.AddSingleton<DeckTreeBuilder>();
            sc.AddSingleton<CardService>();
            sc.AddSingleton<CardQueryService>();
            sc.AddSingleton<LearningPool>();
            sc.AddAutoMapper(typeof(App));

            // ==== REPOS ====
            sc.AddSingleton<DeckOptionsRepo>();
            sc.AddSingleton<DeckRepo>();
            sc.AddSingleton<TagRepo>();
            sc.AddSingleton<CardRepo>();
            sc.AddSingleton<UserRepo>();

            // ==== DB CONTEXT ====
            sc.AddDbContext<AppDbContext>(o => 
                o.UseSqlite($"Data Source={DbPath}"));

            sc.AddDbContextFactory<AppDbContext>();

            return sc;
        }
    }


