namespace Acklann.Templata
{
    public readonly struct Command : System.IEquatable<Command>
    {
        public Command(string input, Switch options)
        {
            Input = input;
            Kind = options;
        }

        public readonly string Input;

        public readonly Switch Kind;

        public static Command Parse(string text)
        {
            if (string.IsNullOrEmpty(text)) return new Command(string.Empty, Switch.None);

            int pkg = text.IndexOf(':');

            if (pkg > -1)
            {
                string flag = text.Substring(0, pkg).ToLowerInvariant();
                return new Command(text.Substring(pkg + 1).Trim(), ToSwitch(flag));
            }
            else if (text.EndsWith("/") || text.EndsWith("\\"))
                return new Command(text, Switch.AddFolder);
            else
                return new Command(text, Switch.AddFile);
        }

        public static Switch ToSwitch(string text)
        {
            switch (text.Replace(" ", string.Empty).ToLowerInvariant())
            {
                default: return Switch.None;

                case "":
                case "nuget":
                    return Switch.NugetPackage;

                case "npm": return Switch.NPMPackage;
            }
        }

        public override string ToString()
        {
            switch (Kind)
            {
                case Switch.NugetPackage:
                    return $"nuget:{Input}";

                case Switch.NPMPackage:
                    return $"npm:{Input}";

                default:
                    return Input;
            }
        }

        #region IEquatable

        public override bool Equals(object obj)
        {
            if (obj is Command)
                return Equals((Command)obj);
            else
                return false;
        }

        public bool Equals(Command other)
        {
            return string.Equals(Input, other.Input, System.StringComparison.OrdinalIgnoreCase) && Kind == other.Kind;
        }

        public override int GetHashCode()
        {
            return Input.GetHashCode() ^ Kind.GetHashCode();
        }

        #endregion IEquatable

        #region Operators

        public static bool operator ==(Command x, Command y) => x.Equals(y);

        public static bool operator !=(Command x, Command y) => !x.Equals(y);

        public static implicit operator string(Command obj)
        {
            return obj.ToString();
        }

        public static implicit operator Command(string obj)
        {
            return Command.Parse(obj);
        }

        #endregion Operators
    }
}