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
using FlashMemo.Model;

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

        await sp.GetRequiredService<DbSeeder>()
            .MigrateAndSeedAsync();

        await InitUserSession();
    }
    
    // TODO: maybe encapsulate this to some initialization service ??
    private async Task InitUserSession()
    {
        var ss = sp.GetRequiredService<ISessionDataService>();
        await ss.LoadAsync();

        if (ss.Current.LastLoadedUserId is null)
        {
            var ws = sp.GetRequiredService<WindowService>();
            await ws.ShowUserSelect();
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
        sc.AddSingleton<MainWindow>(); //* only one MainWindow per runtime
        sc.AddTransient<BrowseWindow>();
        sc.AddTransient<UserOptionsWindow>();
        sc.AddTransient<EditCardWindow>();
        sc.AddTransient<CreateCardWindow>();
        sc.AddTransient<UserSelectWindow>();
        
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
        sc.AddSingleton<UserOptionsVMF>();
        sc.AddSingleton<ReviewVMF>();

        // ==== SERVICES ====
        sc.AddSingleton<WindowService>();
        sc.AddSingleton<IDisplayControl, DisplayControl>();
        sc.AddSingleton<IDeckTreeBuilder, DeckTreeBuilder>();
        sc.AddSingleton<ICardService, CardService>();
        sc.AddSingleton<ICardQueryService, CardQueryService>();
        sc.AddSingleton<IUserVMBuilder, UserVMBuilder>();
        sc.AddSingleton<ISessionDataService, SessionDataService>();
        sc.AddAutoMapper(opt => opt.AddProfile<MappingProfile>());
        sc.AddTransient<DbSeeder>();
        sc.AddLogging();

        // ==== REPOS ====
        sc.AddSingleton<IDeckOptionsRepo, DeckOptionsRepo>();
        sc.AddSingleton<IUserOptionsRepo, UserOptionsRepo>();
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