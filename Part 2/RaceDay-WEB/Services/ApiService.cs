using RaceDay.Web.Models;
using RaceDay_WEB.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace RaceDay_WEB.Services
{
    public class ApiResult
    {
        public bool Success { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string Error { get; set; } = "";

        // Friendly messages only, never raw API errors.
        public static string Message(HttpStatusCode code) => code switch
        {
            HttpStatusCode.Unauthorized => "Your session has expired. Please log in again.",
            HttpStatusCode.Forbidden => "You do not have permission to do that.",
            HttpStatusCode.NotFound => "We could not find that record.",
            HttpStatusCode.BadRequest => "Some of the details were not accepted. Please check your entries and try again.",
            HttpStatusCode.ServiceUnavailable => "The service is unavailable right now. Please try again shortly.",
            _ => "Something went wrong. Please try again."
        };
    }

    public class ApiResult<T> : ApiResult
    {
        public T? Data { get; set; }
    }

    // All communication with the Part 2 API goes through this class.
    public class ApiService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _http;
        private readonly IHttpContextAccessor _ctx;

        public ApiService(HttpClient http, IConfiguration config, IHttpContextAccessor ctx)
        {
            _http = http;
            _ctx = ctx;
            _http.BaseAddress = new Uri(config["ApiSettings:BaseUrl"]!);
        }

        // Builds a request with the logged-in user's token attached.
        private HttpRequestMessage Build(HttpMethod method, string url, object? body = null)
        {
            var request = new HttpRequestMessage(method, url);
            var token = _ctx.HttpContext?.Session.GetString("Token");
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (body != null)
                request.Content = JsonContent.Create(body);
            return request;
        }

        public async Task<ApiResult<T>> GetAsync<T>(string url)
        {
            try
            {
                var response = await _http.SendAsync(Build(HttpMethod.Get, url));
                if (!response.IsSuccessStatusCode)
                    return new ApiResult<T> { StatusCode = response.StatusCode, Error = ApiResult.Message(response.StatusCode) };

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<T>(json, JsonOptions);
                return new ApiResult<T> { Success = true, StatusCode = response.StatusCode, Data = data };
            }
            catch (HttpRequestException)
            {
                return new ApiResult<T> { StatusCode = HttpStatusCode.ServiceUnavailable, Error = ApiResult.Message(HttpStatusCode.ServiceUnavailable) };
            }
            catch (JsonException)
            {
                return new ApiResult<T> { StatusCode = HttpStatusCode.InternalServerError, Error = ApiResult.Message(HttpStatusCode.InternalServerError) };
            }
        }

        // For POST, PUT and DELETE calls where we only need to know if it worked.
        public async Task<ApiResult> SendAsync(HttpMethod method, string url, object? body = null)
        {
            try
            {
                var response = await _http.SendAsync(Build(method, url, body));
                return new ApiResult
                {
                    Success = response.IsSuccessStatusCode,
                    StatusCode = response.StatusCode,
                    Error = response.IsSuccessStatusCode ? "" : ApiResult.Message(response.StatusCode)
                };
            }
            catch (HttpRequestException)
            {
                return new ApiResult { StatusCode = HttpStatusCode.ServiceUnavailable, Error = ApiResult.Message(HttpStatusCode.ServiceUnavailable) };
            }
        }

        // ---------- Auth ----------

        // Returns the JWT token, or null if login failed.
        public async Task<string?> LoginAsync(string email, string password)
        {
            var response = await _http.PostAsJsonAsync("api/auth/login", new { email, password });
            if (!response.IsSuccessStatusCode) return null;

            var body = await response.Content.ReadAsStringAsync();
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.String)
                    return root.GetString();

                if (root.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in root.EnumerateObject())
                    {
                        if (prop.Name.Equals("token", StringComparison.OrdinalIgnoreCase) ||
                            prop.Name.Equals("accessToken", StringComparison.OrdinalIgnoreCase))
                            return prop.Value.GetString();
                    }
                }
                return null;
            }
            catch (JsonException)
            {
                return body.Trim().Trim('"');
            }
        }

        public async Task<UserProfile?> GetProfileAsync(string token)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "api/users/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadFromJsonAsync<UserProfile>();
        }

        public async Task<(bool Success, string Error)> RegisterAsync(RegisterViewModel model)
        {
            var response = await _http.PostAsJsonAsync("api/auth/register", new
            {
                firstName = model.FirstName,
                lastName = model.LastName,
                email = model.Email,
                password = model.Password,
                role = model.Role
            });

            if (response.IsSuccessStatusCode) return (true, "");

            var body = await response.Content.ReadAsStringAsync();
            return (false, $"({(int)response.StatusCode}) {body}");
        }
    }
}