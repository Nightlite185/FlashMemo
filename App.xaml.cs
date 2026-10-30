using System.Windows;
using FlashMemo.View;
using Microsoft.Extensions.DependencyInjection;
using FlashMemo.Model.Persistence;
using Microsoft.EntityFrameworkCore;
using FlashMemo.Services;
using FlashMemo.Repositories;
using FlashMemo.ViewModel.Factories;
using FlashMemo.Model;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using Serilog;
using Syncfusion.Licensing;

namespace FlashMemo;
public partial class App : Application
{
    private ServiceProvider sp = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        await CheckLicenses();
        base.OnStartup(e);

        var services = ConfigureServices();
        this.sp = services.BuildServiceProvider();

        using (var scope = sp.CreateScope())
        {
            await scope.ServiceProvider
                .GetRequiredService<DbSeeder>()
                .SeedAsync();
        }

        await InitUserSession();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        sp?.Dispose();
        base.OnExit(e);
    }
    
    private async Task InitUserSession()
    {
        await sp.GetRequiredService<ICardService>()
            .UnburyIfNextDay();

        var lss = sp.GetRequiredService<ILastSessionService>();
        await lss.LoadAppSessionAsync();

        if (lss.LastUserId is null)
        {
            await sp.GetRequiredService<WindowService>()
                .ShowUserSelect(currentUserId: null);
        }

        else
        {
            await lss.LoadUserCacheAsync((long)lss.LastUserId);

            var mainVM = await sp.GetRequiredService<MainVMF>()
                .CreateAsync((long)lss.LastUserId);
            
            sp.GetRequiredService<MainWindowBootstrapper>()
                .SetupMainWindow(mainVM);
        }
    }
    private static async Task CheckLicenses()
    {
        await using var stream = ResourceAssembly
            .GetManifestResourceStream("FlashMemo.SyncfusionLicense");

        // Let Syncfusion handle missing registration with its own licensing warning.
        if (stream is null)
            return;

        using var reader = new StreamReader(stream);
        string key = (await reader.ReadToEndAsync()).Trim();

        if (!string.IsNullOrWhiteSpace(key))
            SyncfusionLicenseProvider.RegisterLicense(key);
    }
    private static ServiceCollection ConfigureServices()
    {
        ServiceCollection sc = new();

        #region WINDOWS
        sc.AddSingleton<MainWindow>(); //* only one MainWindow per runtime
        sc.AddTransient<BrowseWindow>();
        sc.AddTransient<UserOptionsWindow>();
        sc.AddTransient<EditCardWindow>();
        sc.AddTransient<CreateCardWindow>();
        sc.AddTransient<UserSelectWindow>();
        sc.AddTransient<DeckOptionsWindow>();
        #endregion
        
        #region VM FACTORIES
        sc.AddSingleton<ExportVMF>();
        sc.AddSingleton<DeckOptionsMenuVMF>();
        sc.AddSingleton<DeckSelectVMF>();
        sc.AddSingleton<CardCtxMenuVMF>();
        sc.AddSingleton<CreateCardVMF>();
        sc.AddSingleton<CreateDeckVMF>();
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
        sc.AddSingleton<StatsVMF>();
        #endregion

        #region SERVICES
        sc.AddTransient<MainWindowBootstrapper>();
        sc.AddTransient<DisplayControlFactory>();
        sc.AddSingleton<INoteComparer, XamlNoteComparer>();
        sc.AddSingleton<INoteSerializer, XamlNoteSerializer>();
        sc.AddSingleton<IExportService, ExportService>();
        sc.AddSingleton<IDialogService, WpfDialogService>();
        sc.AddSingleton<SafetyNet>();
        sc.AddSingleton<ILoginService, LoginService>();
        sc.AddSingleton<ICountingService, CountingService>();
        sc.AddSingleton<IDeckTreeBuilder, DeckTreeBuilder>();
        sc.AddSingleton<ICardService, CardService>();
        sc.AddSingleton<IDeckOptionsService, DeckOptionsService>();
        sc.AddSingleton<IUserOptionsService, UserOptionsService>();
        sc.AddSingleton<IActivityVMBuilder, ActivityVMBuilder>();
        sc.AddSingleton<ICardQueryService, CardQueryService>();
        sc.AddSingleton<IStatsQueryService, StatsQueryService>();
        sc.AddSingleton<IDeckOptVMBuilder, DeckOptVMBuilder>();
        sc.AddSingleton<IUserVMBuilder, UserVMBuilder>();
        sc.AddSingleton<ILastSessionService, LastSessionService>();
        sc.AddSingleton<IVMEventBus, VMEventBus>();
        sc.AddAutoMapper(opt => opt.AddProfile<MappingProfile>());
        sc.AddSingleton<WindowService>();
        sc.AddTransient<DbSeeder>();
        #endregion

        #region REPOS
        sc.AddSingleton<IDeckRepo, DeckRepo>();
        sc.AddSingleton<ITagRepo, TagRepo>();
        sc.AddSingleton<ICardRepo, CardRepo>();
        sc.AddSingleton<IUserRepo, UserRepo>();
        #endregion

        #region DB CONTEXT
        sc.AddDbContext<AppDbContext>(o =>
        {
            o.UseSqlite($"Data Source={AppDbContext.DbPath}");
            o.EnableSensitiveDataLogging(); // TODO: remove for the release build

            o.LogTo(
                log => Debug.WriteLine(log),
                LogLevel.Information);
        });

        sc.AddDbContextFactory<AppDbContext>();
        #endregion

        #region LOGGER
        string logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FlashMemo", "Logs");

        Directory.CreateDirectory(logDirectory);

        var fileLogger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Async(sink => sink.File(
                    Path.Combine(logDirectory, "log-.txt"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"),
                blockWhenFull: false)
            .CreateLogger();

        sc.AddLogging(logging => logging.AddSerilog(fileLogger, dispose: true));
        #endregion

        return sc;
    }
}
