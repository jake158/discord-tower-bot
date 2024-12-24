using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Sockets;
using System.Security.Cryptography;
using Tower.Services.Antivirus.Models;
using Tower.Persistence.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Tower.Services.Antivirus;
public class FileScanner(
    ILogger<FileScanner> logger,
    IServiceScopeFactory scopeFactory,
    IOptions<FileScanner.FileScannerOptions> options)
{
    private readonly ILogger<FileScanner> _logger = logger;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly FileScannerOptions _options = options.Value;

    public class FileScannerOptions
    {
        public string AntivirusServerHost { get; set; } = "localhost";
        public int AntivirusServerPort { get; set; } = 5000;
        public string SharedDirectory { get; set; } = "/tmp";
    }

    private static string CalculateMD5(string filePath)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(filePath);
        var hashBytes = md5.ComputeHash(stream);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public async Task<ScanResult> ScanFileAtUrlAsync(Uri fileUri, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ScanResultCache>();

        var cachedResult = await cache.GetScanResultAsync(fileUri, ResourceType.File);

        if (cachedResult != null)
        {
            _logger.LogInformation($"Cache hit for file: {fileUri.AbsoluteUri}");
            return (ScanResult)cachedResult;
        }

        _logger.LogInformation($"Downloading file from URL: {fileUri.AbsoluteUri}");

        string filePath = await DownloadFileAsync(fileUri, cancellationToken);

        _logger.LogDebug($"File downloaded to {filePath}");

        try
        {
            string md5Hash = CalculateMD5(filePath);

            _logger.LogDebug($"MD5 hash calculated: {md5Hash}");

            var md5CachedResult = await cache.GetScanResultAsync(fileUri, ResourceType.File, md5Hash);

            if (md5CachedResult != null)
            {
                _logger.LogInformation($"Cache hit for file: {fileUri.AbsoluteUri}");
                return (ScanResult)md5CachedResult;
            }

            _logger.LogInformation($"Cache miss for file: {fileUri.AbsoluteUri}, starting scan...");

            bool isMalware = await DoAntivirusScanAsync(filePath, cancellationToken);

            return await cache.SaveScanResultAsync(
                url: fileUri,
                type: ResourceType.File,
                scanSource: "Microsoft Defender",
                isMalware: isMalware,
                isSuspicious: false,
                md5Hash: md5Hash
            );
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

        var fileName = Guid.NewGuid().ToString();
        var filePath = Path.Combine(tempDirectory, fileName);

        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(fileUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var fs = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await response.Content.CopyToAsync(fs, cancellationToken);

        return filePath;
    }

    private async Task<bool> DoAntivirusScanAsync(string filePath, CancellationToken cancellationToken)
    {
        using var tcpClient = new TcpClient();

        await tcpClient.ConnectAsync(
            _options.AntivirusServerHost,
            _options.AntivirusServerPort,
            cancellationToken
            );

        using var stream = tcpClient.GetStream();
        using var writer = new StreamWriter(stream) { AutoFlush = true };
        using var reader = new StreamReader(stream);

        var fileName = Path.GetFileName(filePath);

        _logger.LogInformation($"Sending file {fileName} for antivirus scanning.");

        await writer.WriteLineAsync(fileName);

        var response = await reader.ReadLineAsync();
        if (string.IsNullOrEmpty(response))
        {
            throw new InvalidOperationException("Received an empty response from the antivirus server");
        }

        _logger.LogInformation($"Received response from antivirus server: {response}");

        bool isMalware = response.Contains("DETECTED THREATS");
        return isMalware;
    }
}