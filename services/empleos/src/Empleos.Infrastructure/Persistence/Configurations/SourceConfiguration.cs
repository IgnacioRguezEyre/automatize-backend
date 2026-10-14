using Empleos.Domain.Sources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Empleos.Infrastructure.Persistence.Configurations;

internal sealed class SourceConfiguration : IEntityTypeConfiguration<Source>
{
    public void Configure(EntityTypeBuilder<Source> builder)
    {
        builder.ToTable("Sources");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Name).HasMaxLength(Source.MaxNameLength).IsRequired();
        builder.HasIndex(s => s.Name).IsUnique();

        builder.Property(s => s.Rule).HasMaxLength(Source.MaxRuleLength).IsRequired();
        builder.Property(s => s.IsActive).IsRequired();

        // Los remitentes se guardan en una sola columna separados por ';'. Son pocos y siempre se leen juntos,
        // así que no compensa una tabla aparte. El comparador le dice a EF cuándo ha cambiado la lista.
        builder.Property(s => s.Senders)
            .HasConversion(
                senders => string.Join(';', senders),
                value => value.Split(';', StringSplitOptions.RemoveEmptyEntries),
                new ValueComparer<IReadOnlyList<string>>(
                    (a, b) => a!.SequenceEqual(b!),
                    v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
                    v => v.ToArray()))
            .HasMaxLength(2000)
            .IsRequired();
    }
}
