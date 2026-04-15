namespace Api.Domain.Entities
{
    public static class TtsJobStatus
    {
        public const string None       = "None";        // không cần TTS (Free plan)
        public const string Pending    = "Pending";     // đã queue, chưa xử lý
        public const string Processing = "Processing";  // đang xử lý
        public const string Completed  = "Completed";   // hoàn thành
        public const string Failed     = "Failed";      // thất bại
    }
}
