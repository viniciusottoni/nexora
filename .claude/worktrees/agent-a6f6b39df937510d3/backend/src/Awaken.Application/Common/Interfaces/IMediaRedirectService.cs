namespace Awaken.Application.Common.Interfaces;

/// <summary>
/// Resolve uma URL presigned para leitura de mídia privada no bucket.
/// </summary>
public interface IMediaRedirectService
{
    string CreatePresignedReadUrl(string key);
}
