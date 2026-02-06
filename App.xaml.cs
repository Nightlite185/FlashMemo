using System.Windows;
using FlashMemo.View;
using FlashMemo.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using FlashMemo.Model.Persistence;
using Microsoft.EntityFrameworkCore;
using FlashMemo.Services;
using FlashMemo.Repositories;
using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Factories;

namespace FlashMemo;
public partial class App : Application
{
    private IServiceProvider sp = null!;
    public static string DbPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FlashMemo", "Data",
        dbFileName
    );
    private const string dbFileName = "flashmemo.db";

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = ConfigureServices();
        this.sp = services.BuildServiceProvider();

        await InitUserSession();
    }
    
    // TODO: maybe encapsulate this to some initialization service ??
    private async Task InitUserSession()
    {
        var ss = sp.GetRequiredService<ISessionDataService>();
        await ss.LoadAsync();

        if (ss.Current.LastLoadedUserId is null)
        {
            var ws = sp.GetRequiredService<IWindowService>();
            await ws.ShowUserSelectWindow();
        }

        else
        {
            var factory = sp.GetRequiredService<MainVMF>();
            
            var mainVM = factory
                .Create((long)ss.Current.LastLoadedUserId);

            var win = new MainWindow(mainVM)
                { DataContext = mainVM };

            //wire any needed lifetime events here.

            win.Show();
        }
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
        sc.AddTransient<MainVM>(); //* transient cuz u can change user mid-runtime -> mainVM reloads.
        sc.AddTransient<ReviewVM>();
        sc.AddTransient<StatsVM>();
        sc.AddTransient<EditCardVM>();
        sc.AddTransient<CreateCardVM>();
        sc.AddTransient<DeckOptionsMenuVM>();
        sc.AddTransient<DecksVM>();
        sc.AddTransient<UserOptionsVM>();
        sc.AddTransient<BrowseVM>();
        sc.AddTransient<FiltersVM>();

        // ==== VM FACTORIES ====
        sc.AddSingleton<DeckOptionsMenuVMF>();
        sc.AddSingleton<DeckSelectVMF>();
        sc.AddSingleton<CardCtxMenuVMF>();
        sc.AddSingleton<CreateCardVMF>();
        sc.AddSingleton<UserSelectVMF>();
        sc.AddSingleton<EditCardVMF>();
        sc.AddSingleton<FiltersVMF>();
        sc.AddSingleton<ManageTagsVMF>();
        sc.AddSingleton<BrowseVMF>();
        sc.AddSingleton<MainVMF>();

        // ==== SERVICES ====
        sc.AddSingleton<IWindowService, WindowService>();
        sc.AddSingleton<INavigationService, NavigationService>();
        sc.AddSingleton<IDeckTreeBuilder, DeckTreeBuilder>();
        sc.AddSingleton<ICardService, CardService>();
        sc.AddSingleton<ICardQueryService, CardQueryService>();
        sc.AddSingleton<IUserVMBuilder, UserVMBuilder>();
        sc.AddSingleton<ISessionDataService, SessionDataService>();
        sc.AddAutoMapper(typeof(App));

        // ==== REPOS ====
        sc.AddSingleton<IDeckOptionsRepo, DeckOptionsRepo>();
        sc.AddSingleton<IDeckRepo, DeckRepo>();
        sc.AddSingleton<ITagRepo, TagRepo>();
        sc.AddSingleton<ICardRepo, CardRepo>();
        sc.AddSingleton<IUserRepo, UserRepo>();

        // ==== DB CONTEXT ====
        sc.AddDbContext<AppDbContext>(o => 
            o.UseSqlite($"Data Source={DbPath}"));

        sc.AddDbContextFactory<AppDbContext>();

        return sc;
    }
}