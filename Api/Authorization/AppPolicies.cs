namespace Api.Authorization
{
    public static class AppPolicies
    {
        public const string AdminOnly = "AdminOnly";
        public const string AdminOrBusinessOwner = "AdminOrBusinessOwner";
    }
}
