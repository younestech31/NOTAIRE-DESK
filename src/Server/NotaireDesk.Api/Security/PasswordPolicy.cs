namespace NotaireDesk.Api.Security;

public static class PasswordPolicy
{
    public const int MinimumLength = 14;
    public const int MaximumLength = 128;

    public static bool IsValid(string? password) =>
        !string.IsNullOrEmpty(password) &&
        password.Length >= MinimumLength &&
        password.Length <= MaximumLength &&
        !password.Contains('\0');
}
