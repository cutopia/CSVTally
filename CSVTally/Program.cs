using System;
using System.Collections.Generic;

// Following the assumptions for CSV format as specified in:
// http://www.creativyst.com/Doc/Articles/CSV/CSV01.htm#FileFormat
namespace CSVTally
{
    class MainClass
    {
        private const string COLUMN_ID = "Cost, Initial";
        // We make the assumption that we have US style decimals.
        // A fancier version of this tool could have a command line parameter 
        // to provide an override.
        private const char DECIMAL_SYMBOL = '.';
        
        /// <summary>
        /// The entry point of the program, where the program control starts and ends.
        /// Accepts filenames as command line arguments. Attempts to read each file
        /// and total up the values found in the COLUMN_ID specified data column.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        public static void Main(string[] args)
        {
            float sum = 0;
            int totalValues = 0;
            if (args.Length > 0)
            {
                foreach (string arg in args)
                {
                    string text;
                    try
                    {
                        text = System.IO.File.ReadAllText(arg);
                    } catch (System.IO.FileNotFoundException e)
                    {
                        Console.WriteLine(e.GetType() + ": " + e.Message + " Filename: " + arg);
                        continue;
                    }
                    List<List<string>> parsedCSV = ParseCSV(text);
                    Console.WriteLine("File: " + arg + ": " + (parsedCSV.Count - 1) + " data entries found");
                    if (parsedCSV.Count > 1)
                    {
                        var columnResult = SumSpecifiedColumn(FindColumnIndex(COLUMN_ID, parsedCSV), parsedCSV);
                        sum += columnResult.Item1;
                        // Some effort made here to correlate actual numbers found with what we will divide 
                        // by for our average instead of just using the count of data lines in the list.
                        // This gives a little more tolerance for mangled data.
                        totalValues += columnResult.Item2;
                    }
                }
            }
            Console.WriteLine("Sum of all values found for " + COLUMN_ID + ": " + sum.ToString());
            if (totalValues > 0)
            {
                Console.WriteLine("Average of those " + totalValues + " values: " + (sum / totalValues).ToString());
            }
        }

        /// <summary>
        /// Finds the index of the specified column. Checks if each header entry contains the specified columnIdentifier string.
        /// </summary>
        /// <returns>The column index.</returns>
        /// <param name="columnIdentifier">Column identifier.</param>
        /// <param name="parsedCSV">Parsed csv.</param>
        private static int FindColumnIndex(string columnIdentifier, List<List<string>> parsedCSV)
        {
            List<string> headers = parsedCSV[0];
            for (int i = 0; i < headers.Count; i++)
            {
                if (headers[i].Contains(columnIdentifier))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Given a column index, goes through each row of the csv and totals up the value in that column.
        /// </summary>
        /// <returns>The sum and total number of values summed for the given csv file</returns>
        /// <param name="colIndex">Col index.</param>
        /// <param name="parsedCSV">Parsed csv.</param>
        private static (float, int) SumSpecifiedColumn(int colIndex, List<List<string>> parsedCSV)
        {
            float sum = 0;
            int totalSummed = 0;
            if (colIndex > 0)
            {
                for (int i = 1; i < parsedCSV.Count; i++)
                {
                    if (parsedCSV[i].Count >= (colIndex + 1))
                    {
                        try
                        {
                            sum += ExtractNumberFromString(parsedCSV[i][colIndex], DECIMAL_SYMBOL);
                            totalSummed++;
                        } catch(System.FormatException e)
                        {
                            // Don't count this value for average since it was not a valid number.
                            Console.WriteLine(e.GetType() + ": Omitting line due to invalid data.");
                        }
                    }
                }
            }
            return (sum, totalSummed);
        }

        /// <summary>
        /// Given a potentially very messy string, clean it up and try to convert it to a float number.
        /// </summary>
        /// <returns>The number from string.</returns>
        /// <param name="str">String.</param>
        /// <param name="decimalSymbol">Decimal symbol.</param>
        private static float ExtractNumberFromString(string str, char decimalSymbol)
        {
            string cleaned = "";
            // float.Parse can be configured to be pretty tolerant of garbage, 
            // but it seems like a brutal cleansing of the data might be safest.
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == decimalSymbol)
                {
                    cleaned += '.';
                }
                else if (str[i] >= 0x30 && str[i] <= 0x39)
                {
                    cleaned += str[i];
                }
                else if (str[i] == '-')
                {
                    if (cleaned.Length == 0)
                    {
                        cleaned += '-';
                    }
                }
            }
            try
            {
                return float.Parse(cleaned, System.Globalization.CultureInfo.InvariantCulture);
            } catch(System.FormatException e)
            {
                Console.WriteLine(e.GetType() + ": "+ e.Message + " Data: " +  str);
                throw;
            }
        }

        /// <summary>
        /// Convert the raw text of a csv file into a list of lists.
        /// Each element of the top level of the list represents a row of data fields.
        /// The individual data fields for each row are broken up into a sublist.
        /// </summary>
        /// <returns>The csv.</returns>
        /// <param name="csv">Csv.</param>
        private static List<List<string>> ParseCSV(string csv)
        {
            List<List<string>> lines = new List<List<string>>();
            List<string> fields = new List<string>();
            char[] csvChars = csv.ToCharArray();
            bool inQuotes = false;
            string builtString = "";
            for (int i = 0; i < csvChars.Length; i++)
            {
                if (csvChars[i] == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }
                if (csvChars[i] == ',' && !inQuotes)
                {
                    fields.Add(builtString);
                    builtString = "";
                    continue;
                }
                // linefeed
                if ((csvChars[i] == '\r' || csvChars[i] == '\n') && !inQuotes)
                {
                    if (builtString.Length > 0)
                    {
                        fields.Add(builtString);
                        builtString = "";
                        lines.Add(fields);
                        fields = new List<string>();
                        continue;
                    }
                }
                builtString += csvChars[i];
            }
            return lines;
        }
    }
}