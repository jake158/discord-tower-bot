using Microsoft.Extensions.Logging;
using System.IO.Pipes;

namespace Tower.Services.Antivirus;
public class FileScanner
{
    private readonly ILogger<FileScanner> _logger;

    public FileScanner(ILogger<FileScanner> logger)
    {
        _logger = logger;
    }

    public async Task<ScanResult> ScanFileAtUrlAsync(Uri fileUri)
    {
        if (!fileUri.IsFile)
        {
            throw new ArgumentException("The provided URI is not a file", nameof(fileUri));
        }

        _logger.LogInformation($"Downloading file from URL: {fileUri.AbsoluteUri}");

        string filePath = await DownloadFileAsync(fileUri);

        _logger.LogInformation($"File downloaded to {filePath}, starting scan...");

        var result = await DoAntivirusScanAsync(filePath);
        File.Delete(filePath);

        return result;
    }

    private static async Task<string> DownloadFileAsync(Uri fileUri)
    {
        var filePath = Path.Combine(Path.GetTempPath(), Path.GetFileName(fileUri.LocalPath));

        using (HttpClient client = new())
        {
            var response = await client.GetAsync(fileUri);
            response.EnsureSuccessStatusCode();

            using var fs = new FileStream(filePath, FileMode.Create);
            await response.Content.CopyToAsync(fs);
        }
        return filePath;
    }

    private async Task<ScanResult> DoAntivirusScanAsync(string filePath)
    {
        _logger.LogInformation($"Sending file {filePath} for antivirus scanning via named pipes");

        var scanResult = new ScanResult(filePath, isMalware: false, isSuspicious: false);

        try
        {
            using var pipeClient = new NamedPipeClientStream(".", "TowerScanPipe", PipeDirection.InOut);
            await pipeClient.ConnectAsync();

            using var writer = new StreamWriter(pipeClient);
            using var reader = new StreamReader(pipeClient);
            await writer.WriteLineAsync(filePath);
            await writer.FlushAsync();

            var response = await reader.ReadLineAsync();
            ArgumentNullException.ThrowIfNull(response, nameof(response));

            _logger.LogInformation($"Received response from host scanner: {response}");

            if (response.Contains("DETECTED THREATS"))
            {
                scanResult = new ScanResult(filePath, isMalware: true, isSuspicious: false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error scanning file {filePath} via named pipes: {ex.Message}");
        }
        return scanResult;
    }
}