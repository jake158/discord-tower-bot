using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Sockets;
using Tower.Services.Antivirus.Models;

namespace Tower.Services.Antivirus;
public class FileScanner
{
    private readonly ILogger<FileScanner> _logger;
    private readonly FileScannerOptions _options;

    public FileScanner(ILogger<FileScanner> logger, IOptions<FileScannerOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public class FileScannerOptions
    {
        public string Address { get; set; } = "localhost";
        public int Port { get; set; } = 5000;
        public string SharedDirectory { get; set; } = "/tmp";
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

    private async Task<string> DownloadFileAsync(Uri fileUri, CancellationToken cancellationToken)
    {
        var tempDirectory = _options.SharedDirectory;

        Directory.CreateDirectory(tempDirectory);
        var fileName = Path.GetFileName(fileUri.LocalPath);
        var filePath = Path.Combine(tempDirectory, fileName);

        using HttpClient client = new();
        var response = await client.GetAsync(fileUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var fs = new FileStream(filePath, FileMode.Create);
        await response.Content.CopyToAsync(fs, cancellationToken);

        return filePath;
    }

    private async Task<ScanResult> DoAntivirusScanAsync(string filePath)
    {
        var scanResult = new ScanResult(filePath, isMalware: false, isSuspicious: false);

        using (var tcpClient = new TcpClient())
        {
            await tcpClient.ConnectAsync(_options.Address, _options.Port);

            using var stream = tcpClient.GetStream();
            using var writer = new StreamWriter(stream) { AutoFlush = true };
            using var reader = new StreamReader(stream);

            var fileName = Path.GetFileName(filePath);

            _logger.LogInformation($"Sending file {fileName} for antivirus scanning.");

            await writer.WriteLineAsync(fileName);

            var response = await reader.ReadLineAsync();
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

        return scanResult;
    }
}

