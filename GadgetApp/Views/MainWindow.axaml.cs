using Avalonia.Controls;
using GadgetApp.ViewModels;
using System.ComponentModel;
using Avalonia.Interactivity; 

namespace GadgetApp.Views
{
    /// Code-behind для головного вікна програми.
    public partial class MainWindow : Window
    {
        // Доступ до ViewModel через DataContext.
        private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

        public MainWindow()
        {
            InitializeComponent();
            // Встановлюємо ViewModel.
            DataContext = new MainWindowViewModel();
            // Підписуємося на подію закриття.
            this.Closing += MainWindow_Closing;
        }
        
        // Обробник події закриття вікна (перевіряє незбережені зміни).
        private async void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
        {
            if (ViewModel == null) return;

            // 1. Тимчасово блокуємо закриття.
            e.Cancel = true;

            // 2. Викликаємо метод ViewModel для перевірки та діалогу.
            bool canClose = await ViewModel.CanCloseAsync();

            // 3. Якщо можна закривати (зміни збережені/скасовані/не було):
            if (canClose)
            {
                // 3.1. Відписуємося від події, щоб уникнути рекурсії.
                this.Closing -= MainWindow_Closing;
                // 3.2. Закриваємо вікно остаточно.
                Close();
            }
            // 4. Якщо скасовано користувачем, e.Cancel залишається true.
        }
    }
}