using System.Windows;
using FlashMemo.View;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using FlashMemo.Model.Persistence;
using Microsoft.EntityFrameworkCore;
using FlashMemo.Services;
using FlashMemo.Repositories;
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
    
    private async Task InitUserSession()
    {
        var lss = sp.GetRequiredService<ILastSessionService>();
        await lss.LoadAsync();

        long? lastUser = lss.Current.LastLoadedUserId;
        
        if (lastUser is null)
        {
            var ws = sp.GetRequiredService<WindowService>();
            await ws.ShowUserSelect(currentUserId: null);
        }

        else
        {
            var mainVM = sp.GetRequiredService<MainVMF>()
                .Create((long)lastUser);
            
            sp.GetRequiredService<MainWindowFactory>()
                .Resolve(mainVM);
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
        sc.AddSingleton<DecksVMF>();

        // ==== SERVICES ====
        sc.AddTransient<MainWindowFactory>();
        sc.AddTransient<DisplayControlFactory>();
        sc.AddSingleton<ILoginService, LoginService>();
        sc.AddSingleton<ICountingService, CountingService>();
        sc.AddSingleton<IDeckTreeBuilder, DeckTreeBuilder>();
        sc.AddSingleton<ICardService, CardService>();
        sc.AddSingleton<ICardQueryService, CardQueryService>();
        sc.AddSingleton<ICardQueryBuilder, CardQueryBuilder>();
        sc.AddSingleton<IUserVMBuilder, UserVMBuilder>();
        sc.AddSingleton<ILastSessionService, LastSessionService>();
        sc.AddAutoMapper(opt => opt.AddProfile<MappingProfile>());
        sc.AddSingleton<WindowService>();
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