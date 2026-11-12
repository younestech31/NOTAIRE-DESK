using NotaireDesk.Domain.Security;
using Xunit;

namespace NotaireDesk.Tests;

public sealed class OfficeSecurityDomainTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 11, 12, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void UsernameIsNormalizedAndWhitespaceIsRejected()
    {
        var user = CreateUser("  Younes.Clerk  ");

        Assert.Equal("Younes.Clerk", user.Username);
        Assert.Equal("YOUNES.CLERK", user.NormalizedUsername);
        Assert.Throws<ArgumentException>(() =>
            OfficeUser.Create("two words", "اسم", null, "hash", CreatedAt));
    }

    [Fact]
    public void FifthFailedLoginLocksAccountForTheConfiguredWindow()
    {
        var user = CreateUser();
        var lockDuration = TimeSpan.FromMinutes(15);

        for (var attempt = 1; attempt < 5; attempt++)
        {
            Assert.False(user.RecordFailedLogin(CreatedAt.AddMinutes(attempt), 5, lockDuration));
            Assert.Equal(attempt, user.FailedLoginAttempts);
        }

        var lockedAt = CreatedAt.AddMinutes(5);
        Assert.True(user.RecordFailedLogin(lockedAt, 5, lockDuration));
        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.True(user.IsLockedOut(lockedAt.AddMinutes(1)));
        Assert.False(user.IsLockedOut(lockedAt.Add(lockDuration).AddSeconds(1)));
    }

    [Fact]
    public void SuccessfulLoginResetsFailedAttemptsAndLockout()
    {
        var user = CreateUser();
        user.RecordFailedLogin(CreatedAt.AddMinutes(1), 5, TimeSpan.FromMinutes(15));
        user.RecordFailedLogin(CreatedAt.AddMinutes(2), 5, TimeSpan.FromMinutes(15));

        user.RecordSuccessfulLogin(CreatedAt.AddMinutes(3));

        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.Null(user.LockoutEndUtc);
    }

    [Fact]
    public void DisabledUserCannotAccumulateFailedLoginAttempts()
    {
        var user = CreateUser();
        user.Disable(CreatedAt.AddMinutes(1));

        Assert.False(user.RecordFailedLogin(
            CreatedAt.AddMinutes(2), 5, TimeSpan.FromMinutes(15)));
        Assert.False(user.IsActive);
        Assert.Equal(0, user.FailedLoginAttempts);
    }

    [Fact]
    public void RoleAssignmentsMustBeUniqueAndNonEmpty()
    {
        var user = CreateUser();
        var roleId = Guid.NewGuid();

        user.ReplaceRoles([roleId]);
        Assert.Single(user.Roles);

        Assert.Throws<ArgumentException>(() => user.ReplaceRoles([roleId, roleId]));
        Assert.Throws<ArgumentException>(() => user.ReplaceRoles([Guid.Empty]));
        Assert.Single(user.Roles);
    }

    [Fact]
    public void SessionStoresHashAndCanBeRevokedOnlyOnce()
    {
        var tokenHash = new string('a', 64);
        var session = UserSession.Create(
            Guid.NewGuid(), tokenHash, CreatedAt, CreatedAt.AddDays(14), "Office PC");
        var replacementId = Guid.NewGuid();

        session.Revoke(CreatedAt.AddMinutes(1), replacementId);

        Assert.Equal(2, session.Version);
        Assert.False(session.IsActive(CreatedAt.AddMinutes(2)));
        Assert.Equal(replacementId, session.ReplacedBySessionId);
        Assert.Throws<InvalidOperationException>(() => session.Revoke(CreatedAt.AddMinutes(3)));
    }

    [Fact]
    public void AuditEventRejectsOversizedMetadata()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AuditEvent.Create(
            null, null, "security.event", "OfficeUser", null,
            AuditOutcome.Failed, CreatedAt, safeMetadata: new string('x', 2001)));
    }

    private static OfficeUser CreateUser(string username = "clerk01") =>
        OfficeUser.Create(username, "كاتب", "Clerc", "test-only-password-hash", CreatedAt);
}
