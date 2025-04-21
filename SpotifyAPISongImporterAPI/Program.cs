using SpotifyAPI.Web;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var clientId = builder.Configuration["User:UserId"]; 
var redirectUri = "https://localhost:5001/";
var (verifier, challenge) = PKCEUtil.GenerateCodes();

var loginRequest = new LoginRequest(
    new Uri(redirectUri),
    clientId,
    LoginRequest.ResponseType.Code
)
{
    CodeChallengeMethod = "S256",
    CodeChallenge = challenge,
    Scope = new[] {
        Scopes.PlaylistModifyPrivate,
        Scopes.PlaylistModifyPublic,
        Scopes.UserReadPrivate
    }
};

app.MapGet("/", () =>
{
    var loginUri = loginRequest.ToUri();

    return Results.Redirect(loginUri.ToString());
});

app.MapGet("/callback", async (HttpContext context) =>
{
    var code = context.Request.Query["code"];

    if (string.IsNullOrEmpty(code))
        return Results.BadRequest("Código não recebido");

    var tokenResponse = await new OAuthClient().RequestToken(
        new PKCETokenRequest(clientId, code!, new Uri(redirectUri), verifier)
    );

    var spotify = new SpotifyClient(tokenResponse.AccessToken);
    var me = await spotify.UserProfile.Current();
    var html = $"<h1>✅ Autenticado como {me.DisplayName}</h1>";

    return Results.Content(html, "text/html");
});

app.Run("https://localhost:5001");
