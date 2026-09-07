using LoanAmortization.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoanAmortization.Infrastructure.Data.Configurations;

public class SimulationConfiguration : IEntityTypeConfiguration<Simulation>
{
    public void Configure(EntityTypeBuilder<Simulation> builder)
    {
        builder.ToTable("simulations");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");

        builder.Property(s => s.UserId).HasColumnName("user_id").IsRequired();

        builder.Property(s => s.Currency)
            .HasColumnName("currency")
            .HasColumnType("char(3)")
            .IsRequired();

        builder.Property(s => s.Principal)
            .HasColumnName("principal")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(s => s.AnnualRate)
            .HasColumnName("annual_rate")
            .HasColumnType("numeric(7,4)")
            .IsRequired();

        builder.Property(s => s.TermMonths)
            .HasColumnName("term_months")
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
