namespace AuthService.API.Models
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public ApiError? Error { get; set; }

        public static ApiResponse<T> Ok(T data) => new()
        {
            Success = true,
            Data = data
        };

        public static ApiResponse<T> Fail(string message, string? code = null, int? statusCode = null) => new()
        {
            Success = false,
            Error = new ApiError
            {
                Message = message,
                Code = code,
                StatusCode = statusCode
            }
        };
    }
}
