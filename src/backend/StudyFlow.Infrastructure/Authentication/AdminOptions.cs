namespace StudyFlow.Infrastructure.Authentication;

public sealed class AdminOptions
{
    public const string SectionName = "Admin";
    public string[] BootstrapEmails { get; init; } = [];

    public bool IsBootstrapAdmin(string email) =>
        BootstrapEmails.Contains(email.Trim(), StringComparer.OrdinalIgnoreCase);
}
