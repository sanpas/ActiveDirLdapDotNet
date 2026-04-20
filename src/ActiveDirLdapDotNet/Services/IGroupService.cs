using ActiveDirLdapDotNet.Models;

namespace ActiveDirLdapDotNet.Services;

/// <summary>Opérations LDAP sur les groupes AD.</summary>
public interface IGroupService
{
    /// <summary>Récupère un groupe par son CN.</summary>
    Task<AdGroup?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Retourne tous les groupes du domaine (paginé automatiquement).</summary>
    Task<IReadOnlyList<AdGroup>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Retourne les utilisateurs membres directs d'un groupe (par CN du groupe).</summary>
    Task<IReadOnlyList<AdUser>> GetMembersAsync(string groupName, CancellationToken cancellationToken = default);

    /// <summary>Retourne les groupes dont un utilisateur est membre direct.</summary>
    Task<IReadOnlyList<AdGroup>> GetGroupsForUserAsync(string username, CancellationToken cancellationToken = default);
}
