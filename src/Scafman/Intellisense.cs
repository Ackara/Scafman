using System;
using System.Collections.Generic;

namespace Acklann.Scafman
{
    public class Intellisense
    {
        internal const int DEFAULT_LIMIT = 3;

        public static IntellisenseItem[] GetTemplates(string input, string[] templates, int take = DEFAULT_LIMIT)
        {
            if (string.IsNullOrEmpty(input) || templates == null || templates.Length < 1) return new IntellisenseItem[0];

            string item, keyword;
            var matches = new List<IntellisenseItem>(take);
            int index = (input.LastIndexOf(Template.Separators) + 1);

            keyword = input.Substring(index);

            for (int i = 0; i < templates.Length; i++)
                if (!string.IsNullOrEmpty(item = templates[i]))
                    if (item.StartsWith(keyword, StringComparison.InvariantCultureIgnoreCase))
                    {
                        matches.Add(new IntellisenseItem(
                            item,
                            null,
                            string.Concat(input.Substring(0, index), item)
                            ));

                        if (matches.Count >= take) break;
                    }

            return matches.ToArray();
        }

        public static IntellisenseItem[] GetItemGroups(string input, in ItemGroup[] options, int take = DEFAULT_LIMIT)
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

        public static IntellisenseItem[] GetItemGroups(string input, string configurationFile, int take = DEFAULT_LIMIT)
        {
            return GetItemGroups(input, ItemGroup.ReadFile(configurationFile), take);
        }
    }
}