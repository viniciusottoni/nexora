using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Platform;

internal sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        // "status = 3" em vez de "status = 'INVITED'" pelo mesmo motivo documentado abaixo em
        // uq_app_user_pin: UserStatus ainda é gravado como integer simples (não enum nativo do
        // Postgres). UserStatus.Invited == 3 (Nexora.Domain.Platform.Enums.UserStatus) — dono
        // recém-provisionado por AppUser.Invite (US-002, Docs/Domain/12 §8) ainda não tem
        // password_hash/pin_hash; só ganha um dos dois ao aceitar o convite (AppUser.SetPassword,
        // que também transiciona o status para Active).
        builder.ToTable("app_user", b => b.HasCheckConstraint(
            "ck_app_user_credential",
            "password_hash IS NOT NULL OR pin_hash IS NOT NULL OR status = 3"));

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(u => u.TenantId).HasColumnName("tenant_id");
        builder.Property(u => u.Name).HasColumnName("name").IsRequired();
        builder.Property(u => u.Email).HasColumnName("email").HasColumnType("citext");
        builder.Property(u => u.PasswordHash).HasColumnName("password_hash"); // Argon2id — gestor/administrativo
        builder.Property(u => u.PinHash).HasColumnName("pin_hash"); // Argon2id — operação (ADR-014)
        builder.Property(u => u.MfaSecret).HasColumnName("mfa_secret_encrypted");
        builder.Property(u => u.PinLookup).HasColumnName("pin_lookup");
        builder.Property(u => u.PinRotatedAt).HasColumnName("pin_rotated_at").HasColumnType("timestamptz");
        builder.Property(u => u.Status).HasColumnName("status").HasDefaultValue(UserStatus.Active);
        builder.Property(u => u.FailedAttempts).HasColumnName("failed_attempts").HasColumnType("smallint").HasDefaultValue((short)0);
        builder.Property(u => u.BlockedUntil).HasColumnName("blocked_until").HasColumnType("timestamptz");
        builder.Property(u => u.LastLoginAt).HasColumnName("last_login_at").HasColumnType("timestamptz");
        builder.Property(u => u.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(u => u.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");

        builder.HasIndex(u => u.TenantId).HasDatabaseName("idx_app_user_tenant");

        builder.HasIndex(u => new { u.TenantId, u.Email })
            .IsUnique()
            .HasDatabaseName("uq_app_user_email")
            .HasFilter("email IS NOT NULL AND deleted_at IS NULL");

        // PIN não pode repetir entre usuários ativos do mesmo tenant (ADR-014).
        // NOTA: "status = 0" em vez de "status = 'ACTIVE'" porque nenhuma propriedade de enum
        // desta solution está mapeada como enum nativo do Postgres ainda (Docs/Domain/13
        // descreve o alvo via Npgsql.MapEnum, pendente de implementação em todas as entidades —
        // fora do escopo de US-001); hoje o EF Core grava UserStatus como integer (convenção
        // padrão), e o filtro de índice parcial precisa bater com o valor real da coluna.
        // UserStatus.Active == 0 (Nexora.Domain.Platform.Enums.UserStatus).
        //
        // CORREÇÃO (US-004, gap "índice de unicidade de PIN aponta pro campo errado"): a coluna
        // indexada era pin_hash — mas pin_hash é Argon2id com salt aleatório (ADR-014), então dois
        // PINs IGUAIS produzem hashes DIFERENTES e o índice nunca detecta colisão nenhuma (é
        // unicidade sobre um valor que já nasce único por construção, mesmo quando o PIN em texto
        // claro se repete). A coluna certa é pin_lookup — digest HMAC determinístico
        // (IPinLookupDigester) do mesmo PIN, já usado por LoginWithPinCommandHandler/
        // AuthorizeSensitiveActionCommandHandler para ENCONTRAR o usuário pelo PIN informado; dois
        // usuários com o mesmo PIN produzem o mesmo pin_lookup, e esta é a coluna que precisa ser
        // única. Ver Nexora.Migrations.FixPinUniquenessIndex.
        builder.HasIndex(u => new { u.TenantId, u.PinLookup })
            .IsUnique()
            .HasDatabaseName("uq_app_user_pin")
            .HasFilter("pin_lookup IS NOT NULL AND status = 0 AND deleted_at IS NULL");

        builder.HasOne(u => u.Tenant).WithMany(t => t.Users).HasForeignKey(u => u.TenantId);
    }
}
