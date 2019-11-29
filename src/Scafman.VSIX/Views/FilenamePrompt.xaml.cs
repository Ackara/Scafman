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

        private void OnKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                default: _model.Validate(); break;

                case System.Windows.Input.Key.Escape:
                    DialogResult = false;
                    Close();
                    break;

                case System.Windows.Input.Key.Enter:
                    DialogResult = _model.Validate();
                    Close();
                    break;
            }
        }

        private void OnWindowSourceInitialized(object sender, System.EventArgs e)
        {
            InputBox.SelectAll();
        }

        #region Backing Members

        private readonly Models.FilenamePromptViewModel _model;

        #endregion Backing Members
    }
}