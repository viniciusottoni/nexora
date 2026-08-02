using Awaken.Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class PasswordResetConfiguration : IEntityTypeConfiguration<PasswordResetRequest>
{
    public void Configure(EntityTypeBuilder<PasswordResetRequest> builder)
    {
        builder.ToTable("password_reset_requests");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.UserId).IsRequired();
        builder.Property(r => r.TokenHash).IsRequired().HasMaxLength(128);
        builder.HasIndex(r => r.TokenHash).IsUnique();
        builder.Property(r => r.RequestedAtUtc).IsRequired();
        builder.Property(r => r.ExpiresAtUtc).IsRequired();
    }
}
