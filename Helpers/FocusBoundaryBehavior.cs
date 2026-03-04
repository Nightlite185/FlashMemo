using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using FlashMemo.ViewModel;

namespace FlashMemo.Helpers;

public static class FocusBoundaryBehavior
{
    public static readonly DependencyProperty EnableBoundaryFocusTrackingProperty =
        DependencyProperty.RegisterAttached(
            "EnableBoundaryFocusTracking",
            typeof(bool),
            typeof(FocusBoundaryBehavior),
            new PropertyMetadata(false, OnEnableBoundaryFocusTrackingChanged));

    private static readonly DependencyProperty StateProperty =
        DependencyProperty.RegisterAttached(
            "State",
            typeof(FocusTrackingState),
            typeof(FocusBoundaryBehavior),
            new PropertyMetadata(null));

    public static bool GetEnableBoundaryFocusTracking(DependencyObject obj)
        => (bool)obj.GetValue(EnableBoundaryFocusTrackingProperty);

    public static void SetEnableBoundaryFocusTracking(DependencyObject obj, bool value)
        => obj.SetValue(EnableBoundaryFocusTrackingProperty, value);

    private static FocusTrackingState? GetState(DependencyObject obj)
        => (FocusTrackingState?)obj.GetValue(StateProperty);

    private static void SetState(DependencyObject obj, FocusTrackingState? state)
        => obj.SetValue(StateProperty, state);

    private static void OnEnableBoundaryFocusTrackingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement root)
            return;

        if ((bool)e.NewValue)
        {
            var state = new FocusTrackingState(root);
            SetState(root, state);

            root.PreviewGotKeyboardFocus += Root_PreviewGotKeyboardFocus;
            root.PreviewLostKeyboardFocus += Root_PreviewLostKeyboardFocus;
            root.Loaded += Root_Loaded;
            root.Unloaded += Root_Unloaded;
            root.DataContextChanged += Root_DataContextChanged;

            WireWindow(root, state);
            Reevaluate(root, state);
            return;
        }

        var existing = GetState(root);
        if (existing is null)
            return;

        root.PreviewGotKeyboardFocus -= Root_PreviewGotKeyboardFocus;
        root.PreviewLostKeyboardFocus -= Root_PreviewLostKeyboardFocus;
        root.Loaded -= Root_Loaded;
        root.Unloaded -= Root_Unloaded;
        root.DataContextChanged -= Root_DataContextChanged;
        UnwireWindow(existing);
        SetState(root, null);
    }

    private static void Root_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement root || GetState(root) is not FocusTrackingState state)
            return;

        WireWindow(root, state);
        Reevaluate(root, state);
    }

    private static void Root_Unloaded(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement root || GetState(root) is not FocusTrackingState state)
            return;

        if (state.HasBoundaryFocus)
        {
            NotifyFocusLost(root);
            state.HasBoundaryFocus = false;
        }

        UnwireWindow(state);
    }

    private static void Root_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is not FrameworkElement root || GetState(root) is not FocusTrackingState state)
            return;

        var hasFocusNow = ComputeHasBoundaryFocus(root, state);

        if (!state.HasBoundaryFocus && hasFocusNow)
        {
            state.HasBoundaryFocus = true;
            NotifyFocusGained(root);
        }
        else if (state.HasBoundaryFocus && !hasFocusNow)
        {
            state.HasBoundaryFocus = false;
            NotifyFocusLost(root);
        }
    }

    private static void Root_PreviewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is not FrameworkElement root || GetState(root) is not FocusTrackingState state)
            return;

        var oldInside = IsInsideRoot(root, e.OldFocus as DependencyObject);
        var newInside = IsInsideRoot(root, e.NewFocus as DependencyObject);

        if (!oldInside && newInside && !state.HasBoundaryFocus && IsWindowActive(root, state))
        {
            state.HasBoundaryFocus = true;
            NotifyFocusGained(root);
        }
    }

    private static void Root_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is not FrameworkElement root || GetState(root) is not FocusTrackingState state)
            return;

        var oldInside = IsInsideRoot(root, e.OldFocus as DependencyObject);
        var newInside = IsInsideRoot(root, e.NewFocus as DependencyObject);

        if (oldInside && !newInside && state.HasBoundaryFocus)
        {
            state.HasBoundaryFocus = false;
            NotifyFocusLost(root);
        }
    }

    private static void WireWindow(FrameworkElement root, FocusTrackingState state)
    {
        var win = Window.GetWindow(root);
        if (win is null || ReferenceEquals(win, state.Window))
            return;

        UnwireWindow(state);

        state.Window = win;
        win.Activated += state.OnWindowActivated;
        win.Deactivated += state.OnWindowDeactivated;
        win.PreviewMouseDown += state.OnWindowPreviewMouseDown;
    }

    private static void UnwireWindow(FocusTrackingState state)
    {
        if (state.Window is null)
            return;

        state.Window.Activated -= state.OnWindowActivated;
        state.Window.Deactivated -= state.OnWindowDeactivated;
        state.Window.PreviewMouseDown -= state.OnWindowPreviewMouseDown;
        state.Window = null;
    }

    private static bool IsWindowActive(FrameworkElement root, FocusTrackingState state)
    {
        var win = state.Window ?? Window.GetWindow(root);
        return win?.IsActive ?? false;
    }

    private static bool ComputeHasBoundaryFocus(FrameworkElement root, FocusTrackingState state)
    {
        if (!IsWindowActive(root, state))
            return false;

        return IsInsideRoot(root, Keyboard.FocusedElement as DependencyObject);
    }

    private static void Reevaluate(FrameworkElement root, FocusTrackingState state)
    {
        var hasFocusNow = ComputeHasBoundaryFocus(root, state);

        if (!state.HasBoundaryFocus && hasFocusNow)
        {
            state.HasBoundaryFocus = true;
            NotifyFocusGained(root);
            return;
        }

        if (state.HasBoundaryFocus && !hasFocusNow)
        {
            state.HasBoundaryFocus = false;
            NotifyFocusLost(root);
        }
    }

    private static bool IsInsideRoot(FrameworkElement root, DependencyObject? element)
    {
        if (element is null)
            return false;

        var current = element;

        while (current is not null)
        {
            if (ReferenceEquals(current, root))
                return true;

            current = GetParent(current);
        }

        return false;
    }

    private static DependencyObject? GetParent(DependencyObject current)
    {
        if (current is FrameworkContentElement fce)
            return fce.Parent ?? fce.TemplatedParent;

        if (current is Visual || current is Visual3D)
            return VisualTreeHelper.GetParent(current) ?? LogicalTreeHelper.GetParent(current);

        return LogicalTreeHelper.GetParent(current);
    }

    private static void NotifyFocusGained(FrameworkElement root)
    {
        if (root.DataContext is IFocusState vm)
            _ = vm.OnFocusGained();
    }

    private static void NotifyFocusLost(FrameworkElement root)
    {
        if (root.DataContext is IFocusState vm)
            vm.OnFocusLost();
    }

    private sealed class FocusTrackingState
    {
        private readonly FrameworkElement root;

        public FocusTrackingState(FrameworkElement root)
        {
            this.root = root;
        }

        public bool HasBoundaryFocus { get; set; }
        public Window? Window { get; set; }

        public void OnWindowActivated(object? sender, EventArgs e)
        {
            var hasFocusNow = IsInsideRoot(root, Keyboard.FocusedElement as DependencyObject);

            if (!HasBoundaryFocus && hasFocusNow)
            {
                HasBoundaryFocus = true;
                NotifyFocusGained(root);
            }
        }

        public void OnWindowDeactivated(object? sender, EventArgs e)
        {
            if (!HasBoundaryFocus)
                return;

            HasBoundaryFocus = false;
            NotifyFocusLost(root);
        }

        public void OnWindowPreviewMouseDown(object? sender, MouseButtonEventArgs e)
        {
            var clickedInside = IsInsideRoot(root, e.OriginalSource as DependencyObject);

            if (!HasBoundaryFocus && clickedInside)
            {
                HasBoundaryFocus = true;
                NotifyFocusGained(root);
                return;
            }

            if (HasBoundaryFocus && !clickedInside)
            {
                HasBoundaryFocus = false;
                NotifyFocusLost(root);
            }
        }
    }
}
