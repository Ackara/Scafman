using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Acklann.Scafman.Models
{
    public class SearchResult : INotifyPropertyChanged
    {
        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                RaisePropertyChangedEvent(nameof(Title));
            }
        }

        public string ToolTip
        {
            get { return _tooltip; }
            set
            {
                _tooltip = value;
                RaisePropertyChangedEvent(nameof(ToolTip));
            }
        }

        public string Command
        {
            get { return _command; }
            set
            {
                _command = value;
                RaisePropertyChangedEvent(nameof(Command));
            }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                RaisePropertyChangedEvent(nameof(IsSelected));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public static SearchResult CreateFrom(IntellisenseItem item)
        {
            return new SearchResult
            {
                _title = item.Title,
                _command = item.FullText,
                _tooltip = item.Description
            };
        }

        public void Copy(IntellisenseItem item)
        {
            Title = item.Title;
            Command = item.FullText;
            ToolTip = item.Description;
        }

        protected void RaisePropertyChangedEvent([CallerMemberName]string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Private Members

        private bool _isSelected;
        private string _title, _tooltip, _command;

        #endregion Private Members
    }
}