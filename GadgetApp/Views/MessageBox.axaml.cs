using Avalonia.Controls;

namespace GadgetApp.Views
{
    // Code-behind для простого вікна повідомлень.
    public partial class MessageBoxView : Window
    {
        public MessageBoxView()
        {
            InitializeComponent();
        }
        
        // Статичний метод для зручного виклику.
        public static void Show(Window owner, string title, string message)
        {
            var dialog = new MessageBoxView();
            dialog.FindControl<TextBlock>("MessageTitle")!.Text = title;
            dialog.FindControl<TextBlock>("MessageContent")!.Text = message;
            dialog.FindControl<Button>("OkButton")!.Click += (s, e) => dialog.Close();
            dialog.ShowDialog(owner);
        }
    }
}