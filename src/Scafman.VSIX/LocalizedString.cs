namespace Acklann.Scafman
{
    public class LocalizedString
    {
        public const string WindowTitleFormat = ("{0} | " + Metadata.Name);
        public const string StatusBarFormat = (Metadata.Name + ": {0}");

        public static string GetStatus(string message) => string.Format(StatusBarFormat, message);

        public static string GetWindowTitle(string message)
        {
            if (string.IsNullOrEmpty(message))
                return Metadata.Name;
            else
                return string.Format(WindowTitleFormat, message);
        }

    }
}