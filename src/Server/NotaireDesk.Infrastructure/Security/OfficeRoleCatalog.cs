using NotaireDesk.Domain.Security;

namespace NotaireDesk.Infrastructure.Security;

public sealed record RoleDefinition(
    string Code,
    string NameAr,
    string NameFr,
    IReadOnlyCollection<string> Permissions);

public static class OfficeRoleCatalog
{
    private static readonly string[] AllPermissions = PermissionCodes.All.OrderBy(x => x, StringComparer.Ordinal).ToArray();

    public static IReadOnlyList<RoleDefinition> Definitions { get; } =
    [
        new(SystemRoleCodes.OfficeAdministrator, "مدير المكتب", "Administrateur du bureau", AllPermissions),
        new(SystemRoleCodes.Notary, "الموثق", "Notaire",
        [
            PermissionCodes.UsersRead, PermissionCodes.RolesRead, PermissionCodes.AuditRead,
            PermissionCodes.PartiesRead, PermissionCodes.PartiesWrite,
            PermissionCodes.PropertiesRead, PermissionCodes.PropertiesWrite,
            PermissionCodes.ContractsRead, PermissionCodes.ContractsWrite,
            PermissionCodes.ContractsSubmit, PermissionCodes.ContractsApprove,
            PermissionCodes.ContractsExecute, PermissionCodes.ContractsArchive,
            PermissionCodes.DocumentsGenerate, PermissionCodes.DocumentsExport
        ]),
        new(SystemRoleCodes.OfficeClerk, "كاتب الموثق", "Clerc de notaire",
        [
            PermissionCodes.PartiesRead, PermissionCodes.PartiesWrite,
            PermissionCodes.PropertiesRead, PermissionCodes.PropertiesWrite,
            PermissionCodes.ContractsRead, PermissionCodes.ContractsWrite,
            PermissionCodes.ContractsSubmit, PermissionCodes.DocumentsGenerate,
            PermissionCodes.DocumentsExport
        ]),
        new(SystemRoleCodes.ContractEditor, "محرر العقود", "Rédacteur d'actes",
        [
            PermissionCodes.PartiesRead, PermissionCodes.PropertiesRead,
            PermissionCodes.ContractsRead, PermissionCodes.ContractsWrite,
            PermissionCodes.DocumentsGenerate, PermissionCodes.DocumentsExport
        ]),
        new(SystemRoleCodes.DataEntry, "موظف إدخال البيانات", "Agent de saisie",
        [
            PermissionCodes.PartiesRead, PermissionCodes.PartiesWrite,
            PermissionCodes.PropertiesRead, PermissionCodes.PropertiesWrite,
            PermissionCodes.ContractsRead
        ]),
        new(SystemRoleCodes.AuditViewer, "مراجع العمليات", "Auditeur",
        [
            PermissionCodes.UsersRead, PermissionCodes.RolesRead,
            PermissionCodes.AuditRead
        ])
    ];
}
