using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using rps_backend.Features.Auth.Domain;

namespace rps_backend.Persistence.Configurations;

public class TokenBlacklistConfiguration : IEntityTypeConfiguration<TokenBlacklist>
{
    public void Configure(EntityTypeBuilder<TokenBlacklist> builder)
    {
        builder.ToTable("token_blacklist");

        builder.HasKey(t => t.Jti);

        builder.Property(t => t.Jti)
            .HasMaxLength(50);
    }
}
