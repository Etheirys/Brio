namespace Brio.Core;

internal static class IntExtensions
{
    private static readonly string[] _units = ["Zero",
        "One",
        "Two",
        "Three",
        "Four",
        "Five",
        "Six",
        "Seven",
        "Eight",
        "Nine",
        "Ten",
        "Eleven",
        "Twelve",
        "Thirteen",
        "Fourteen",
        "Fifteen",
        "Sixteen",
        "Seventeen",
        "Eighteen",
        "Nineteen"];
    private static readonly string[] _tens = ["",
        "",
        "Twenty",
        "Thirty",
        "Forty",
        "Fifty",
        "Sixty",
        "Seventy",
        "Eighty",
        "Ninety"];

    public static string ToWords(this int i, string separator = " ")
    {
        string output = i.ToString();
        if (i < 20)
        {
            output = _units[i];
        }
        else if (i < 100)
        {
            output = _tens[i / 10] + ((i % 10 > 0) ? separator + ToWords(i % 10, separator) : "");
        }

        return output;
    }

    public static string ToBrioName(this int i)
    {
        return $"Brio {i.ToWords("'")}";
    }
}