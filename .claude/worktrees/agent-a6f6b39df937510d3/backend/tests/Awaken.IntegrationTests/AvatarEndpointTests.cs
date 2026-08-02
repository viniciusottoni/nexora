using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Hunter;
using Awaken.Contracts.Users;
using Awaken.Domain.Entities.Avatars;
using Awaken.Domain.Entities.Inventory;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

using Microsoft.Extensions.Configuration;
namespace Awaken.IntegrationTests;

/// US-234: cobre o catalogo de avatares internos (GET /api/users/me/avatars)
/// e a selecao controlada (PUT /api/users/me/avatar) - CA-001/CA-002/CA-003.
public class AvatarEndpointTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("awaken")
        .WithUsername("awaken")
        .WithPassword("awaken_test_password")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PostgreSQL"] = _postgres.GetConnectionString(),
                });
            });
        });

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        await dbContext.Database.MigrateAsync();

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private async Task<string> RegisterAndGetTokenAsync(string email)
    {
        var payload = new
        {
            email,
            password = "Str0ngPass!",
            name = "Hunter",
            language = "pt-BR"
        };
        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body!.AccessToken;
    }

    private async Task GrantItemAsync(string email, string itemKey, int quantity = 1)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);

        var item = InventoryItem.Create(user.Id, itemKey, quantity);
        dbContext.Add(item);
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAvatarsReturnsUnauthorizedWhenNotAuthenticated()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/users/me/avatars");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static string UniqueEmail(string prefix) => $"{prefix}-{Guid.NewGuid():N}@awaken.app";

    [Fact]
    public async Task CA001_ProfileWithoutPriorSelectionUsesPredictableDefaultAvatarKey()
    {
        var token = await RegisterAndGetTokenAsync(UniqueEmail("noavatar"));
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var profileResponse = await _client.GetAsync("/api/hunter/profile");
        profileResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await profileResponse.Content.ReadFromJsonAsync<HunterProfileResponse>();
        profile!.SelectedAvatarKey.Should().Be(AvatarCatalog.DefaultAvatarKey);

        var avatarsResponse = await _client.GetAsync("/api/users/me/avatars");
        avatarsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var avatars = await avatarsResponse.Content.ReadFromJsonAsync<List<AvatarCatalogItemResponse>>();

        avatars.Should().NotBeNull();
        avatars!.Should().Contain(a => a.AvatarKey == AvatarCatalog.DefaultAvatarKey && a.IsSelected && a.IsUnlocked);
        avatars!.Where(a => a.AvatarKey != AvatarCatalog.DefaultAvatarKey).Should().OnlyContain(a => !a.IsSelected);
    }

    [Fact]
    public async Task CA002_SelectingValidUnlockedAvatarReturnsNoContentAndReflectsInSubsequentGet()
    {
        var token = await RegisterAndGetTokenAsync(UniqueEmail("selectavatar"));
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var putResponse = await _client.PutAsJsonAsync(
            "/api/users/me/avatar", new SelectAvatarRequest("avatar_male_1"));
        putResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var avatarsResponse = await _client.GetAsync("/api/users/me/avatars");
        var avatars = await avatarsResponse.Content.ReadFromJsonAsync<List<AvatarCatalogItemResponse>>();
        avatars!.Single(a => a.AvatarKey == "avatar_male_1").IsSelected.Should().BeTrue();

        var profileResponse = await _client.GetAsync("/api/hunter/profile");
        var profile = await profileResponse.Content.ReadFromJsonAsync<HunterProfileResponse>();
        profile!.SelectedAvatarKey.Should().Be("avatar_male_1");
    }

    [Fact]
    public async Task CA003_SelectingUnknownAvatarKeySimulatingExternalUrlUploadIsRejected()
    {
        var token = await RegisterAndGetTokenAsync(UniqueEmail("uploadattempt"));
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var putResponse = await _client.PutAsJsonAsync(
            "/api/users/me/avatar",
            new SelectAvatarRequest("https://external-cdn.example.com/my-custom-photo.png"));

        putResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CA003_SelectingEmptyAvatarKeyIsRejectedAsValidationError()
    {
        var token = await RegisterAndGetTokenAsync(UniqueEmail("emptyavatar"));
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var putResponse = await _client.PutAsJsonAsync(
            "/api/users/me/avatar", new SelectAvatarRequest(string.Empty));

        putResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task SelectingPackAvatarWithoutPackReturnsConflictAvatarLocked()
    {
        var token = await RegisterAndGetTokenAsync(UniqueEmail("lockedavatar"));
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var putResponse = await _client.PutAsJsonAsync(
            "/api/users/me/avatar", new SelectAvatarRequest("avatar_male_pack_striker"));

        putResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var avatarsResponse = await _client.GetAsync("/api/users/me/avatars");
        var avatars = await avatarsResponse.Content.ReadFromJsonAsync<List<AvatarCatalogItemResponse>>();
        avatars!.Single(a => a.AvatarKey == "avatar_male_pack_striker").IsUnlocked.Should().BeFalse();
    }

    // RN-005: o pack libera a mesma tematica para os dois sexos - concedido o
    // pack, tanto o avatar masculino quanto o feminino ficam selecionaveis.
    [Fact]
    public async Task SelectingPackAvatarAfterGrantingPackSucceedsForBothSexes()
    {
        var email = UniqueEmail("unlockedafterpack");
        var token = await RegisterAndGetTokenAsync(email);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var beforeGrantResponse = await _client.PutAsJsonAsync(
            "/api/users/me/avatar", new SelectAvatarRequest("avatar_male_pack_striker"));
        beforeGrantResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        await GrantItemAsync(email, ItemKeys.PackStriker);

        var maleAfterGrantResponse = await _client.PutAsJsonAsync(
            "/api/users/me/avatar", new SelectAvatarRequest("avatar_male_pack_striker"));
        maleAfterGrantResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var femaleAfterGrantResponse = await _client.PutAsJsonAsync(
            "/api/users/me/avatar", new SelectAvatarRequest("avatar_female_pack_striker"));
        femaleAfterGrantResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var avatarsResponse = await _client.GetAsync("/api/users/me/avatars");
        var avatars = await avatarsResponse.Content.ReadFromJsonAsync<List<AvatarCatalogItemResponse>>();
        avatars!.Single(a => a.AvatarKey == "avatar_male_pack_striker").IsUnlocked.Should().BeTrue();
        avatars!.Single(a => a.AvatarKey == "avatar_female_pack_striker").IsUnlocked.Should().BeTrue();
        avatars!.Single(a => a.AvatarKey == "avatar_female_pack_striker").IsSelected.Should().BeTrue();
    }

    // US-234 RN-002: apos informar sexo biologico "feminino" no onboarding
    // (sem imagem do Google), o avatar padrao efetivo passa a ser o feminino.
    [Fact]
    public async Task DefaultAvatarFollowsBiologicalSexInformedDuringOnboarding()
    {
        var email = UniqueEmail("femaledefault");
        var token = await RegisterAndGetTokenAsync(email);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var patchResponse = await _client.PatchAsJsonAsync(
            "/api/users/me/profile/onboarding",
            new { age = 28, heightCm = 165, weightKg = 60, biologicalSex = "feminino" });
        patchResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var profileResponse = await _client.GetAsync("/api/hunter/profile");
        var profile = await profileResponse.Content.ReadFromJsonAsync<HunterProfileResponse>();
        profile!.SelectedAvatarKey.Should().Be(AvatarCatalog.DefaultFemaleAvatarKey);

        var avatarsResponse2 = await _client.GetAsync("/api/users/me/avatars");
        var avatars2 = await avatarsResponse2.Content.ReadFromJsonAsync<List<AvatarCatalogItemResponse>>();
        avatars2!.Single(a => a.AvatarKey == AvatarCatalog.DefaultFemaleAvatarKey).IsSelected.Should().BeTrue();
    }
}
