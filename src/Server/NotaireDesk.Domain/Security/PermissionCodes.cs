namespace NotaireDesk.Domain.Security;

public static class PermissionCodes
{
    public const string UsersRead = "users.read";
    public const string UsersManage = "users.manage";
    public const string RolesRead = "roles.read";
    public const string RolesManage = "roles.manage";
    public const string AuditRead = "audit.read";
    public const string PartiesRead = "parties.read";
    public const string PartiesWrite = "parties.write";
    public const string PropertiesRead = "properties.read";
    public const string PropertiesWrite = "properties.write";
    public const string ContractsRead = "contracts.read";
    public const string ContractsWrite = "contracts.write";
    public const string ContractsSubmit = "contracts.submit";
    public const string ContractsApprove = "contracts.approve";
    public const string ContractsExecute = "contracts.execute";
    public const string ContractsArchive = "contracts.archive";
    public const string DocumentsGenerate = "documents.generate";
    public const string DocumentsExport = "documents.export";
    public const string SystemManage = "system.manage";

    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        UsersRead, UsersManage, RolesRead, RolesManage, AuditRead,
        PartiesRead, PartiesWrite, PropertiesRead, PropertiesWrite,
        ContractsRead, ContractsWrite, ContractsSubmit, ContractsApprove,
        ContractsExecute, ContractsArchive, DocumentsGenerate, DocumentsExport,
        SystemManage
    };
}
