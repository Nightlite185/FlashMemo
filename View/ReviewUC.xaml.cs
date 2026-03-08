using FlashMemo.Helpers;
using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FlashMemo.View;

public partial class ReviewUC : UserControl
{
    private double zoom = 1.0;
    private ReviewVM? VM;
    private StandardNoteVM? observedNote;

    public ReviewUC()
    {
        InitializeComponent();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ReviewVM reviewVM)
            return;

        this.VM = reviewVM;

        // ctx menu events
        MoreButton.ContextMenu.DataContext = VM.CtxMenuVM;
        MoreButton.ContextMenuClosing += (_, _) => VM.CtxMenuVM.CloseMenu();
        MoreButton.ContextMenuOpening += (_, _) => VM.ShowCtxMenuCommand.Execute(null);

        // timer event
        CompositionTarget.Rendering += (_, _) => VM.UpdateTime();

        // current card rendering
        VM.PropertyChanged += OnVMPropertyChanged;

        RewireObservedNote(VM.CurrentCard);
        LoadCard(VM);
        Focus();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (VM is not null)
            VM.PropertyChanged -= OnVMPropertyChanged;

        UnwireObservedNote();
        VM = null;
    }

    private void OnVMPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (VM is null)
            return;

        if (e.PropertyName == nameof(VM.CurrentCard))
        {
            RewireObservedNote(VM.CurrentCard);
            LoadCard(VM);
        }
    }

    private void OnObservedNotePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (VM is null) return;

        if (e.PropertyName is nameof(StandardNoteVM.FrontContent)
            or nameof(StandardNoteVM.BackContent))
        {
            LoadCard(VM);
        }
    }

    private void RewireObservedNote(CardVM? card)
    {
        UnwireObservedNote();

        if (card?.Note is StandardNoteVM sn)
        {
            observedNote = sn;
            observedNote.PropertyChanged += OnObservedNotePropertyChanged;
        }
    }

    private void UnwireObservedNote()
    {
        if (observedNote is not null)
            observedNote.PropertyChanged -= OnObservedNotePropertyChanged;

        observedNote = null;
    }

    private void LoadCard(ReviewVM vm)
    {
        if (vm.CurrentCard is null)
            return;

        if (vm.CurrentCard.Note is not StandardNoteVM sn)
            throw new NotSupportedException("Only standard notes supported for now.");

        FrontBox.Document = XamlSerializer
            .FromXaml(sn.FrontContent);

        BackBox.Document = XamlSerializer
            .FromXaml(sn.BackContent);

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
        if (DataContext is not ReviewVM vm) return;

        if (e.Key is Key.Space or Key.Enter)
        {
            if (!vm.AnswerRevealed &&
            vm.RevealAnswerCommand.CanExecute(null))
            {
                vm.RevealAnswerCommand.Execute(null);
            }

            else if (vm.GoodAnswerCommand.CanExecute(null))
            {
                vm.GoodAnswerCommand.Execute(null);
            }

            e.Handled = true;
            return;
        }

        if (!vm.AnswerRevealed) return;

        switch (e.Key)
        {
            case Key.D1 or Key.NumPad1:
                vm.AgainAnswerCommand.Execute(null);
                break;

            case Key.D2 or Key.NumPad2:
                vm.HardAnswerCommand.Execute(null);
                break;

            case Key.D3 or Key.NumPad3:
                vm.GoodAnswerCommand.Execute(null);
                break;

            case Key.D4 or Key.NumPad4:
                vm.EasyAnswerCommand.Execute(null);
                break;

            default:
                return;
        }

        e.Handled = true;
    }

    private void MoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn
        || btn.ContextMenu is null)
            return;

        btn.ContextMenu.PlacementTarget = btn;
        btn.ContextMenu.IsOpen = true;
    }
    private void HistoryButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.ContextMenu is null)
            return;

        btn.ContextMenu.PlacementTarget = btn;
        btn.ContextMenu.IsOpen = true;
    }
}
