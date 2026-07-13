using System;

namespace Core.Interfaces;

public class DataImportResult
{
    public int TotalProcessed { get; set; }
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public List<string> Errors { get; set; } = [];
    public TimeSpan Duration { get; set; }
}

public interface IDataImportService
{
    Task<DataImportResult> ImportFromDatasetAsync(string datasetBasePath, string targetImagePath);
}
