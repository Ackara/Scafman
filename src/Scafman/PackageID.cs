namespace Acklann.Scafman
{
    public readonly struct PackageID
    {
        public PackageID(string name, string version)
        {
            Name = name;
            Version = version;
            IsTool = false;
        }

        public readonly string Name, Version;
        public readonly bool IsTool;

        public static PackageID Parse(string text)
        {
            if (string.IsNullOrEmpty(text)) return new PackageID();

            string[] segments = text.Split('-');
            return new PackageID(segments[0], (segments.Length > 1 ? segments[1] : string.Empty));
        }

        public override string ToString()
        {
            return (string.IsNullOrEmpty(Version)) ? Name : $"{Name}-{Version}";
        }

        #region Operators

        public static implicit operator PackageID(string text) => Parse(text);

        public static implicit operator string(PackageID obj) => obj.ToString();

        #endregion Operators
    }
}