using Acklann.GlobN;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Acklann.Scafman.Models
{
    [XmlRoot(nameof(PromptBase))]
    public sealed class FilenamePromptViewModel : PromptBase
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
            return Restore<FilenamePromptViewModel>(stateFilePath);
        }

        public static Task<FilenamePromptViewModel> RestoreAsync(string documentPath = default) => Task.Run(() => Restore(documentPath));

        public string GetFullPath(string filename)
        {
            if (string.IsNullOrEmpty(filename)) return null;
            else return ((Glob)filename).ExpandPath(_projectDirectory);
        }

        public void Initialize(string cwd = default, string filename = default)
        {
            _projectDirectory = cwd;
            _userInput = filename;
            _isValid = true;

            Validate(filename);
        }

        public bool Validate(string name)
        {
            string x = GetFullPath(name);

            if (!Template.ValidateFilename(Path.GetFileName(x), out string error))
            {
                Message = error;
                return HasValidInput = false;
            }
            else if (!Path.IsPathRooted(x))
            {
                Message = "The path is not rooted.";
                return HasValidInput = false;
            }
            else if (File.Exists(x))
            {
                Message = "The file already exist.";
                return HasValidInput = false;
            }

            Message = null;
            return HasValidInput = true;
        }

        #region Backing Members

        private string _userInput, _projectDirectory, _message;
        private bool _isValid;

        #endregion Backing Members
    }
}