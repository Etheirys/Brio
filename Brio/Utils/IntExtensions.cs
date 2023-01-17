using Lumina.Excel.GeneratedSheets;

namespace Brio.Utils;

public static class IntExtensions
{
    private static string[] _units = { "Zero", "One", "Two", "Three",
    "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven",
    "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen",
    "Seventeen", "Eighteen", "Nineteen" };
    private static string[] _tens = { "", "", "Twenty", "Thirty", "Forty",
    "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

    public static string ToCharacterName(this int i)
    {
        string words = ToWords(i);
        if (words.Contains(' '))
            return words;

        return "Brio " + words;
    }

    public static string ToWords(this int i)
    {
        string output = i.ToString();
        if (i < 20)
        {
            output =  _units[i];
        }
        else if (i < 100)
        {
            output = _tens[i / 10] + ((i % 10 > 0) ? " " + ToWords(i % 10) : "");
        }

        return output;
    }
}
