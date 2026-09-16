using System.Text;
using DevOps.Api.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace DevOps.Api.Auth;

public static class AuthServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformAuth(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.AddSingleton<TokenService>();

        var enabled = configuration.GetValue("Auth:Enabled", false) && !environment.IsEnvironment("Testing");
        if (!enabled)
        {
            services.AddAuthentication(DevelopmentBypassHandler.SchemeName)
                .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, DevelopmentBypassHandler>(
                    DevelopmentBypassHandler.SchemeName,
                    _ => { });
            services.AddAuthorization(options =>
            {
                options.AddPolicy(PlatformRoles.ViewerPolicy, policy => policy.RequireAuthenticatedUser());
                options.AddPolicy(PlatformRoles.OperatorPolicy, policy => policy.RequireAuthenticatedUser());
                options.AddPolicy(PlatformRoles.AdminPolicy, policy => policy.RequireAuthenticatedUser());
            });
            return services;
        }

        var auth = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
        if (string.IsNullOrWhiteSpace(auth.SigningKey) || auth.SigningKey.Length < 32)
        {
            throw new InvalidOperationException("Auth:SigningKey must be at least 32 characters when Auth:Enabled is true.");
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = auth.Issuer,
                    ValidAudience = auth.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(auth.SigningKey)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(PlatformRoles.ViewerPolicy, policy => policy.RequireAuthenticatedUser());
            options.AddPolicy(
                PlatformRoles.OperatorPolicy,
                policy => policy.RequireRole(PlatformRoles.Operator, PlatformRoles.Administrator));
            options.AddPolicy(
                PlatformRoles.AdminPolicy,
                policy => policy.RequireRole(PlatformRoles.Administrator));
        });

        return services;
    }
}
