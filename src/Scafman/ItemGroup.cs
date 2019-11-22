using System.IO;
using System.Runtime.Serialization;

namespace Acklann.Scafman
{
    [System.Diagnostics.DebuggerDisplay("{GetDebuggerDisplay()}")]
    public struct ItemGroup
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "fileList")]
        public string[] FileList { get; set; }

        public static ItemGroup[] ReadFile(string filePath)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException($"Could not find item-group configuraiton file at '{filePath}'.");

            using (Stream file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(ItemGroup[]));
                return (ItemGroup[])serializer.ReadObject(file);
            }
        }

        internal string GetDebuggerDisplay()
        {
            return $"{Name}: {string.Join(",", FileList)}";
        }
    }
}