using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Fellow.Web.UI.Components;
using Fellow.Web.UI.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = AspNet.Security.OAuth.GitHub.GitHubAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddGitHub(options =>
    {
        options.ClientId = builder.Configuration["Fellow:Authentication:GitHub:ClientId"] ?? throw new InvalidOperationException("Authentication:GitHub:ClientId is missing");
        options.ClientSecret = builder.Configuration["Fellow:Authentication:GitHub:ClientSecret"] ?? throw new InvalidOperationException("Authentication:GitHub:ClientSecret is missing");
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddFellowAiServices(builder.Configuration, builder.Environment);

// Configure Forwarded Headers for Codespaces/Proxies
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | 
                               ForwardedHeaders.XForwardedProto | 
                               ForwardedHeaders.XForwardedHost;
    // Trust all proxies (safe for Codespaces dev environment)
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();

app.MapGet("/login", (string? returnUrl, HttpContext context) =>
{
    var properties = new AuthenticationProperties
    {
        RedirectUri = returnUrl ?? "/"
    };
    return Results.Challenge(properties, [AspNet.Security.OAuth.GitHub.GitHubAuthenticationDefaults.AuthenticationScheme]);
});

app.MapGet("/logout", (string? returnUrl, HttpContext context) =>
{
    var properties = new AuthenticationProperties
    {
        RedirectUri = returnUrl ?? "/"
    };
    return Results.SignOut(properties, [CookieAuthenticationDefaults.AuthenticationScheme]);
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
