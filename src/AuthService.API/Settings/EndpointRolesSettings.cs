namespace AuthService.API.Settings
{
    public class EndpointRolesSettings
    {
        public Dictionary<string, Dictionary<string, List<string>>> RolesPerEndpoint { get; set; } = new();
    }
}
