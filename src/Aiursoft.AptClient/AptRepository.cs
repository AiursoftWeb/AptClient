using System.IO.Compression;
using System.Security.Cryptography;

namespace Aiursoft.AptClient;

public class AptRepository
{
    public string BaseUrl { get; }
    public string Suite { get; } 
    public string? SignedBy { get; }

    private Dictionary<string, string>? _trustedHashes;
    private bool _isVerified;

    public AptRepository(string baseUrl, string suite, string? signedBy)
    {
        // Ensure BaseUrl ends with /
        BaseUrl = baseUrl.EndsWith("/") ? baseUrl : baseUrl + "/";
        Suite = suite;
        SignedBy = signedBy;
    }

    /// <summary>
    /// Ensures the repository metadata (InRelease) is downloaded and verified.
    /// Uses a semaphore/lock internally if needed, but for simplicity we rely on Task await.
    /// </summary>
    public async Task EnsureVerifiedAsync(HttpClient client, Action<string, long>? progress = null)
    {
        if (_isVerified && _trustedHashes != null) return;

        var inReleaseUrl = $"{BaseUrl}dists/{Suite}/InRelease";
        
        // 1. Download InRelease data as bytes to preserve exact signature
        byte[] inReleaseBytes;
        try 
        {
            var response = await client.GetAsync(inReleaseUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            var length = response.Content.Headers.ContentLength ?? 0;
            progress?.Invoke(inReleaseUrl, length);
            
            inReleaseBytes = await response.Content.ReadAsByteArrayAsync();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to download InRelease for {Suite}", ex);
        }

        // 2. Verify Signature
        if (!string.IsNullOrWhiteSpace(SignedBy))
        {
            // Assuming AptGpgVerifier is available
            bool isValid = await AptGpgVerifier.VerifyInReleaseAsync(inReleaseBytes, SignedBy);
            if (!isValid)
            {
                 throw new Exception($"GPG Verification failed for {InReleaseUrl} using key {SignedBy}");
            }
        }

        // 3. Parse Hashes
        // InRelease is UTF8 text signed.
        var content = System.Text.Encoding.UTF8.GetString(inReleaseBytes);
        _trustedHashes = DebianPackageParser.ParseInRelease(content);
        _isVerified = true;
    }

    /// <summary>
    /// Downloads a file that MUST exist in the trusted hash list.
    /// </summary>
    public async Task<Stream> GetValidatedStreamAsync(HttpClient client, string relativePath, Action<string, long>? progress = null)
    {
        // Ensure verification
        if (!_isVerified) await EnsureVerifiedAsync(client, progress);

        // relativePath is like "main/binary-amd64/Packages.gz"
        // We check if it is in the trusted hashes
        if (_trustedHashes == null || !_trustedHashes.TryGetValue(relativePath, out var expectedHash))
        {
             // If validation is required (SignedBy is set), we SHOULD expect a hash.
             // If SignedBy is null, maybe we are looser? 
             if (!string.IsNullOrWhiteSpace(SignedBy))
             {
                 throw new Exception($"File {relativePath} is not listed in InRelease checksums! Cannot trust it.");
             }
             expectedHash = null;
        }

        var url = $"{BaseUrl}dists/{Suite}/{relativePath}";
        
        // Download
        var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        var totalBytes = response.Content.Headers.ContentLength ?? 0;
        progress?.Invoke(url, totalBytes);
        
        if (expectedHash == null)
        {
            return await response.Content.ReadAsStreamAsync();
        }

        // Validate Hash (Must read all bytes)
        // For optimization, we can stream and hash, but that requires a CryptoStream wrapper that doesn't just calculate on write but on read.
        // Or simpler: download to memory (if size reasonable) or temp file.
        // Packages.gz can be 10MB+. Memory is fine for modern machines.
        var data = await response.Content.ReadAsByteArrayAsync();
        
        using var sha256 = SHA256.Create();
        var actualHashBytes = sha256.ComputeHash(data);
        var actualHash = BitConverter.ToString(actualHashBytes).Replace("-", "").ToLowerInvariant();

        if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception($"Hash mismatch for {url}.\nExpected: {expectedHash}\nActual:   {actualHash}");
        }

        return new MemoryStream(data);
    }
    
    // Helper to get InRelease URL solely for error reporting
    private string InReleaseUrl => $"{BaseUrl}dists/{Suite}/InRelease";

    public override string ToString() => $"{BaseUrl} ({Suite})";
}
