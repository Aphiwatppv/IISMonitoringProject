using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CsvExporter
{
    public class CsvExporter : ICsvExporter
    {
        private readonly string _directoryPath;

        public CsvExporter(string directoryPath = null)
        {
            // Set the directory path; default to "historical" in the current directory if not provided
            _directoryPath = directoryPath ?? Path.Combine(Environment.CurrentDirectory, "historical");

            // Create the directory if it doesn't exist
            if (!Directory.Exists(_directoryPath))
            {
                Directory.CreateDirectory(_directoryPath);
            }
        }

        public void ExportToCsv<T>(IEnumerable<T> items)
        {
            if (items == null || !items.Any())
                throw new ArgumentException("The collection is null or empty.", nameof(items));

            var csvLines = new List<string>();

            // Get the properties of the model type
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // Generate the file name with the current date
            string fileName = $"Log_{DateTime.Now:yyyyMMdd}.csv";
            string filePath = Path.Combine(_directoryPath, fileName);

            bool fileExists = File.Exists(filePath);

            // Create the header row only if the file does not exist
            if (!fileExists)
            {
                var header = string.Join(",", properties.Select(p => p.Name));
                csvLines.Add(header);
            }

            // Create the data rows
            foreach (var item in items)
            {
                var values = properties.Select(p => p.GetValue(item, null));
                var line = string.Join(",", values.Select(v => v != null ? v.ToString().Replace(",", " ") : string.Empty));
                csvLines.Add(line);
            }

            // Append all lines to the file
            File.AppendAllLines(filePath, csvLines, Encoding.UTF8);
        }
    }
}
