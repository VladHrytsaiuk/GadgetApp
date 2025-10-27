using Avalonia.Controls;
using System.Threading.Tasks;

namespace GadgetApp.Views
{
    // Результат діалогу збереження змін.
    public enum SaveChangesResult
    {
        Save,
        DontSave,
        Cancel
    }
    
    // Code-behind для діалогового вікна підтвердження збереження змін.
    public partial class SaveChangesDialog : Window
    {
        public SaveChangesDialog()
        {
            InitializeComponent();

            this.FindControl<Button>("SaveButton")!.Click += (s, e) => Close(SaveChangesResult.Save);
            this.FindControl<Button>("DontSaveButton")!.Click += (s, e) => Close(SaveChangesResult.DontSave);
            this.FindControl<Button>("CancelButton")!.Click += (s, e) => Close(SaveChangesResult.Cancel);
        }

        // Статичний метод для зручного виклику діалогу.
        public static async Task<SaveChangesResult> ShowAsync(Window owner)
        {
            var dialog = new SaveChangesDialog
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            // Показуємо діалог і чекаємо на результат типу SaveChangesResult
            return await dialog.ShowDialog<SaveChangesResult>(owner);
        }
    }
}