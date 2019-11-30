using System.Windows;

namespace Acklann.Scafman.Views
{
    public partial class FilenamePrompt : Window
    {
        public FilenamePrompt() : this(null)
        {
        }

        public FilenamePrompt(Models.FilenamePromptViewModel model)
        {
            InitializeComponent();
            if (model != null) DataContext = _model = model;

            SizeToContent = SizeToContent.Height;
            WindowStartupLocation = (_model.Top <= Models.PromptBase.DEFAULT_POSITION ? WindowStartupLocation.CenterOwner : WindowStartupLocation.Manual);
        }

        private void OnKeyPressedDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case System.Windows.Input.Key.Escape:
                    DialogResult = false;
                    Close();
                    break;

                case System.Windows.Input.Key.Enter:
                    DialogResult = _model.Validate(InputBox.Text);
                    Close();
                    break;
            }
        }

        private void OnKeyReleased(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                default: _model.Validate(InputBox.Text); break;

                case System.Windows.Input.Key.Enter:
                case System.Windows.Input.Key.Escape:
                    break;
            }
        }

        private void OnWindowSourceInitialized(object sender, System.EventArgs e)
        {
            Helper.HideMinimizeAndMaximizeButtons(this);
            InputBox.Focus();
            InputBox.SelectAll();
            System.Diagnostics.Debug.WriteLine(nameof(OnWindowSourceInitialized));
        }

        #region Backing Members

        private readonly Models.FilenamePromptViewModel _model;

        #endregion Backing Members
    }
}