using ActiveDirLdapDotNet.Models;

namespace ActiveDirLdapDotNet.Services;

/// <summary>Opérations LDAP sur les utilisateurs AD.</summary>
public interface IUserService
{
    /// <summary>Récupère un utilisateur par son sAMAccountName.</summary>
    Task<AdUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>Récupère un utilisateur par son adresse e-mail.</summary>
    Task<AdUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Récupère un utilisateur par son Distinguished Name.</summary>
    Task<AdUser?> GetByDistinguishedNameAsync(string distinguishedName, CancellationToken cancellationToken = default);

    /// <summary>Retourne tous les utilisateurs du domaine (paginé automatiquement).</summary>
    Task<IReadOnlyList<AdUser>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Recherche des utilisateurs par terme (sAMAccountName, cn, mail ou displayName).
    /// Accepte le wildcard * (ex: "jean*").
    /// </summary>
    Task<IReadOnlyList<AdUser>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>Vérifie si un utilisateur est membre direct d'un groupe (par CN du groupe).</summary>
    Task<bool> IsInGroupAsync(string username, string groupName, CancellationToken cancellationToken = default);
}
