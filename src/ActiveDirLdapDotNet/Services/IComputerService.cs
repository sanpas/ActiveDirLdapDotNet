using ActiveDirLdapDotNet.Models;

namespace ActiveDirLdapDotNet.Services;

/// <summary>Opérations LDAP sur les ordinateurs AD.</summary>
public interface IComputerService
{
    /// <summary>Récupère un ordinateur par son nom NetBIOS (CN).</summary>
    Task<AdComputer?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Récupère un ordinateur par son nom DNS complet.</summary>
    Task<AdComputer?> GetByDnsHostNameAsync(string dnsHostName, CancellationToken cancellationToken = default);

    /// <summary>Retourne tous les ordinateurs du domaine (paginé automatiquement).</summary>
    Task<IReadOnlyList<AdComputer>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Recherche des ordinateurs par terme (CN ou dNSHostName).
    /// Accepte le wildcard * (ex: "SRV-*").
    /// </summary>
    Task<IReadOnlyList<AdComputer>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
}
