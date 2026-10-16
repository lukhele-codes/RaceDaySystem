using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace RaceDay.Api.Tests;

public class ApiSecurityTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ApiSecurityTests(ApiFactory factory) => _factory = factory;

    private record AuthResult(string Token, string Email, string Role);
    private record IdResult(int Id);
    private record EnrolmentRow(int Id, int CategoryId, string Status);

    // ---------- helpers ----------

    private async Task<(HttpClient client, string email)> RegisterAsync(string role)
    {
        var client = _factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@test.com";

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Test User",
            email,
            password = "Password123!",
            role
        });
        response.EnsureSuccessStatusCode();

        var auth = (await response.Content.ReadFromJsonAsync<AuthResult>())!;
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.Token);
        return (client, email);
    }

    private static object NewEvent() => new
    {
        name = "Test Run",
        description = "A test event",
        eventDate = DateTime.UtcNow.AddDays(30),
        location = "Boksburg",
        eventType = "Run",
        distanceKm = 10
    };

    private static async Task<int> CreateEventAsync(HttpClient organiser)
    {
        var response = await organiser.PostAsJsonAsync("/api/events", NewEvent());
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IdResult>())!.Id;
    }

    private static async Task<int> CreateCategoryAsync(HttpClient organiser, int eventId)
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            name = "10km",
            distanceKm = 10,
            maxParticipants = 100,
            entryFee = 150
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IdResult>())!.Id;
    }

    // ---------- registration and login ----------

    [Fact]
    public async Task Register_AsOrganiser_ReturnsTokenAndRole()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Org One",
            email = $"{Guid.NewGuid():N}@test.com",
            password = "Password123!",
            role = "Organiser"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResult>();
        Assert.Equal("Organiser", auth!.Role);
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));
    }

    [Fact]
    public async Task Register_AsAdmin_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Sneaky",
            email = $"{Guid.NewGuid():N}@test.com",
            password = "Password123!",
            role = "Admin"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var (client, email) = await RegisterAsync("Participant");

        var second = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Copy",
            email,
            password = "Password123!",
            role = "Participant"
        });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        var (_, email) = await RegisterAsync("Participant");

        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { email, password = "Password123!" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResult>();
        Assert.False(string.IsNullOrWhiteSpace(auth!.Token));
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var (_, email) = await RegisterAsync("Participant");

        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { email, password = "WrongPassword!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- unauthenticated access ----------

    [Fact]
    public async Task GetEvents_WithoutToken_IsPublic()
    {
        var response = await _factory.CreateClient().GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/events", NewEvent());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _factory.CreateClient().GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- event management and role rejection ----------

    [Fact]
    public async Task CreateEvent_AsOrganiser_ReturnsCreated()
    {
        var (organiser, _) = await RegisterAsync("Organiser");
        var response = await organiser.PostAsJsonAsync("/api/events", NewEvent());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsParticipant_ReturnsForbidden()
    {
        var (participant, _) = await RegisterAsync("Participant");
        var response = await participant.PostAsJsonAsync("/api/events", NewEvent());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_InvalidEventType_ReturnsBadRequest()
    {
        var (organiser, _) = await RegisterAsync("Organiser");
        var response = await organiser.PostAsJsonAsync("/api/events", new
        {
            name = "Bad Type",
            eventDate = DateTime.UtcNow.AddDays(30),
            location = "Boksburg",
            eventType = "Swim",
            distanceKm = 5
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_AsOtherOrganiser_ReturnsForbidden()
    {
        var (owner, _) = await RegisterAsync("Organiser");
        var (other, _) = await RegisterAsync("Organiser");
        var eventId = await CreateEventAsync(owner);

        var response = await other.PutAsJsonAsync($"/api/events/{eventId}", NewEvent());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_AsOwner_ReturnsNoContent()
    {
        var (owner, _) = await RegisterAsync("Organiser");
        var eventId = await CreateEventAsync(owner);

        var response = await owner.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_AsParticipant_ReturnsForbidden()
    {
        var (owner, _) = await RegisterAsync("Organiser");
        var (participant, _) = await RegisterAsync("Participant");
        var eventId = await CreateEventAsync(owner);

        var response = await participant.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- enrolments ----------

    [Fact]
    public async Task Enrol_AsParticipant_IsRecorded()
    {
        var (organiser, _) = await RegisterAsync("Organiser");
        var (participant, _) = await RegisterAsync("Participant");
        var eventId = await CreateEventAsync(organiser);
        var categoryId = await CreateCategoryAsync(organiser, eventId);

        var enrol = await participant.PostAsJsonAsync("/api/enrolments", new { categoryId });
        Assert.Equal(HttpStatusCode.Created, enrol.StatusCode);

        var mine = await participant.GetFromJsonAsync<List<EnrolmentRow>>("/api/enrolments/mine");
        Assert.Contains(mine!, e => e.CategoryId == categoryId && e.Status == "Confirmed");
    }

    [Fact]
    public async Task Enrol_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _factory.CreateClient()
            .PostAsJsonAsync("/api/enrolments", new { categoryId = 1 });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_AsOrganiser_ReturnsForbidden()
    {
        var (organiser, _) = await RegisterAsync("Organiser");
        var eventId = await CreateEventAsync(organiser);
        var categoryId = await CreateCategoryAsync(organiser, eventId);

        var response = await organiser.PostAsJsonAsync("/api/enrolments", new { categoryId });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- results ----------

    [Fact]
    public async Task CaptureResult_AsParticipant_ReturnsForbidden()
    {
        var (participant, _) = await RegisterAsync("Participant");

        var response = await participant.PostAsJsonAsync("/api/results", new
        {
            enrolmentId = 1,
            position = 1
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}