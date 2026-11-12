using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotaireDesk.Domain.Security;

namespace NotaireDesk.Infrastructure.Persistence.Configurations;

public sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("audit_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ActorUserId);
        builder.Property(x => x.ActorUsername).HasMaxLength(80);
        builder.Property(x => x.Action).HasMaxLength(100).IsRequired();
        builder.Property(x => x.TargetType).HasMaxLength(80).IsRequired();
        builder.Property(x => x.TargetId).HasMaxLength(120);
        builder.Property(x => x.Outcome).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.OccurredAt).IsRequired();
        builder.Property(x => x.ClientAddress).HasMaxLength(64);
        builder.Property(x => x.CorrelationId).HasMaxLength(100);
        builder.Property(x => x.SafeMetadata).HasMaxLength(2000);
        builder.HasIndex(x => x.OccurredAt);
        builder.HasIndex(x => new { x.ActorUserId, x.OccurredAt });
        builder.HasIndex(x => new { x.TargetType, x.TargetId, x.OccurredAt });
    }
}
