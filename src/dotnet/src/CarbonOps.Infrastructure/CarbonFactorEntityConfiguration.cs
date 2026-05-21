using CarbonOps.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarbonOps.Infrastructure;

internal sealed class CarbonFactorEntityConfiguration : IEntityTypeConfiguration<CarbonFactor>
{
    public void Configure(EntityTypeBuilder<CarbonFactor> builder)
    {
        builder.ToTable("carbon_factors", "carbonops");

        builder.HasKey(factor => factor.Id);
        builder.Property(factor => factor.Id).HasColumnName("factor_id");
        builder.Property(factor => factor.Source).HasColumnName("source_provider");
        builder.Property(factor => factor.Category).HasColumnName("category");
        builder.Property(factor => factor.Activity).HasColumnName("activity");
        builder.Property(factor => factor.FactorValue).HasColumnName("factor_value").HasPrecision(18, 8);
        builder.Property(factor => factor.FactorUnit).HasColumnName("factor_unit");
        builder.Property(factor => factor.Region).HasColumnName("region");
        builder.Property(factor => factor.Year).HasColumnName("factor_year");
        builder.Property(factor => factor.Notes).HasColumnName("notes");

        builder.Ignore("EqualityContract");
    }
}
