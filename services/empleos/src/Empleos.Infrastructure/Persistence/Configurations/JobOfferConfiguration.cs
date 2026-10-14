using Empleos.Domain.JobOffers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Empleos.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeo de JobOffer. El dominio no sabe nada de EF: todo lo de la base de datos se decide aquí.
/// EF usa el constructor privado de JobOffer y rellena Status y StatusDate por sus setters privados.
/// </summary>
internal sealed class JobOfferConfiguration : IEntityTypeConfiguration<JobOffer>
{
    public void Configure(EntityTypeBuilder<JobOffer> builder)
    {
        builder.ToTable("JobOffers");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.Title).HasMaxLength(300).IsRequired();
        builder.Property(o => o.Company).HasMaxLength(200).IsRequired();
        builder.Property(o => o.Portal).HasMaxLength(100).IsRequired();
        builder.Property(o => o.ArrivalDate).IsRequired();

        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(o => o.StatusDate);

        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.ArrivalDate);
    }
}
