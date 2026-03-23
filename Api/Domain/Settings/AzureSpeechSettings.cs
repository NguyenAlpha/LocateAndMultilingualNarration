namespace Api.Domain.Settings
{
    public class AzureSpeechSettings
    {
        public string Endpoint { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string DefaultVoice { get; set; } = string.Empty;
        public string Provider { get; set; } = "AzureTts";
    }
}
