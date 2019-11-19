using System;
using System.Collections.Generic;

namespace Acklann.Templata
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

    public class Intellisense
    {
        internal const int DEFAULT_LIMIT = 3;

        public static IntellisenseItem[] GetOptions(string input, in ItemGroup[] options, int take = DEFAULT_LIMIT)
        {
            if (string.IsNullOrEmpty(input) || options == null || options.Length < 1) return new IntellisenseItem[0];

            int startIndex = input.LastIndexOf('@');
            if (startIndex == -1) return new IntellisenseItem[0];

            var matches = new List<IntellisenseItem>(take);
            string term = input.Substring(startIndex).TrimStart('@', '(', ' '), command;

            int count = 0; ItemGroup item;
            for (int i = 0; i < options.Length; i++)
            {
                item = options[i];
                if (item.Name.Trim().StartsWith(term, StringComparison.OrdinalIgnoreCase))
                {
                    command = string.Concat(input.Substring(0, startIndex), $"@({item.Name})");

                    matches.Add(new IntellisenseItem(item.Name, string.Join(" | ", item.FileList), command));
                    if (++count >= take) break;
                }
            }

            var results = new IntellisenseItem[count];
            matches.CopyTo(0, results, 0, count);
            return results;
        }

        public static IntellisenseItem[] GetOptions(string input, string configurationFile, int take = DEFAULT_LIMIT)
        {
            return GetOptions(input, ItemGroup.ReadFile(configurationFile), take);
        }
    }
}