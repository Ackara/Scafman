using System.Runtime.Serialization;

namespace Acklann.Templata
{
    [System.Diagnostics.DebuggerDisplay("{GetDebuggerDisplay()}")]
    public struct ItemGroup
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "fileList")]
        public string[] FileList { get; set; }

        internal string GetDebuggerDisplay()
        {
            return $"{Name}: {string.Join(",", FileList)}";
        }
    }
}