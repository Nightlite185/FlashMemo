using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FlashMemo.Helpers;

namespace FlashMemo.Tests.ViewTests;

[CollectionDefinition("WPF")]
public class WpfTestCollection : ICollectionFixture<WpfTestHost>;

// WPF controls share one STA thread. Never start FlashMemo.App or open a window:
// these view tests must not run startup migrations or interact with the desktop.
public sealed class WpfTestHost : IDisposable
{
    private readonly BlockingCollection<Action> work = new();
    private readonly Thread thread;

    public WpfTestHost()
    {
        thread = new Thread(() =>
        {
            var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            var resources = app.Resources;
            foreach (string key in new[] { "DeckDimBrush", "DeckNewBrush", "DeckLearnBrush", "DeckDueBrush", "DeckTextBrush" })
                resources[key] = Brushes.White;
            resources["DeckRowHoverBrush"] = new SolidColorBrush(Color.FromRgb(42, 42, 42));
            resources["DeckOptionsButtonStyle"] = new Style(typeof(Button));
            resources["NavButtonStyle"] = new Style(typeof(Button));
            resources["BoolToVisConverter"] = new BoolToVisConverter();
            resources["InverseBoolToVisConverter"] = new InverseBoolToVisConverter();
            resources["NullToVisConverter"] = new NullToVisConverter();
            resources["ActivityWeekToMonthLabelConverter"] = new ActivityWeekToMonthLabelConverter();
            resources["ActivityIntensityToBrushConverter"] = new ActivityIntensityToBrushConverter();
            foreach (Action action in work.GetConsumingEnumerable()) action();
            app.Shutdown();
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
    }

    public Task Run(Action action)
    {
        var result = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        work.Add(() =>
        {
            try { action(); result.SetResult(); }
            catch (Exception e) { result.SetException(e); }
        });
        return result.Task;
    }

    public void Dispose()
    {
        work.CompleteAdding();
        thread.Join();
        work.Dispose();
    }
}
