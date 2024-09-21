using Microsoft.Extensions.Logging;
using System.IO.Pipes;

namespace Tower.Services.Antivirus;
public class FileScanner
{
    private readonly ILogger<FileScanner> _logger;
    private NamedPipeClientStream? _pipeClient;
    private StreamReader? _reader;
    private StreamWriter? _writer;

    public FileScanner(ILogger<FileScanner> logger)
    {
        _logger = logger;
        InitializePipeConnection();
    }

    private void InitializePipeConnection()
    {
        _pipeClient = new NamedPipeClientStream(
            serverName: ".",
            pipeName: "TowerScanPipe",
            direction: PipeDirection.InOut,
            options: PipeOptions.Asynchronous);

        try
        {
            _pipeClient.Connect(1000);
            _writer = new StreamWriter(_pipeClient) { AutoFlush = true };
            _reader = new StreamReader(_pipeClient);
            _logger.LogInformation("Connected to the antivirus server");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to connect to the antivirus server: {ex.Message}");
            _pipeClient = null;
        }
    }

    public async Task<ScanResult> ScanFileAtUrlAsync(Uri fileUri, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Downloading file from URL: {fileUri.AbsoluteUri}");

        string filePath = await DownloadFileAsync(fileUri, cancellationToken);

        _logger.LogInformation($"File downloaded to {filePath}, starting scan...");

        try
        {
            var result = await DoAntivirusScanAsync(filePath);
            return result;
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static async Task<string> DownloadFileAsync(Uri fileUri, CancellationToken cancellationToken)
    {
        // TODO: Add handling file naming conflicts
        var filePath = Path.Combine(Path.GetTempPath(), Path.GetFileName(fileUri.LocalPath));

        using (HttpClient client = new())
        {
            var response = await client.GetAsync(fileUri, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var fs = new FileStream(filePath, FileMode.Create);
            await response.Content.CopyToAsync(fs, cancellationToken);
        }
        return filePath;
    }

    private async Task<ScanResult> DoAntivirusScanAsync(string filePath)
    {
        var scanResult = new ScanResult(filePath, isMalware: false, isSuspicious: false);

        try
        {
            if (_pipeClient == null || !_pipeClient.IsConnected)
            {
                _logger.LogWarning("Pipe client is not connected. Reconnecting...");
                InitializePipeConnection();
            }
            ArgumentNullException.ThrowIfNull(_writer, nameof(_writer));
            ArgumentNullException.ThrowIfNull(_reader, nameof(_reader));

            _logger.LogInformation($"Sending file {filePath} for antivirus scanning.");

            await _writer.WriteLineAsync(filePath);

            var response = await _reader.ReadLineAsync();
            if (string.IsNullOrEmpty(response))
            {
                throw new InvalidOperationException("Received an empty response from the antivirus server.");
            }

            _logger.LogInformation($"Received response from antivirus server: {response}");

            if (response.Contains("DETECTED THREATS"))
            {
                scanResult = new ScanResult(filePath, isMalware: true, isSuspicious: false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error scanning file {filePath}: {ex.Message}");
        }
        return scanResult;
    }

    ~FileScanner()
    {
        _writer?.Dispose();
        _reader?.Dispose();
        _pipeClient?.Dispose();
    }
}
