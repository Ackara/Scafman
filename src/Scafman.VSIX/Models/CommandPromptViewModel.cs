﻿using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Acklann.Scafman.Models
{
    [XmlRoot(nameof(PromptBase))]
    public sealed class CommandPromptViewModel : PromptBase
    {
        public CommandPromptViewModel() : this(GetDefaultFilePath())
        {
        }

        public CommandPromptViewModel(string stateFilePath) : base(stateFilePath)
        {
        }

        public const int LIMIT = 5;
        public const int DEFAULT_CAPACITY = 16;

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
        public SearchResult SelectedItem
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
        public ObservableCollection<SearchResult> Options
        {
            get => _options;
        }

        public static CommandPromptViewModel Restore(string stateFilePath = default)
        {
            return Restore<CommandPromptViewModel>(stateFilePath);
        }

        public static Task<CommandPromptViewModel> RestoreAsync() => Task.Run(() => Restore());

        public void Reset()
        {
            _groups = null;
            Options.Clear();
            UserInput = string.Empty;
            _intellisenseActivated = false;
        }

        public void MoveUp()
        {
            if (_selectedIndex > 0) SelectedIndex--;
        }

        public void MoveDown()
        {
            if (_selectedIndex < (_options.Count - 1)) SelectedIndex++;
        }

        public void ChangeContext(SearchContext context)
        {
            switch (_context = context)
            {
                case SearchContext.None:
                    break;

                case SearchContext.ItemGroup:
                    break;

                case SearchContext.Template:
                    break;

                case SearchContext.NuGet:
                    break;

                case SearchContext.NPM:
                    break;
            }
        }

        public void UpdateIntellisense(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                _intellisenseActivated = false;
                return;
            }

            if (_intellisenseActivated) UpdateIntelliList(Intellisense.GetItemGroups(input, _groups, LIMIT));
            if (_options.Count > 0) SelectedIndex = 0;
        }

        public void CompleteCommand(string input)
        {
            if (_intellisenseActivated) { if (SelectedItem != null) UserInput = SelectedItem.Command; }
            else UserInput = (CheckIfEndsWithExtension(input) ? input : (input + Template.GuessFileExtension(_project, _location)));

            Options.Clear();
        }

        public void ShowIntellisense() => SetIntellisense(true);

        public void HideIntellisense() => SetIntellisense(false);

        private void SetIntellisense(bool on)
        {
            if (on && File.Exists(ConfigurationPage.UserItemGroupConfigurationFilePath)) Task.Run(() =>
            {
                try { _groups = ItemGroup.ReadFile(ConfigurationPage.UserItemGroupConfigurationFilePath); }
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
                else _options.Add(SearchResult.CreateFrom(items[i]));
            }

            while (items.Length < _options.Count) Options.RemoveAt(_options.Count - 1);
        }

        #region Backing Variables

        private readonly ObservableCollection<SearchResult> _options = new ObservableCollection<SearchResult>();

        private int _selectedIndex;
        private SearchContext _context;
        private bool _intellisenseActivated;
        private volatile ItemGroup[] _groups;
        private string _userInput = string.Empty, _location = string.Empty, _project = string.Empty;

        private static bool CheckIfEndsWithExtension(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            else return Regex.IsMatch(path, @"\.[a-z0-9]+$", RegexOptions.IgnoreCase);
        }

        #endregion Backing Variables
    }
}