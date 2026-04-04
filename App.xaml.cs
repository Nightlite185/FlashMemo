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
using Microsoft.Extensions.Logging;
using System.Diagnostics;

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
        await sp.GetRequiredService<ICardService>()
            .UnburyIfNextDay();

        var lss = sp.GetRequiredService<ILastSessionService>();
        await lss.LoadAsync();

        if (lss.LastUserId is null)
        {
            await sp.GetRequiredService<WindowService>()
                .ShowUserSelect(currentUserId: null);
        }

        else
        {
            var mainVM = sp.GetRequiredService<MainVMF>()
                .Create((long)lss.LastUserId);
            
            sp.GetRequiredService<MainWindowBootstrapper>()
                .SetupMainWindow(mainVM);
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
        sc.AddTransient<DeckOptionsWindow>();
        
        // ==== VM FACTORIES ====
        sc.AddSingleton<DeckOptionsMenuVMF>();
        sc.AddSingleton<DeckSelectVMF>();
        sc.AddSingleton<CardCtxMenuVMF>();
        sc.AddSingleton<CreateCardVMF>();
        sc.AddSingleton<UserSelectVMF>();
        sc.AddSingleton<CardEditorVMF>();
        sc.AddSingleton<EditableCardVMF>();
        sc.AddSingleton<HeatmapVMF>();
        sc.AddSingleton<FiltersVMF>();
        sc.AddSingleton<BrowseVMF>();
        sc.AddSingleton<MainVMF>();
        sc.AddSingleton<UserOptionsMenuVMF>();
        sc.AddSingleton<ReviewVMF>();
        sc.AddSingleton<DecksVMF>();
        sc.AddSingleton<CardTagsVMF>();

        // ==== SERVICES ====
        sc.AddTransient<MainWindowBootstrapper>();
        sc.AddTransient<DisplayControlFactory>();
        sc.AddSingleton<INoteComparer, XamlNoteComparer>();
        sc.AddSingleton<ILoginService, LoginService>();
        sc.AddSingleton<ICountingService, CountingService>();
        sc.AddSingleton<IDeckTreeBuilder, DeckTreeBuilder>();
        sc.AddSingleton<ICardService, CardService>();
        sc.AddSingleton<IDeckOptionsService, DeckOptionsService>();
        sc.AddSingleton<IUserOptionsService, UserOptionsService>();
        sc.AddSingleton<IActivityVMBuilder, ActivityVMBuilder>();
        sc.AddSingleton<ICardQueryService, CardQueryService>();
        sc.AddSingleton<IDeckOptVMBuilder, DeckOptVMBuilder>();
        sc.AddSingleton<IUserVMBuilder, UserVMBuilder>();
        sc.AddSingleton<ILastSessionService, LastSessionService>();
        sc.AddSingleton<IVMEventBus, VMEventBus>();
        sc.AddAutoMapper(opt => opt.AddProfile<MappingProfile>());
        sc.AddSingleton<WindowService>();
        sc.AddTransient<DbSeeder>();
        sc.AddLogging();

        // ==== REPOS ====
        sc.AddSingleton<IDeckRepo, DeckRepo>();
        sc.AddSingleton<ITagRepo, TagRepo>();
        sc.AddSingleton<ICardRepo, CardRepo>();
        sc.AddSingleton<IUserRepo, UserRepo>();

        // ==== DB CONTEXT ====
        sc.AddDbContext<AppDbContext>(o =>
        {
            o.UseSqlite($"Data Source={DbPath}");
            o.EnableSensitiveDataLogging();

            o.LogTo(
                log => Debug.WriteLine(log),
                LogLevel.Information);
        });

        sc.AddDbContextFactory<AppDbContext>();

        return sc;
    }
}