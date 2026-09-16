namespace DevOps.Api.Auth;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public bool Enabled { get; set; }
    public string Issuer { get; set; } = "devops-platform";
    public string Audience { get; set; } = "devops-platform";
    public string SigningKey { get; set; } = string.Empty;
    public TimeSpan TokenLifetime { get; set; } = TimeSpan.FromHours(8);
    public List<AuthUserOptions> Users { get; set; } = [];
}

public sealed class AuthUserOptions
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "Viewer";
}

public static class PlatformRoles
{
    public const string Viewer = "Viewer";
    public const string Operator = "Operator";
    public const string Administrator = "Administrator";

    public const string ViewerPolicy = "Viewer";
    public const string OperatorPolicy = "Operator";
    public const string AdminPolicy = "Administrator";
}
