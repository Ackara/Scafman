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
            if (context != null) DataContext = _viewModel = context;

            Title = Vsix.Name;
            SizeToContent = (_viewModel.Width <= CommandPromptViewModel.MINIMUM_WIDTH ? SizeToContent.Width : SizeToContent.Manual);
            WindowStartupLocation = (_viewModel.Top <= CommandPromptViewModel.DEFAULT_POSITION ? WindowStartupLocation.CenterOwner : WindowStartupLocation.Manual);
        }

        private readonly CommandPromptViewModel _viewModel;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Inputbox.Focus();
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

        private void OnSourceInitialized(object sender, System.EventArgs e)
        {
            this.HideMinimizeAndMaximizeButtons();
        }
    }
}