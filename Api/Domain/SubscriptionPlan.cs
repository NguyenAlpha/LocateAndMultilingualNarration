namespace Api.Domain
{
    public static class SubscriptionPlan
    {
        public const string Free  = "Free";
        public const string Basic = "Basic";
        public const string Pro   = "Pro";

        public static int GetMaxStalls(string plan) => plan switch
        {
            Basic => 3,
            Pro   => int.MaxValue,
            _     => 1  // Free hoặc unknown
        };

        public static bool AllowsTts(string plan) => plan != Free;

        public static decimal GetPrice(string plan) => plan switch
        {
            Basic => 199_000m,
            Pro   => 499_000m,
            _     => 0m
        };
    }
}
