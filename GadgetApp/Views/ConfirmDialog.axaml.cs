using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace GadgetApp.Views
{
    // Code-behind для діалогового вікна підтвердження.
    public partial class ConfirmDialog : Window
    {
        public string Message { get; set; } = "Ви впевнені?";

        public ConfirmDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        [RelayCommand] private void Yes() => Close(true);
        [RelayCommand] private void No() => Close(false);
        
        // Статичний метод для зручного виклику діалогу.
        public static async Task<bool> ShowAsync(Window owner, string message)
        {
            var dialog = new ConfirmDialog { Message = message };
            return await dialog.ShowDialog<bool>(owner);
        }
    }
}