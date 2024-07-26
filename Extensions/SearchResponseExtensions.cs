using System.Collections.Generic;
using System.DirectoryServices.Protocols;

namespace MultiFactor.SelfService.Windows.Portal.Extensions
{
    public static class SearchResponseExtensions
    {
        public static List<string> GetAttributeValuesByName(this SearchResponse response, string attributeName)
        {
            var result = new List<string>();
            for (var i = 0; i < response.Entries.Count; i++)
            {
                var entry = response.Entries[i];
                var attribute = entry.Attributes[attributeName];
                if (attribute == null) continue;
                // ReSharper disable once LoopCanBeConvertedToQuery
                // ReSharper disable once ForCanBeConvertedToForeach
                // DirectoryAttribute SHOULD use with [] operator
                for (var index = 0; index < attribute.Count; index++)
                {
                    var t = attribute[index];

                    result.Add(t.ToString());
                }
            }

            return result;
        }
    }
}