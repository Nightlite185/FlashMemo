using FlashMemo.Helpers;
using FlashMemo.ViewModel.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FlashMemo.View;

public partial class ReviewUC : UserControl
{
    private double zoom = 1.0;

    public ReviewUC()
    {
        InitializeComponent();

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is not ReviewVM vm)
            return;

        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(vm.CurrentCard))
            {
                LoadCard(vm);
            }
        };

        LoadCard(vm);
        Focus();
    }


    private void LoadCard(ReviewVM vm)
    {
        if (vm.CurrentCard is null)
            return;

        FrontBox.Document =
            XamlSerializer.FromXaml(vm.FrontContent);

        BackBox.Document =
            XamlSerializer.FromXaml(vm.BackContent);

        ApplyZoom();
    }


    // ================= ZOOM =================

    private void Root_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!Keyboard.IsKeyDown(Key.LeftCtrl))
            return;

        zoom += e.Delta > 0 ? 0.1 : -0.1;
        zoom = Math.Clamp(zoom, 0.5, 3.0);

        ApplyZoom();
    }

    private void ApplyZoom()
    {
        FrontBox.LayoutTransform =
            new ScaleTransform(zoom, zoom);

        BackBox.LayoutTransform =
            new ScaleTransform(zoom, zoom);
    }


    // ================= KEYS =================

    private void Root_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not ReviewVM vm)
            return;

        if (e.Key is Key.Space or Key.Enter)
        {
            if (vm.RevealAnswerCommand.CanExecute(null))
                vm.RevealAnswerCommand.Execute(null);

            e.Handled = true;
        }
    }
}