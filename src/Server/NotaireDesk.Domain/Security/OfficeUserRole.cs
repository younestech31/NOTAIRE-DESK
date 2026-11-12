namespace NotaireDesk.Domain.Security;

public sealed class OfficeUserRole
{
    private OfficeUserRole() { }

    internal OfficeUserRole(Guid userId, Guid roleId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id is required.", nameof(userId));
        if (roleId == Guid.Empty)
            throw new ArgumentException("Role id is required.", nameof(roleId));
        UserId = userId;
        RoleId = roleId;
    }

    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
}
