using Avalonia.Controls;
using Avalonia.Interactivity;
using GadgetApp.ViewModels;

namespace GadgetApp.Views
{
    /// Code-behind для вікна додавання/редагування.
    public partial class AddEditGadgetWindow : Window
    {
        public AddEditGadgetWindow()
        {
            InitializeComponent();
            this.FindControl<Button>("SaveButton").Click += OnSaveButtonClick;
            this.FindControl<Button>("CancelButton").Click += OnCancelButtonClick;
        }
        
        // Обробник натискання кнопки "Зберегти".
        // Запускає валідацію і закриває вікно, якщо вона успішна.
        private void OnSaveButtonClick(object? sender, RoutedEventArgs e)
        {
            if (this.DataContext is AddEditGadgetViewModel vm)
            {
                if (vm.Validate())
                {
                    this.Close(true); // Повертаємо 'true', що означає успішне збереження.
                }
            }
        }
        
        // Обробник натискання кнопки "Скасувати".
        private void OnCancelButtonClick(object? sender, RoutedEventArgs e)
        {
            this.Close(false); // Повертаємо 'false', що означає скасування.
        }
    }
}