using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Acklann.Scafman.Models
{
    [XmlRoot(nameof(Scafman))]
    public class CommandPromptViewModel : INotifyPropertyChanged
    {
        public CommandPromptViewModel() : this(GetDefaultFilePath())
        {
        }

        public CommandPromptViewModel(string stateFilePath)
        {
            _stateFilePath = stateFilePath ?? throw new ArgumentNullException(nameof(stateFilePath));
        }

        public const int LIMIT = 5;
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
        public bool UsingDarkTheme
        {
            get { return _usingDarkTheme; }
            set
            {
                _usingDarkTheme = value;
                RaisePropertyChangedEvent(nameof(UsingDarkTheme));
            }
        }

        [XmlIgnore]
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                if (SelectedItem != null) SelectedItem.IsSelected = false;
                _selectedIndex = value;
                RaisePropertyChangedEvent(nameof(SelectedIndex));
                if (SelectedItem != null) SelectedItem.IsSelected = true;
            }
        }

        [XmlIgnore]
        public SearchItem SelectedItem
        {
            get
            {
                if (_selectedIndex > -1 && _selectedIndex < _options.Count && _options.Count > 0)
                    return _options[_selectedIndex];
                else
                    return null;
            }
        }

        [XmlIgnore]
        public ObservableCollection<SearchItem> Options
        {
            get => _options;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public static CommandPromptViewModel Restore()
        {
            string stateFilePath = GetDefaultFilePath();
            if (!File.Exists(stateFilePath)) return new CommandPromptViewModel(stateFilePath);

            using (Stream file = File.OpenRead(stateFilePath))
            {
                var serializer = new XmlSerializer(typeof(CommandPromptViewModel));
                var model = (CommandPromptViewModel)serializer.Deserialize(file);
                model._stateFilePath = stateFilePath;
                return model;
            }
        }

        public static Task<CommandPromptViewModel> RestoreAsync() => Task.Run(() => Restore());

        public void UpdateIntellisense(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                _intellisenseActivated = false;
                return;
            }

            if (_intellisenseActivated) UpdateIntelliList(Intellisense.GetOptions(input, _groups, LIMIT));
            if (_options.Count > 0) SelectedIndex = 0;
        }

        public void CompleteCommand(string input)
        {
            if (_intellisenseActivated) { if (SelectedItem != null) UserInput = SelectedItem.Command; }
            else UserInput = (Helper.CheckIfEndsWithExtension(input) ? input : (input + Template.GuessExtension(_project, _location)));

            Options.Clear();
        }

        public void MoveUp()
        {
            if (_selectedIndex > 0) SelectedIndex--;
        }

        public void MoveDown()
        {
            if (_selectedIndex < (_options.Count - 1)) SelectedIndex++;
        }

        public void Reset()
        {
            _groups = null;
            Options.Clear();
            UserInput = string.Empty;
            _intellisenseActivated = false;
        }

        public void Save()
        {
            string folder = Path.GetDirectoryName(_stateFilePath);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            using (Stream file = new FileStream(_stateFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                var serializer = new XmlSerializer(typeof(CommandPromptViewModel));
                serializer.Serialize(file, this);
            }
        }

        public Task SaveAsync() => Task.Run(() => { try { Save(); } catch (UnauthorizedAccessException) { } });

        public void ActivateIntellisense(bool on)
        {
#pragma warning disable VSTHRD110 // Observe result of async calls
            if (on) Task.Run(() =>
#pragma warning restore VSTHRD110 // Observe result of async calls
            {
                if (File.Exists(ConfigurationPage.UserItemGroupFile))
                    try { _groups = ItemGroup.ReadFile(ConfigurationPage.UserItemGroupFile); }
                    catch (System.Runtime.Serialization.SerializationException) { }
                    catch (IOException) { }
            });

            _intellisenseActivated = on;
            System.Diagnostics.Debug.WriteLine($"{nameof(Scafman)} | intellisense: {_intellisenseActivated}");
        }

        private void UpdateIntelliList(IntellisenseItem[] items)
        {
            int n = _options.Count;
            for (int i = 0; i < items.Length; i++)
            {
                if (i < (n - 1)) _options[i].Copy(items[i]);
                else _options.Add(SearchItem.CreateFrom(items[i]));
            }

            while (items.Length < _options.Count) Options.RemoveAt(_options.Count - 1);
        }

        private void RaisePropertyChangedEvent(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Backing Variables

        private readonly ObservableCollection<SearchItem> _options = new ObservableCollection<SearchItem>();

        private volatile ItemGroup[] _groups;
        private bool _usingDarkTheme, _intellisenseActivated;
        private int _top = DEFAULT_POSITION, _left = DEFAULT_POSITION, _width = MINIMUM_WIDTH, _selectedIndex;
        private string _userInput = string.Empty, _location = string.Empty, _stateFilePath, _project = string.Empty;

        private static string GetDefaultFilePath() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), nameof(Scafman), "state.xml");

        #endregion Backing Variables
    }
}