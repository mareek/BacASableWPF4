using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;

namespace BacASableWPF4
{
    internal static class CSVHelper
    {
        private readonly static string separator = CultureInfo.CurrentCulture.TextInfo.ListSeparator;

        public static string FormatCsvLine(IEnumerable<string> values)
        {
            return string.Join(separator, values.Select(s => QuoteStringForCsv(s)).ToArray());
        }

        public static string QuoteStringForCsv(string s)
        {
            if (s != null && (s.Contains("\"") || s.Contains(separator)))
            {
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            }
            else
            {
                return s;
            }
        }

        public static void WriteToFile(FileInfo file, IEnumerable<IEnumerable<string>> lines)
        {
            WriteToFile(file, null, lines);
        }

        public static void WriteToFile(FileInfo file, IEnumerable<string> headerColumns, IEnumerable<IEnumerable<string>> lines)
        {
            using (var streamWriter = new StreamWriter(file.FullName, false, new UTF8Encoding(true)))
            {
                if (headerColumns != null)
                {
                    var header = CSVHelper.FormatCsvLine(headerColumns);
                    streamWriter.WriteLine(header);
                }

                foreach (var csvLine in lines)
                {
                    streamWriter.WriteLine(CSVHelper.FormatCsvLine(csvLine));
                }
            }
        }

        public static string FormatDate(DateTime? date)
        {
            if (date.HasValue) return date.Value.ToString("yyyy-MM-dd HH:mm:ss");
            else return null;
        }
    }
}
