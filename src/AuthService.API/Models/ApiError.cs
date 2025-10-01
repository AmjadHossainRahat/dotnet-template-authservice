namespace AuthService.API.Models
{
    public class ApiError
    {
        public string Message { get; set; } = string.Empty;
        public string? Code { get; set; }
        public int? StatusCode { get; set; }
        public IDictionary<string, string[]>? ValidationErrors { get; set; }
    }
}
