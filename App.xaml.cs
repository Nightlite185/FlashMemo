using System.Windows;
using FlashMemo.View;
using Microsoft.Extensions.DependencyInjection;
using FlashMemo.Model.Persistence;
using Microsoft.EntityFrameworkCore;
using FlashMemo.Services;
using FlashMemo.Repositories;
using FlashMemo.ViewModel.Factories;
using FlashMemo.Model;
using System.IO;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using Serilog;
using Syncfusion.Licensing;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace FlashMemo;
public partial class App : Application
{
    private ServiceProvider sp = null!;
    private Mutex? singleInstanceMutex;
    internal static bool IsFatalShutdownStarted { get; set; }

    public App()
    {
        DispatcherUnhandledException += OnUnexpectedUiException;
    }

    private void OnUnexpectedUiException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ILogger logger;
        try { logger = sp?.GetService<ILogger<App>>() ?? (ILogger)new StartupLogger(); }
        catch { logger = new StartupLogger(); }

        // Suppress WPF's default handling only when our shutdown request succeeds.
        e.Handled = TryHandleFatalException(e.Exception, logger, new WpfDialogService(),
            () => Shutdown(exitCode: 1));
    }

    internal static bool TryHandleFatalException(Exception ex, ILogger logger,
        IDialogService dialogs, Action shutdown)
    {
        // A second dispatcher failure during reporting/cleanup must use WPF's fatal path.
        if (IsFatalShutdownStarted)
            return false;

        IsFatalShutdownStarted = true;

        try { logger.LogCritical(ex, "Unexpected UI exception; FlashMemo will close"); }
        catch
        {
            try { new StartupLogger().LogCritical(ex, "Unexpected UI exception; FlashMemo will close"); }
            catch { } // Reporting must not prevent shutdown.
        }

        try
        {
            dialogs.ShowError(
                message: "FlashMemo encountered an unexpected error and needs to close. Unsaved changes may be lost. Please restart the application.",
                title: "Fatal error",
                buttons: DialogButtons.OK);
        }
        catch { } // The UI may itself be the source of the failure.

        try
        {
            shutdown();
            return true;
        }
        catch
        {
            // Leave the dispatcher exception unhandled if orderly shutdown fails.
            return false;
        }
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        singleInstanceMutex = new Mutex(
            initiallyOwned: true,
            name: @"Local\FlashMemo.SingleInstance",
            createdNew: out bool isFirstInstance);

        if (!isFirstInstance)
        {
            singleInstanceMutex.Dispose();
            singleInstanceMutex = null;

            DialogService.ShowInfo(
                message: "FlashMemo is already running. Please use the existing application window.",
                title: "FlashMemo is already running",
                buttons: DialogButtons.OK);

            Shutdown();
            return;
        }

        // DI and the normal log directory may themselves fail to initialize.
        var startupNet = new SafetyNet(new StartupLogger(), new WpfDialogService());

        if (!await startupNet.TryUnsafe(
            () => StartAsync(e),
            actionTried: "start FlashMemo. The application could not initialize and will close.",
            retry: false))
        {
            Shutdown(exitCode: 1);
        }
    }

    private async Task<bool> StartAsync(StartupEventArgs e)
    {
        base.OnStartup(e);
        await CheckLicenses();

        var services = ConfigureServices();
        this.sp = services.BuildServiceProvider();
        using (var scope = sp.CreateScope())
        {
            bool seeded = await scope.ServiceProvider
                .GetRequiredService<DbSeeder>()
                .TrySeedAsync();

            if (!seeded)
                return false;
        }

        var net = sp.GetRequiredService<SafetyNet>();

        return await net.TryUnsafe(
            InitUserSession,
            actionTried: "load the saved application session and open FlashMemo. The application cannot continue and will close.",
            retry: false);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        sp?.Dispose();
        singleInstanceMutex?.ReleaseMutex();
        singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }
    
    private async Task InitUserSession()
    {
        var lss = sp.GetRequiredService<ILastSessionService>();
        await lss.LoadAppSessionAsync();

        if (lss.LastUserId is null)
        {
            //* the user selector is actually a dialog, not a window
            //* so it is only once the dialog is closed, that the execution comes back here.
            await sp.GetRequiredService<WindowService>()
                .ShowUserSelect(currentUserId: null);

            // Cancelling the startup chooser must also exit with explicit shutdown enabled.
            if (!Windows.OfType<MainWindow>().Any(w => w.IsVisible))
                Shutdown();
        }

        else
        {
            bool loggedIn = await sp.GetRequiredService<ILoginService>()
                .ChangeUser((long)lss.LastUserId);

            if (!loggedIn) Shutdown(exitCode: 1);
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
        sc.AddSingleton<RescheduleVMF>();
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
        sc.AddTransient<IMainWindowBootstrapper, MainWindowBootstrapper>();
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
        sc.AddDbContext<AppDbContext>(o => o
            .UseSqlite($"Data Source={AppDbContext.DbPath}"));

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