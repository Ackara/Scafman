namespace Acklann.Templata.Models
{
    [System.Flags]
    public enum SearchContext
    {
        None = 0,
        NPM = 1,
        NuGet = 2,
        ItemGroup = 4
    }
}