using System.Text;

namespace Brio.Core;

internal static class IntExtensions
{
    public static string ToWords(this int number, string separator = " ")
    {
        string[] ones = { "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
        string[] tens = { "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

        StringBuilder result = new();

        if(number < 100)
        {
            if(number < 20)
            {
                result.Append(ones[number]);
            }
            else
            {
                int tenPart = number / 10;
                int onePart = number % 10;
                result.Append(tens[tenPart]);

                if(onePart > 0)
                {
                    result.Append(separator);
                    result.Append(ones[onePart]);
                }
            }
        }
        else
        {
            result.Append(ones[number / 100]);

            int remainder = number % 100;
            if(remainder == 0)
            {
                result.Append(separator);
                result.Append("Hundred");
            }
            else
            {
                result.Append(separator);
                result.Append("Hundred");
                result.Append(separator);
                result.Append(ToWords(remainder, ""));
            }
        }

        return result.ToString();
    }

    public static string ToBrioName(this int i)
    {
        string result = ToWords(i, " ");

        if(!result.Contains(' '))
            return "Brio " + result;

        return result;
    }
}
