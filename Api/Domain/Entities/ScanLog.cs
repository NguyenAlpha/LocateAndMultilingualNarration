namespace Api.Domain.Entities
{
    /// <summary>
    /// Lưu lịch sử quét QR theo thiết bị để đồng bộ logic phiên với Mobile.
    /// </summary>
    public class ScanLog
    {
        public int Id { get; set; }

        public string DeviceId { get; set; } = string.Empty;

        public string QrRawResult { get; set; } = string.Empty;

        public DateTime LastQrScanAt { get; set; }

        public string? LastScannedSlug { get; set; }

        public DateTime QrSessionExpiry { get; set; }

        public bool HasScannedQr { get; set; } = true;
    }
}
