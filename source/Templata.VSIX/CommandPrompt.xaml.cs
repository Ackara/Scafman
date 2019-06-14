using Acklann.Templata.Models;
using System.Windows;
using System.Windows.Input;

namespace Acklann.Templata
{
    public partial class CommandPrompt : Window
    {
        public CommandPrompt(CommandPromptViewModel context = null)
        {
            InitializeComponent();
            if (context != null) DataContext = _model = context;

            Title = Vsix.Name;
            SizeToContent = SizeToContent.Height;
            WindowStartupLocation = (_model.Top <= CommandPromptViewModel.DEFAULT_POSITION ? WindowStartupLocation.CenterOwner : WindowStartupLocation.Manual);
        }

        private readonly CommandPromptViewModel _model;
        private KeyEventArgs _shiftKey;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Inputbox.Focus();
        }

        private void OnPreviewKeyUp(object sender, KeyEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine($"{nameof(Templata)} | pressed: {e.Key}, text: {Inputbox.Text}");

            switch (e.Key)
            {
                default:
                    _model.UpdateIntellisense(Inputbox.Text ?? _model.UserInput);
                    break;

                case Key.D2:// '@' symbol
                    if (_shiftKey.IsDown) _model.ActivateIntellisense(true);
                    break;

                case Key.OemSemicolon: // ')' key
                    _model.ActivateIntellisense(false);
                    break;

                case Key.Up:
                    _model.MoveUp();
                    break;

                case Key.Down:
                    _model.MoveDown();
                    break;

                case Key.Tab:
                    _model.CompleteCommand(Inputbox.Text ?? _model.UserInput);
                    Inputbox.CaretIndex = Inputbox.Text.Length;
                    break;

                case Key.Escape:
                    Close();
                    break;

                case Key.Enter:
                    DialogResult = true;
                    Close();
                    break;

                case Key.D0:
                    if (_shiftKey.IsDown) _model.ActivateIntellisense(false);
                    break;

                case Key.OemComma:
                    _model.ActivateIntellisense(false);
                    break;
            }
        }

        private new void PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.LeftShift:
                case Key.RightShift:
                    _shiftKey = e;
                    break;
            }
        }

        private void OnSourceInitialized(object sender, System.EventArgs e)
        {
            this.HideMinimizeAndMaximizeButtons();
        }
    }
}