using System;
using System.ComponentModel;
using System.IO;

namespace Acklann.Powerbar.ViewModels
{
    public class CommandPromptViewModel : INotifyPropertyChanged
    {
        public CommandPromptViewModel(int capacity = 25)
        {
            _history = new string[capacity];
        }

        internal const string DEFAULT_TEXT = "";

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
            if (_selectionIndex == -1) _selectionIndex = _currentIndex;
            else if ((_selectionIndex - 1) < 0)
            {
                _selectionIndex = _history.Length - 1;
                if (_selectionIndex == _currentIndex) _selectionIndex = 0;
            }
            else if (_selectionIndex - 1 != _currentIndex) _selectionIndex--;

            UserInput = _history[_selectionIndex];
            return _userInput;
        }

        public string SelectNext()
        {
            if (_selectionIndex == -1) _selectionIndex = _currentIndex;
            else if (_selectionIndex >= _history.Length - 1) _selectionIndex = 0;
            else if (_selectionIndex != _currentIndex) _selectionIndex++;

            UserInput = _history[_selectionIndex];
            return _userInput;
        }

        public void Commit(string command = null)
        {
            if (command == null) command = _userInput;
            UserInput = DEFAULT_TEXT;
            _selectionIndex = -1;

            if (string.IsNullOrEmpty(command)) return;
            if (_currentIndex < 0 || command != _history[_currentIndex])
            {
                if ((_currentIndex + 1) >= _history.Length) _currentIndex = -1;
                _history[++_currentIndex] = command;
            }
        }

        protected void RaisePropertyChangedEvent(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Private Members

        private readonly string[] _history;
        private readonly string _stateFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), nameof(Powerbar), "state.xml");

        private string _userInput = DEFAULT_TEXT, _location = string.Empty;
        private int _top = 100, _left = 100, _currentIndex = -1, _selectionIndex = -1;

        #endregion Private Members
    }
}