namespace Acklann.Scafman
{
    public readonly struct Package
    {
        public Package(string name, string version)
        {
            Name = name;
            Version = version;
            IsTool = false;
        }

        public readonly string Name, Version;
        public readonly bool IsTool;

        public static Package Parse(string text)
        {
            if (string.IsNullOrEmpty(text)) return new Package();

            string[] segments = text.Split('-');
            return new Package(segments[0], (segments.Length > 1 ? segments[1] : string.Empty));
        }

        public override string ToString()
        {
            return (string.IsNullOrEmpty(Version)) ? Name : $"{Name}-{Version}";
        }

        #region Operators

        public static implicit operator Package(string text) => Parse(text);

        public static implicit operator string(Package obj) => obj.ToString();

        #endregion Operators
    }
}