using System.Windows;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.View
{
    public partial class EditCardWindow : Window, IViewFor<CardEditorVM>
    {
        public CardEditorVM VM { get; set; } = null!;
        public EditCardWindow()
        {
            InitializeComponent();
        }
    }
}
