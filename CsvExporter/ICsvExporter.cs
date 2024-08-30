using System.Collections.Generic;

namespace CsvExporter
{
    public interface ICsvExporter
    {
        void ExportToCsv<T>(IEnumerable<T> items);
    }
}