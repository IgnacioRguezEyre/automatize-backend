using Empleos.Domain.JobOffers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Empleos.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeo de JobOffer. El dominio no sabe nada de EF: todo lo de la base de datos se decide aquí.
/// EF usa el constructor privado de JobOffer y rellena el resto por sus setters privados.
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

        builder.Property(o => o.ExternalId).HasMaxLength(JobOffer.MaxExternalIdLength);
        // Único solo entre las que lo tienen: las ofertas creadas a mano no llevan ExternalId.
        builder.HasIndex(o => o.ExternalId).IsUnique().HasFilter("[ExternalId] IS NOT NULL");

        builder.Property(o => o.Location).HasMaxLength(JobOfferDetails.MaxLocationLength);
        builder.Property(o => o.WorkMode).HasMaxLength(JobOfferDetails.MaxWorkModeLength);
        builder.Property(o => o.Salary).HasMaxLength(JobOfferDetails.MaxSalaryLength);
        builder.Property(o => o.Url).HasMaxLength(JobOfferDetails.MaxUrlLength);
        builder.Property(o => o.Description).HasMaxLength(JobOfferDetails.MaxDescriptionLength);

        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(o => o.StatusDate);

        builder.Property(o => o.Notes).HasMaxLength(JobOffer.MaxNotesLength).IsRequired();
        builder.Property(o => o.IsClosed).IsRequired();

        // Las novedades van en su propia tabla, pero solo se cargan y se guardan a través de la oferta.
        // EF accede a la lista privada _events porque la propiedad pública es de solo lectura.
        builder.OwnsMany(o => o.Events, events =>
        {
            events.ToTable("ApplicationEvents");
            events.WithOwner().HasForeignKey("JobOfferId");
            events.Property(e => e.Date).IsRequired();
            events.Property(e => e.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
            events.Property(e => e.Text).HasMaxLength(ApplicationEvent.MaxTextLength).IsRequired();
        });
        builder.Navigation(o => o.Events).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.ArrivalDate);
    }
}
