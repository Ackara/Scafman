namespace Acklann.Scafman
{
    public readonly struct IntellisenseItem
    {
        public IntellisenseItem(string title, string description, string text)
        {
            Title = title;
            FullText = text;
            Description = description;
        }

        public readonly string Title, Description, FullText;

        public override string ToString() => $"{Title}: {Description}";
    }
}