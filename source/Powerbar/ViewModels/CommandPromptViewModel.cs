using System;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;

namespace Acklann.Powerbar.ViewModels
{
    [XmlRoot(nameof(Powerbar))]
    public class CommandPromptViewModel : INotifyPropertyChanged
    {
        public CommandPromptViewModel() : this(GetDefaultFilePath(), DEFAULT_CAPACITY)
        {
        }

        public CommandPromptViewModel(int capacity = DEFAULT_CAPACITY) : this(GetDefaultFilePath(), capacity)
        {
        }

        public CommandPromptViewModel(string stateFilePath, int capacity = DEFAULT_CAPACITY)
        {
            _stateFilePath = stateFilePath ?? throw new ArgumentNullException(nameof(stateFilePath));
            _commandList = Shell.GetCommands();
            _history = new string[capacity];
        }

        public const int MINIMUM_WIDTH = 300;
        public const int DEFAULT_CAPACITY = 16;
        public const int DEFAULT_POSITION = 100;

        [XmlIgnore]
        public string UserInput
        {
            get => _userInput;
            set
            {
                _userInput = value;
                RaisePropertyChangedEvent(nameof(UserInput));
            }
        }

        [XmlIgnore]
        public string Project
        {
            get => _project;
            set
            {
                _project = value;
                RaisePropertyChangedEvent(nameof(Project));
            }
        }

        [XmlIgnore]
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

        [XmlIgnore]
        public string CurrentFolder
        {
            get => (Path.GetFileName(_location.TrimEnd('\\', '/')) + '\\');
        }

        [XmlAttribute]
        public int Top
        {
            get { return _top; }
            set
            {
                _top = value;
                RaisePropertyChangedEvent(nameof(Top));
            }
        }

        [XmlAttribute]
        public int Left
        {
            get { return _left; }
            set
            {
                _left = value;
                RaisePropertyChangedEvent(nameof(Left));
            }
        }

        [XmlAttribute]
        public int Width
        {
            get { return _width; }
            set
            {
                _width = (value < MINIMUM_WIDTH ? MINIMUM_WIDTH : value);
                RaisePropertyChangedEvent(nameof(Width));
            }
        }

        [XmlAttribute]
        public bool OpenInOtherWindow
        {
            get { return _openInOtherWindow; }
            set
            {
                _openInOtherWindow = value;
                RaisePropertyChangedEvent(nameof(OpenInOtherWindow));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public static CommandPromptViewModel Restore(string stateFilePath = null)
        {
            if (string.IsNullOrEmpty(stateFilePath)) stateFilePath = GetDefaultFilePath();
            if (!File.Exists(stateFilePath)) return new CommandPromptViewModel(stateFilePath);

            using (Stream file = File.OpenRead(stateFilePath))
            {
                var serializer = new XmlSerializer(typeof(CommandPromptViewModel));
                var model = (CommandPromptViewModel)serializer.Deserialize(file);
                model._stateFilePath = stateFilePath;
                return model;
            }
        }

        public string CompleteCommand(string input)
        {
            if (input == null) input = _userInput;

            string temp = input;
            var options = Shell.ExtractOptions(ref temp);

            if (options.HasFlag(Switch.RunCommand))
                UserInput = Shell.CompleteCommand(input, _commandList);
            else if (options.HasFlag(Switch.AddFile))
                UserInput = (!Path.HasExtension(input) ? (input + Template.GetExtension(_project, _location)) : input);

            return _userInput;
        }

        public void Commit(string command = null)
        {
            if (command == null) command = _userInput;
            _selectionIndex = -1;

            if (string.IsNullOrEmpty(command)) return;
            if (_currentIndex < 0 || command != _history[_currentIndex])
            {
                if ((_currentIndex + 1) >= _history.Length) _currentIndex = -1;
                _history[++_currentIndex] = command;
            }
        }

        public string SelectPrevious()
        {
            if (_currentIndex == -1) return string.Empty;
            else if (_selectionIndex == -1) _selectionIndex = _currentIndex;
            else if ((_selectionIndex - 1) < 0)
            {
                _selectionIndex = _history.Length - 1;
                if (_selectionIndex == _currentIndex) _selectionIndex = 0;
                else if (_history[_selectionIndex] == null) _selectionIndex = 0;
            }
            else if (_selectionIndex - 1 != _currentIndex) _selectionIndex--;

            UserInput = _history[_selectionIndex];
            return _userInput;
        }

        public string SelectNext()
        {
            if (_currentIndex == -1) return string.Empty;
            else if (_selectionIndex == -1) _selectionIndex = _currentIndex;
            else if (_selectionIndex >= _history.Length - 1) _selectionIndex = 0;
            else if (_selectionIndex != _currentIndex) _selectionIndex++;

            UserInput = _history[_selectionIndex];
            return _userInput;
        }

        public void Clear()
        {
            _selectionIndex = -1;
            UserInput = string.Empty;
        }

        public void Save()
        {
            string folder = Path.GetDirectoryName(_stateFilePath);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            using (Stream file = File.OpenWrite(_stateFilePath))
            {
                var serializer = new XmlSerializer(typeof(CommandPromptViewModel));
                serializer.Serialize(file, this);
            }
        }

        protected void RaisePropertyChangedEvent(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Private Members

        private readonly string[] _history, _commandList;

        private bool _openInOtherWindow;
        private string _userInput = string.Empty, _location = string.Empty, _stateFilePath, _project = string.Empty;
        private int _top = DEFAULT_POSITION, _left = DEFAULT_POSITION, _width = MINIMUM_WIDTH, _currentIndex = -1, _selectionIndex = -1;

        private static string GetDefaultFilePath() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), nameof(Powerbar), "state.xml");

        #endregion Private Members
    }
}