using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.DataAccess.Storage;

namespace SchoolERP.Api.Controllers.Files;

/// <summary>
/// Serves generated PDFs kept in the database. No login token is needed -- the signed,
/// short-lived link handed out by the receipt/report-card/certificate endpoints is the
/// permission, the same way an Azure SAS link was.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private readonly IFileStoreReader _files;
    private readonly FileLinkSigner _signer;

    public FilesController(IFileStoreReader files, FileLinkSigner signer)
    {
        _files = files;
        _signer = signer;
    }

    [HttpGet("{container}/{**path}")]
    public async Task<IActionResult> Download(string container, string path, [FromQuery] long expires, [FromQuery] string? sig, CancellationToken ct)
    {
        if (!_signer.IsValid(container, path, expires, sig, DateTimeOffset.UtcNow))
            return NotFound();

        var file = await _files.ReadAsync(container, path, ct);
        if (file is null)
            return NotFound();

        // Open in the browser's PDF viewer rather than downloading. The API's usual
        // "default-src 'none'" CSP stops some viewers rendering the file, so this response
        // keeps only the anti-framing rule.
        Response.Headers.ContentSecurityPolicy = "frame-ancestors 'none'";
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers.ContentDisposition = $"inline; filename=\"{Path.GetFileName(path)}\"";
        return File(file.Content, file.ContentType);
    }
}
