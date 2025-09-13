using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using RekazDrive.Application.Abstractions;

namespace RekazDrive.Infrastructure.Storage;

public sealed class S3HttpBlobStorage : IBlobStorage
{
    private readonly HttpClient _http;
    private readonly string _bucket;
    private readonly string _endpointHost; // e.g. s3.amazonaws.com or play.min.io
    private readonly string _region; // e.g. us-east-1
    private readonly string _accessKey;
    private readonly string _secretKey;
    private readonly bool _usePathStyle;

    public S3HttpBlobStorage(HttpClient http, string bucket, string endpointHost, string region, string accessKey, string secretKey, bool usePathStyle = false)
    {
        _http = http;
        _bucket = bucket;
        _endpointHost = endpointHost;
        _region = region;
        _accessKey = accessKey;
        _secretKey = secretKey;
        _usePathStyle = usePathStyle;
    }

    public async Task SaveAsync(string id, ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        var (uri, hostHeader) = BuildUri(id);
        using var req = new HttpRequestMessage(HttpMethod.Put, uri);
        req.Content = new ByteArrayContent(data.ToArray());
        var payloadHash = ToHexString(SHA256.HashData(data.Span));
        AddCommonAwsHeaders(req, hostHeader, payloadHash);
        SignV4(req, payloadHash);
        var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
    }

    public async Task<ReadOnlyMemory<byte>> GetAsync(string id, CancellationToken ct = default)
    {
        var (uri, hostHeader) = BuildUri(id);
        using var req = new HttpRequestMessage(HttpMethod.Get, uri);
        const string payloadHash = "UNSIGNED-PAYLOAD";
        AddCommonAwsHeaders(req, hostHeader, payloadHash);
        SignV4(req, payloadHash);
        var res = await _http.SendAsync(req, ct);
        if (res.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new FileNotFoundException("Blob not found", id);
        res.EnsureSuccessStatusCode();
        var bytes = await res.Content.ReadAsByteArrayAsync(ct);
        return new ReadOnlyMemory<byte>(bytes);
    }

    public async Task<bool> ExistsAsync(string id, CancellationToken ct = default)
    {
        var (uri, hostHeader) = BuildUri(id);
        using var req = new HttpRequestMessage(HttpMethod.Head, uri);
        const string payloadHash = "UNSIGNED-PAYLOAD";
        AddCommonAwsHeaders(req, hostHeader, payloadHash);
        SignV4(req, payloadHash);
        var res = await _http.SendAsync(req, ct);
        return res.IsSuccessStatusCode;
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var (uri, hostHeader) = BuildUri(id);
        using var req = new HttpRequestMessage(HttpMethod.Delete, uri);
        const string payloadHash = "UNSIGNED-PAYLOAD";
        AddCommonAwsHeaders(req, hostHeader, payloadHash);
        SignV4(req, payloadHash);
        var res = await _http.SendAsync(req, ct);
        if (res.StatusCode != System.Net.HttpStatusCode.NoContent && res.StatusCode != System.Net.HttpStatusCode.OK && res.StatusCode != System.Net.HttpStatusCode.NotFound)
            res.EnsureSuccessStatusCode();
    }

    private (Uri Uri, string Host) BuildUri(string id)
    {
        // URL-encode key but keep slashes for path-like ids
        var safeKey = string.Join('/', id.Split('/').Select(Uri.EscapeDataString));
        string host = _usePathStyle ? _endpointHost : $"{_bucket}.{_endpointHost}";
        string path = _usePathStyle ? $"/{_bucket}/{safeKey}" : $"/{safeKey}";
        var uri = new Uri($"https://{host}{path}");
        return (uri, host);
    }

    private void AddCommonAwsHeaders(HttpRequestMessage req, string host, string payloadSha256)
    {
        req.Headers.Host = host;
        var now = DateTimeOffset.UtcNow;
        req.Headers.TryAddWithoutValidation("x-amz-date", now.ToString("yyyyMMddTHHmmssZ"));
        req.Headers.TryAddWithoutValidation("x-amz-content-sha256", payloadSha256);
    }

    private void SignV4(HttpRequestMessage req, string payloadSha256)
    {
        // Build canonical request
        var amzDate = req.Headers.GetValues("x-amz-date").First();
        var date = amzDate.Substring(0, 8);
        var canonicalUri = req.RequestUri!.AbsolutePath;
        var canonicalQuery = req.RequestUri!.Query.TrimStart('?');
        var orderedQuery = string.Join('&', canonicalQuery.Split('&', StringSplitOptions.RemoveEmptyEntries).OrderBy(x => x));

        var headers = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["host"] = req.Headers.Host!,
            ["x-amz-content-sha256"] = payloadSha256,
            ["x-amz-date"] = amzDate
        };
        var canonicalHeaders = string.Join('\n', headers.Select(kvp => $"{kvp.Key}:{kvp.Value}")) + "\n";
        var signedHeaders = string.Join(';', headers.Keys);
        var canonicalRequest = string.Join('\n', new[]
        {
            req.Method.Method,
            canonicalUri,
            orderedQuery,
            canonicalHeaders,
            signedHeaders,
            payloadSha256
        });

        var scope = $"{date}/{_region}/s3/aws4_request";
        var stringToSign = string.Join('\n', new[]
        {
            "AWS4-HMAC-SHA256",
            amzDate,
            scope,
            ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest)))
        });

        byte[] signingKey = GetSigningKey(_secretKey, date, _region, "s3");
        var signature = ToHexString(HmacSHA256(signingKey, Encoding.UTF8.GetBytes(stringToSign)));
        var credential = $"Credential={_accessKey}/{scope}";
        var header = $"AWS4-HMAC-SHA256 Credential={_accessKey}/{scope}, SignedHeaders={signedHeaders}, Signature={signature}";
        req.Headers.TryAddWithoutValidation("Authorization", header);
    }

    private static byte[] HmacSHA256(byte[] key, byte[] data)
    {
        using var h = new HMACSHA256(key);
        return h.ComputeHash(data);
    }

    private static byte[] GetSigningKey(string secretKey, string date, string region, string service)
    {
        var kDate = HmacSHA256(Encoding.UTF8.GetBytes("AWS4" + secretKey), Encoding.UTF8.GetBytes(date));
        var kRegion = HmacSHA256(kDate, Encoding.UTF8.GetBytes(region));
        var kService = HmacSHA256(kRegion, Encoding.UTF8.GetBytes(service));
        var kSigning = HmacSHA256(kService, Encoding.UTF8.GetBytes("aws4_request"));
        return kSigning;
    }

    private static string ToHexString(byte[] bytes)
        => Convert.ToHexString(bytes).ToLowerInvariant();
}

