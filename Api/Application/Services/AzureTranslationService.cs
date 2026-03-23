using System.Text;
using System.Text.Json;
using Api.Domain.Settings;
using Microsoft.Extensions.Options;

namespace Api.Application.Services
{
    /// <summary>
    /// Dịch nội dung văn bản giữa hai ngôn ngữ bằng Azure Translator.
    /// </summary>
    public interface ITranslationService
    {
        /// <summary>
        /// Dịch văn bản từ ngôn ngữ nguồn sang ngôn ngữ đích.
        /// </summary>
        /// <param name="text">Nội dung cần dịch.</param>
        /// <param name="fromLanguageCode">Mã ngôn ngữ nguồn.</param>
        /// <param name="toLanguageCode">Mã ngôn ngữ đích.</param>
        /// <param name="cancellationToken">Token hủy tác vụ.</param>
        /// <returns>Chuỗi văn bản đã được dịch.</returns>
        Task<string> TranslateAsync(string text, string fromLanguageCode, string toLanguageCode, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Xử lý nghiệp vụ dịch tự động:
    /// - Bỏ qua khi nội dung rỗng.
    /// - Bỏ qua khi ngôn ngữ nguồn và đích trùng nhau.
    /// - Gọi Azure Translator và trích xuất kết quả dịch.
    /// </summary>
    public class AzureTranslationService : ITranslationService
    {
        private readonly HttpClient _httpClient;
        private readonly AzureTranslationSettings _settings;
        private readonly ILogger<AzureTranslationService> _logger;

        public AzureTranslationService(HttpClient httpClient, IOptions<AzureTranslationSettings> settings, ILogger<AzureTranslationService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Thực hiện nghiệp vụ dịch văn bản qua Azure Translator.
        /// </summary>
        /// <param name="text">Nội dung cần dịch.</param>
        /// <param name="fromLanguageCode">Mã ngôn ngữ nguồn.</param>
        /// <param name="toLanguageCode">Mã ngôn ngữ đích.</param>
        /// <param name="cancellationToken">Token hủy tác vụ.</param>
        /// <returns>Chuỗi văn bản sau khi dịch.</returns>
        public async Task<string> TranslateAsync(string text, string fromLanguageCode, string toLanguageCode, CancellationToken cancellationToken = default)
        {
            // Nếu không có nội dung cần dịch thì giữ nguyên dữ liệu đầu vào để tránh gọi API không cần thiết.
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogDebug("Bỏ qua dịch vì nội dung trống.");
                return text;
            }

            // Kiểm tra cấu hình bắt buộc để đảm bảo yêu cầu dịch có thể được gửi tới Azure Translator.
            if (string.IsNullOrWhiteSpace(_settings.Endpoint) || string.IsNullOrWhiteSpace(_settings.Key))
            {
                _logger.LogError("Thiếu cấu hình Azure Translator: Endpoint hoặc Key.");
                throw new InvalidOperationException("Thiếu cấu hình Azure Translator (Endpoint/Key).");
            }

            // Chuẩn hoá mã ngôn ngữ để phù hợp với tham số from/to của Azure Translator.
            var from = NormalizeLanguageCode(fromLanguageCode);
            var to = NormalizeLanguageCode(toLanguageCode);

            // Nếu ngôn ngữ nguồn và đích giống nhau thì không cần dịch, trả lại nguyên văn bản.
            if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Bỏ qua dịch vì ngôn ngữ nguồn và đích trùng nhau: {LanguageCode}.", from);
                return text;
            }

            // Tạo URL gọi API dịch theo đúng định dạng của Azure Translator.
            var endpoint = _settings.Endpoint.TrimEnd('/');
            var requestUri = $"{endpoint}/translate?api-version=3.0&from={from}&to={to}";

            _logger.LogInformation("Bắt đầu dịch văn bản từ {FromLanguage} sang {ToLanguage}.", from, to);

            // Gửi khóa xác thực và vùng triển khai nếu có để Azure chấp nhận request.
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Add("Ocp-Apim-Subscription-Key", _settings.Key);
            if (!string.IsNullOrWhiteSpace(_settings.Region))
            {
                request.Headers.Add("Ocp-Apim-Subscription-Region", _settings.Region);
            }

            // Azure Translator yêu cầu body là mảng các đối tượng có trường Text.
            var payload = new[] { new { Text = text } };
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            // Gửi yêu cầu dịch sang Azure Translator và đọc toàn bộ phản hồi để xử lý lỗi nghiệp vụ.
            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            // Nếu API trả về trạng thái lỗi thì ghi log chi tiết để hỗ trợ truy vết nghiệp vụ và kỹ thuật.
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Dịch thất bại: {StatusCode} - {Content}", response.StatusCode, responseContent);
                throw new InvalidOperationException("Dịch thất bại khi gọi Azure Translator.");
            }

            // Trích xuất bản dịch cuối cùng từ cấu trúc JSON trả về của Azure Translator.
            var translatedText = ExtractTranslatedText(responseContent);
            if (string.IsNullOrWhiteSpace(translatedText))
            {
                _logger.LogWarning("Azure Translator trả về phản hồi hợp lệ nhưng không có nội dung dịch.");
                throw new InvalidOperationException("Không nhận được nội dung dịch từ Azure Translator.");
            }

            _logger.LogInformation("Dịch thành công từ {FromLanguage} sang {ToLanguage}.", from, to);
            return translatedText;
        }

        /// <summary>
        /// Trích xuất nội dung dịch từ JSON phản hồi của Azure Translator.
        /// </summary>
        /// <param name="json">Chuỗi JSON phản hồi từ Azure Translator.</param>
        /// <returns>Văn bản đã dịch; trả về chuỗi rỗng nếu không đọc được dữ liệu.</returns>
        private static string ExtractTranslatedText(string json)
        {
            // Phản hồi của Azure Translator là mảng, phần tử đầu tiên chứa danh sách bản dịch tương ứng.
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
            {
                return string.Empty;
            }

            // Lấy mảng translations để đảm bảo chọn đúng kết quả dịch đầu tiên từ API.
            var translations = root[0].GetProperty("translations");
            if (translations.ValueKind != JsonValueKind.Array || translations.GetArrayLength() == 0)
            {
                return string.Empty;
            }

            // Ưu tiên giá trị text trả về; nếu thiếu thì trả rỗng để tầng trên xử lý theo nghiệp vụ.
            return translations[0].GetProperty("text").GetString() ?? string.Empty;
        }

        /// <summary>
        /// Chuẩn hóa mã ngôn ngữ về dạng ngắn và chữ thường.
        /// </summary>
        /// <param name="languageCode">Mã ngôn ngữ đầu vào.</param>
        /// <returns>Mã ngôn ngữ đã chuẩn hóa.</returns>
        private static string NormalizeLanguageCode(string languageCode)
        {
            // Chuẩn hoá mã ngôn ngữ về dạng ngắn và chữ thường, ví dụ: vi-VN -> vi.
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                return string.Empty;
            }

            var trimmed = languageCode.Trim();
            var separatorIndex = trimmed.IndexOf('-');
            return separatorIndex > 0 ? trimmed[..separatorIndex].ToLowerInvariant() : trimmed.ToLowerInvariant();
        }
    }
}
