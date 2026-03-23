namespace Api.Domain.Settings
{
    public class AzureTranslationSettings
    {
        public string Endpoint { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string Provider { get; set; } = "AzureTranslator";
    }
}
