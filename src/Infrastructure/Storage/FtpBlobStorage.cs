using System.Net;
using RekazDrive.Application.Abstractions;

namespace RekazDrive.Infrastructure.Storage;

public sealed class FtpBlobStorage : IBlobStorage
{
    private readonly string _host; // e.g. ftp://example.com
    private readonly NetworkCredential _cred;

    public FtpBlobStorage(string host, string username, string password)
    {
        _host = host.TrimEnd('/');
        _cred = new NetworkCredential(username, password);
    }

    public async Task SaveAsync(string id, ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        var request = (FtpWebRequest)WebRequest.Create($"{_host}/{EscapeKey(id)}");
        request.Method = WebRequestMethods.Ftp.UploadFile;
        request.Credentials = _cred;
        request.UseBinary = true;
        using var stream = await request.GetRequestStreamAsync();
        await stream.WriteAsync(data, ct);
        using var response = (FtpWebResponse)await request.GetResponseAsync();
        _ = response.StatusDescription;
    }

    public async Task<ReadOnlyMemory<byte>> GetAsync(string id, CancellationToken ct = default)
    {
        var request = (FtpWebRequest)WebRequest.Create($"{_host}/{EscapeKey(id)}");
        request.Method = WebRequestMethods.Ftp.DownloadFile;
        request.Credentials = _cred;
        using var response = (FtpWebResponse)await request.GetResponseAsync();
        using var stream = response.GetResponseStream();
        using var ms = new MemoryStream();
        await stream!.CopyToAsync(ms, ct);
        return new ReadOnlyMemory<byte>(ms.ToArray());
    }

    public async Task<bool> ExistsAsync(string id, CancellationToken ct = default)
    {
        try
        {
            var request = (FtpWebRequest)WebRequest.Create($"{_host}/{EscapeKey(id)}");
            request.Method = WebRequestMethods.Ftp.GetFileSize;
            request.Credentials = _cred;
            using var response = (FtpWebResponse)await request.GetResponseAsync();
            return response.StatusCode == FtpStatusCode.FileStatus;
        }
        catch (WebException ex) when (ex.Response is FtpWebResponse r && r.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
        {
            return false;
        }
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var request = (FtpWebRequest)WebRequest.Create($"{_host}/{EscapeKey(id)}");
        request.Method = WebRequestMethods.Ftp.DeleteFile;
        request.Credentials = _cred;
        using var response = (FtpWebResponse)await request.GetResponseAsync();
        _ = response.StatusDescription;
    }

    private static string EscapeKey(string id)
        => string.Join('/', id.Replace("\\", "/").Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.EscapeDataString));
}

