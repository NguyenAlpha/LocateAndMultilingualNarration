using Api.Domain.Settings;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;

namespace Api.Application.Services
{
    /// <summary>
    /// Sinh SAS URL (Shared Access Signature) có thời hạn cho file audio trên Azure Blob Storage.
    ///
    /// Lý do tồn tại:
    ///   Blob container được cấu hình <c>PublicAccessType.None</c> — không public.
    ///   Để client (Mobile / Web) tải được file audio, API phải cấp một URL tạm thời
    ///   kèm chữ ký số (SAS) thay vì để blob public vĩnh viễn.
    ///
    /// Cách hoạt động:
    ///   SAS URL = blob URI + query string chứa thời hạn + quyền + chữ ký HMAC-SHA256.
    ///   Azure xác minh chữ ký mỗi khi client gọi URL — không cần token JWT, không cần tài khoản.
    ///   Sau khi hết hạn, URL trả 403 và không thể gia hạn (phải gọi API để lấy URL mới).
    ///
    /// Tại sao 24h không phá Mobile:
    ///   Mobile dùng <see cref="AudioCacheService"/> — tải file về local ngay khi nhận URL.
    ///   Sau khi tải xong, audio phát từ file local, SAS URL không cần dùng nữa.
    /// </summary>
    public interface IBlobUrlService
    {
        /// <summary>
        /// Sinh SAS URL chỉ-đọc cho một blob.
        /// </summary>
        /// <param name="blobId">
        ///   Tên blob trong container (ví dụ: <c>narration-audio/abc123/20260418.mp3</c>).
        ///   Chính là <c>NarrationAudio.BlobId</c> được lưu trong DB.
        /// </param>
        /// <param name="expiry">
        ///   Thời gian hiệu lực của URL tính từ thời điểm gọi hàm.
        ///   Mặc định 24 giờ — đủ để Mobile hoàn tất download và cache local.
        /// </param>
        /// <returns>
        ///   SAS URL dạng chuỗi nếu sinh thành công;
        ///   <c>null</c> nếu <paramref name="blobId"/> rỗng hoặc thiếu cấu hình Blob Storage.
        /// </returns>
        string? GetSasUrl(string? blobId, TimeSpan? expiry = null);
    }

    /// <inheritdoc cref="IBlobUrlService"/>
    public class BlobUrlService : IBlobUrlService
    {
        private readonly BlobStorageSettings _settings;

        public BlobUrlService(IOptions<BlobStorageSettings> settings)
        {
            _settings = settings.Value;
        }

        /// <inheritdoc/>
        public string? GetSasUrl(string? blobId, TimeSpan? expiry = null)
        {
            // Không thể sinh SAS nếu thiếu blobId hoặc chưa cấu hình Blob Storage.
            if (string.IsNullOrWhiteSpace(blobId)
                || string.IsNullOrWhiteSpace(_settings.ConnectionString)
                || string.IsNullOrWhiteSpace(_settings.ContainerName))
                return null;

            // Tạo BlobClient trỏ đúng tới blob cần cấp quyền truy cập.
            var containerClient = new BlobServiceClient(_settings.ConnectionString)
                .GetBlobContainerClient(_settings.ContainerName);
            var blobClient = containerClient.GetBlobClient(blobId);

            // Cấu hình SAS: chỉ cấp quyền đọc (sp=r) cho đúng blob này, hết hạn sau expiry.
            // Resource = "b" nghĩa là SAS cấp cho một blob cụ thể, không phải cả container.
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _settings.ContainerName,
                BlobName          = blobId,
                Resource          = "b",
                ExpiresOn         = DateTimeOffset.UtcNow.Add(expiry ?? TimeSpan.FromHours(24))
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            // GenerateSasUri ký SAS bằng account key lấy từ ConnectionString,
            // trả về URI đầy đủ kèm query string chứa chữ ký số.
            return blobClient.GenerateSasUri(sasBuilder).ToString();
        }
    }
}
