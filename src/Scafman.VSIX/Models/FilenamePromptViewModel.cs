using Acklann.GlobN;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Acklann.Scafman.Models
{
    [XmlRoot(nameof(PromptBase))]
    public sealed class FilenamePromptViewModel : PromptBase, INotifyPropertyChanged
    {
        public FilenamePromptViewModel() : this(GetDefaultFilePath())
        {
        }

        public FilenamePromptViewModel(string stateFilePath) : base(stateFilePath)
        {
        }

        [XmlIgnore]
        public string UserInput
        {
            get => _userInput;
            set
            {
                _userInput = value;
                RaisePropertyChangedEvent();
                RaisePropertyChangedEvent(nameof(FullPath));
            }
        }

        [XmlIgnore]
        public string CurrenntDirectory
        {
            get => _projectDirectory;
            set
            {
                _projectDirectory = value;
                RaisePropertyChangedEvent();
                RaisePropertyChangedEvent(nameof(FolderName));
            }
        }

        [XmlIgnore]
        public string FolderName
        {
            get => string.Concat(Path.GetFileName(CurrenntDirectory).Trim('\\', '/'), '\\');
        }

        [XmlIgnore]
        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                RaisePropertyChangedEvent();
            }
        }

        [XmlIgnore]
        public bool HasValidInput
        {
            get => _isValid;
            set
            {
                _isValid = value;
                RaisePropertyChangedEvent(nameof(HasValidInput));
            }
        }

        [XmlIgnore]
        public string FullPath
        {
            get
            {
                if (string.IsNullOrEmpty(_userInput)) return null;
                else return ((Glob)_userInput).ExpandPath(_projectDirectory);
            }
        }

        public static FilenamePromptViewModel Restore(string stateFilePath = default)
        {
            if (stateFilePath == default) stateFilePath = GetDefaultFilePath();
            if (!File.Exists(stateFilePath)) return new FilenamePromptViewModel(stateFilePath);

            using (var stream = new FileStream(stateFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var serializer = new XmlSerializer(typeof(FilenamePromptViewModel));
                var model = (FilenamePromptViewModel)serializer.Deserialize(stream);
                model.StateFilePath = stateFilePath;
                return model;
            }
        }

        public static Task<FilenamePromptViewModel> RestoreAsync(string documentPath = default) => Task.Run(() => Restore(documentPath));

        public void Initialize(string cwd = default, string filename = default)
        {
            _projectDirectory = cwd;
            _userInput = filename;
            _isValid = true;
        }

        public bool Validate()
        {
            string x = FullPath;

            if (!Path.IsPathRooted(x))
            {
                Message = "The path is not rooted.";
                return HasValidInput = false;
            }
            else if (!File.Exists(x))
            {
                Message = "A file with the same name already exists.";
                return HasValidInput = false;
            }

            return HasValidInput = true;
        }

        #region Backing Members

        private string _userInput, _projectDirectory, _message;
        private bool _isValid;

        #endregion Backing Members
    }
}