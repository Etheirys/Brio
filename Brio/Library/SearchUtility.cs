using System.Text.RegularExpressions;

namespace Brio.Library;


public static class SearchUtility
{
    public static string[] ToQuery(string input)
    {
        return input.Split(' ');
    }

    public static bool Matches(object input, string[]? query) => Matches(input.ToString(), query);

    public static bool Matches(string? input, string[]? query)
    {
        if(input == null)
            return false;

        if(query == null)
            return true;

        input = input.ToLower();
        input = Regex.Replace(input, @"[^\w\d\s]", string.Empty);

        bool matchesSearch = true;
        foreach(string str in query)
        {
            string strB = str.ToLower();

            // ignore 'the'
            if(strB == "the")
                continue;

            // ignore all symbols
            strB = Regex.Replace(strB, @"[^\w\d\s]", string.Empty);

            // Parse integers as numbers instead of strings
            if(int.TryParse(str, out int v))
            {
                matchesSearch &= input.Contains(v.ToString());
            }
            else
            {
                matchesSearch &= input.Contains(strB);
            }
        }

        if(!matchesSearch)
        {
            return false;
        }

        return true;
    }
}
