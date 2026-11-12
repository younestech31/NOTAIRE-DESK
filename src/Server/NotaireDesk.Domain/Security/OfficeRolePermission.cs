namespace NotaireDesk.Domain.Security;

public sealed class OfficeRolePermission
{
    private OfficeRolePermission() { }

    internal OfficeRolePermission(Guid roleId, string permission)
    {
        if (roleId == Guid.Empty)
            throw new ArgumentException("Role id is required.", nameof(roleId));
        if (!PermissionCodes.All.Contains(permission))
            throw new ArgumentException("Unknown permission code.", nameof(permission));
        RoleId = roleId;
        Permission = permission;
    }

    public Guid RoleId { get; private set; }
    public string Permission { get; private set; } = string.Empty;
}
