using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Acklann.Scafman.Models
{
    public abstract class PromptBase : INotifyPropertyChanged
    {
        public PromptBase(string filePath)
        {
            StateFilePath = filePath;
        }

        public const int MINIMUM_WIDTH = 300;
        public const int DEFAULT_POSITION = 100;

        protected string StateFilePath;

        public event PropertyChangedEventHandler PropertyChanged;

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

        public void Save()
        {
            string folder = Path.GetDirectoryName(StateFilePath);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            using (var stream = new FileStream(StateFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                var serializer = new XmlSerializer(GetType());
                serializer.Serialize(stream, this);
                stream.Flush();
            }
        }

        public Task SaveAsync() => Task.Run(() => { try { Save(); } catch (UnauthorizedAccessException) { } });

        internal static string GetDefaultFilePath(string name = "user-prompt-state.xml")
        {
#if DEBUG
            return Path.Combine(Path.GetTempPath(), Metadata.Name, name);
#else
            return Path.Combine(Path.GetDirectoryName(typeof(PromptBase).Assembly.Location), Metadata.Name, name);
#endif
        }

        protected virtual void RaisePropertyChangedEvent([CallerMemberName]string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Backing Members

        private int _top = DEFAULT_POSITION, _left = DEFAULT_POSITION, _width = MINIMUM_WIDTH;

        #endregion Backing Members
    }
}