using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotaireDesk.Domain.Security;

namespace NotaireDesk.Infrastructure.Persistence.Configurations;

public sealed class OfficeUserRoleConfiguration : IEntityTypeConfiguration<OfficeUserRole>
{
    public void Configure(EntityTypeBuilder<OfficeUserRole> builder)
    {
        builder.ToTable("office_user_roles");
        builder.HasKey(x => new { x.UserId, x.RoleId });
        builder.HasOne<OfficeRole>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
    }
}
