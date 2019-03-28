using System.ComponentModel;
using System.IO;

namespace Acklann.Powerbar.ViewModels
{
    public class CommandPromptViewModel : INotifyPropertyChanged
    {
        public string UserInput
        {
            get => _userInput;
            set
            {
                _userInput = value;
                RaisePropertyChangedEvent(nameof(UserInput));
            }
        }

        public string Location
        {
            get => _location;
            set
            {
                _location = value;
                RaisePropertyChangedEvent(nameof(Location));
                RaisePropertyChangedEvent(nameof(CurrentFolder));
            }
        }

        public string CurrentFolder
        {
            get => (Path.GetFileName(_location.TrimEnd('\\', '/')) + '\\');
        }

        public int Top
        {
            get { return _top; }
            set
            {
                _top = value;
                RaisePropertyChangedEvent(nameof(Top));
            }
        }

        public int Left
        {
            get { return _left; }
            set
            {
                _left = value;
                RaisePropertyChangedEvent(nameof(Left));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string SelectPrevious()
        {
            return null;
        }

        public string SelectNext()
        {
            return null;
        }

        protected void RaisePropertyChangedEvent(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Private Members

        private int _top = 40, _left = 40;
        private string _userInput = "", _location = "";

        #endregion Private Members
    }
}