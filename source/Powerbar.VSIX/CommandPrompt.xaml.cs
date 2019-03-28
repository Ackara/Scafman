using Acklann.Powerbar.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace Acklann.Powerbar
{
    /// <summary>
    /// Interaction logic for CommandPrompt.xaml
    /// </summary>
    public partial class CommandPrompt : Window
    {
        public CommandPrompt(CommandPromptViewModel context = null)
        {
            InitializeComponent();
            if (context != null) base.DataContext = _viewModel = context;
            Title = Vsix.Name;

            
        }

        private readonly CommandPromptViewModel _viewModel;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Inputbox.Focus();
            Inputbox.CaretIndex = 0;
            Inputbox.Select(0, _viewModel.UserInput.Length);
            System.Diagnostics.Debug.WriteLine("Prompt Opened");
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Tab:
                    break;

                case Key.Up:
                    _viewModel.SelectPrevious();
                    break;

                case Key.Down:
                    _viewModel.SelectNext();
                    break;

                case Key.Escape:
                    Close();
                    break;

                case Key.Enter:
                    DialogResult = true;
                    _viewModel.Commit();
                    Close();
                    break;
            }
        }
    }
}