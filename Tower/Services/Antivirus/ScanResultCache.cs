using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tower.Persistence;
using Tower.Persistence.Entities;
using Tower.Services.Antivirus.Models;

namespace Tower.Services.Antivirus;
public class ScanResultCache(ILogger<ScanResultCache> logger, TowerDbContext db)
{
    private readonly ILogger<ScanResultCache> _logger = logger;
    private readonly TowerDbContext _db = db;

    private static string HashLink(Uri uri)
    {
        var inputBytes = Encoding.UTF8.GetBytes(uri.ToString());
        using SHA256 sha256 = SHA256.Create();
        var inputHash = sha256.ComputeHash(inputBytes);
        return Convert.ToHexString(inputHash).ToLowerInvariant();
    }

    private static ScanResult MapToScanResult(ScannedLinkEntity linkEntity, Uri url)
    {
        return new ScanResult(url.ToString(), linkEntity.ScanSource, linkEntity.Id, linkEntity.IsMalware, linkEntity.IsSuspicious);
    }

    public async Task<ScanResult?> GetScanResultAsync(Uri url, ResourceType expectedType, string? md5Hash = null)
    {
        // TODO: Cache misses for identical urls, ensure url is standardized
        var query = _db.ScannedLinks.AsNoTracking();

        if (expectedType == ResourceType.File && md5Hash != null)
        {
            query = query.Where(x => x.MD5hash == md5Hash);
        }
        else
        {
            query = query.Where(x => x.LinkHash == HashLink(url));
        }

        var existingResult = await query.SingleOrDefaultAsync();

        if (existingResult == null)
        {
            _logger.LogDebug($"Cache miss for url: {url}, md5: {md5Hash}");
            return null;
        }

        if (existingResult.Type != expectedType)
        {
            _logger.LogWarning($"Conflict: ResourceType mismatch for url: {url}. Expected: {expectedType}, Actual: {existingResult.Type}");
            return null;
        }

        if (existingResult.ExpireTime.HasValue && existingResult.ExpireTime.Value < DateTimeOffset.UtcNow)
        {
            _logger.LogDebug($"Expired scan result for url: {url}. Expire time: {existingResult.ExpireTime}");
            return null;
        }

        _logger.LogDebug($"Cache hit for url: {url}, md5: {md5Hash}");
        return MapToScanResult(existingResult, url);
    }

    public async Task<ScanResult> SaveScanResultAsync(
        Uri url,
        ResourceType type,
        string scanSource,
        bool isMalware,
        bool isSuspicious,
        string? md5Hash = null,
        DateTimeOffset? expireTime = null)
    {
        var linkHash = HashLink(url);
        _logger.LogInformation($"Saving scan result for link: {url} with hash: {linkHash}");

        var entity = await _db.ScannedLinks
            .SingleOrDefaultAsync(x => x.LinkHash == linkHash)
            ?? new ScannedLinkEntity { LinkHash = linkHash };

        entity.Type = type;
        entity.ScanSource = scanSource;
        entity.IsMalware = isMalware;
        entity.IsSuspicious = isSuspicious;
        entity.ScannedAt = DateTime.UtcNow;
        entity.MD5hash = md5Hash;
        entity.ExpireTime = expireTime;

        try
        {
            _db.ScannedLinks.Update(entity);
            await _db.SaveChangesAsync();
            _logger.LogInformation($"Successfully saved scan result for link: {url.AbsoluteUri} with hash: {linkHash}");

            return new ScanResult()
            {
                Link = url.ToString(),
                ScannedLinkId = entity.Id,
                ScanSource = scanSource,
                IsMalware = isMalware,
                IsSuspicious = isSuspicious,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to save scan result for link: {url.AbsoluteUri} with hash: {linkHash}");

            return new ScanResult()
            {
                Link = url.ToString(),
                ScannedLinkId = null,
                ScanSource = scanSource,
                IsMalware = isMalware,
                IsSuspicious = isSuspicious,
            };
        }
    }
}
