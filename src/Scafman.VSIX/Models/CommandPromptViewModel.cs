using Acklann.GlobN;
using System.Collections.ObjectModel;
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
        public string Solution
        {
            get => _solution;
            set
            {
                _solution = value;
                RaisePropertyChangedEvent(nameof(Solution));
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
                if (_selectedIndex > -1 && _selectedIndex < _searchItems.Count && _searchItems.Count > 0)
                    return _searchItems[_selectedIndex];
                else
                    return null;
            }
        }

        [XmlIgnore]
        public ObservableCollection<SearchResult> Options
        {
            get => _searchItems;
        }

        public static CommandPromptViewModel Restore(string stateFilePath = default)
        {
            return Restore<CommandPromptViewModel>(stateFilePath);
        }

        public static Task<CommandPromptViewModel> RestoreAsync() => Task.Run(() => Restore());

        public void Reset()
        {
            Options.Clear();
            UserInput = string.Empty;
            ChangeContext(SearchContext.Template);

            Task.Run(() => { _templates = Template.GetFiles(ConfigurationPage.UserTemplateDirectories); });

            if (File.Exists(ConfigurationPage.UserItemGroupConfigurationFilePath))
                Task.Run(() =>
                {
                    try { _itemGroups = ItemGroup.ReadFile(ConfigurationPage.UserItemGroupConfigurationFilePath); }
                    catch (System.Runtime.Serialization.SerializationException) { }
                    catch (IOException) { }
                });
        }

        public void MoveUp()
        {
            if (_selectedIndex > 0) SelectedIndex--;
        }

        public void MoveDown()
        {
            if (_selectedIndex < (_searchItems.Count - 1)) SelectedIndex++;
        }

        public void ChangeContext(SearchContext context)
        {
            _context = context;
            System.Diagnostics.Debug.WriteLine($">> {nameof(ChangeContext)}: {context}");
        }

        public void UpdateIntellisense(string input)
        {
            if (string.IsNullOrEmpty(input)) Options.Clear();

            switch (_context)
            {
                default:
                case SearchContext.Template:
                    UpdateIntelliList(Intellisense.GetTemplates(input, _templates, LIMIT));
                    break;

                case SearchContext.ItemGroup:
                    UpdateIntelliList(Intellisense.GetItemGroups(input, _itemGroups, LIMIT));
                    break;
            }

            if (_searchItems.Count > 0) SelectedIndex = 0;
        }

        public void CompleteCommand(string input)
        {
            if (!string.IsNullOrEmpty(SelectedItem?.Command))
            {
                UserInput = SelectedItem.Command;
            }
            else if (HasNoFlieExtension(input))
            {
                string lastItem = Utilities.GetLastSegment(input);
                UserInput = string.Concat(
                    input,
                    Template.GuessFileExtension(
                        Path.GetDirectoryName(lastItem.ExpandPath(GetLocation(lastItem))),
                        _project)
                    );
            }

            Options.Clear();
        }

        private void UpdateIntelliList(IntellisenseItem[] items)
        {
            /// NOTE: To maintain performances we are overwritting the existing items within list
            /// when necessary and then trimming the excess items.

            // Overwrite

            int sidx = _searchItems.Count - 1;
            for (int i = 0; i < items.Length; i++)
            {
                if (i <= sidx) _searchItems[i].Copy(items[i]);
                else _searchItems.Add(SearchResult.CreateFrom(items[i]));
            }

            // Trim

            //if (_searchItems.Count > items.Length)
            for (int i = items.Length; i < _searchItems.Count; i++)
            {
                Options.RemoveAt(i--);
            }

            //while (items.Length < _searchItems.Count) Options.RemoveAt(_searchItems.Count - 1);
        }

        #region Backing Variables

        private readonly ObservableCollection<SearchResult> _searchItems = new ObservableCollection<SearchResult>();

        private readonly Regex _extPattern = new Regex(@"\.[a-z0-9]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private int _selectedIndex;
        private SearchContext _context;
        private volatile string[] _templates;
        private volatile ItemGroup[] _itemGroups;
        private string _userInput = string.Empty, _location = string.Empty, _project = string.Empty, _solution = string.Empty;

        private bool HasNoFlieExtension(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            return _extPattern.IsMatch(input) == false;
        }

        private string GetLocation(string input)
        {
            if (string.IsNullOrEmpty(input)) return _location;

            if (input.StartsWith(@"\\") || input.StartsWith("//"))
                return _solution;
            else if (input.StartsWith(@"\") || input.StartsWith("/"))
                return _project;
            else
                return _location;
        }

        #endregion Backing Variables
    }
}