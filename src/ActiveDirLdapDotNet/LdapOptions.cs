using System.DirectoryServices.Protocols;

namespace ActiveDirLdapDotNet;

/// <summary>Options de connexion au serveur LDAP/Active Directory.</summary>
public sealed class LdapOptions
{
    /// <summary>Nom ou adresse IP du contrôleur de domaine.</summary>
    public required string Server { get; init; }

    /// <summary>Port LDAP (389 par défaut, 636 pour LDAPS).</summary>
    public int Port { get; init; } = 389;

    /// <summary>Base DN de recherche, ex: "DC=exemple,DC=com".</summary>
    public required string BaseDn { get; init; }

    /// <summary>Compte de service (UPN ou DN). Null pour une liaison anonyme.</summary>
    public string? Username { get; init; }

    /// <summary>Mot de passe du compte de service.</summary>
    public string? Password { get; init; }

    /// <summary>Activer SSL/LDAPS.</summary>
    public bool UseSsl { get; init; }

    /// <summary>
    /// Type d'authentification.
    /// Utiliser <see cref="AuthType.Negotiate"/> (Kerberos/NTLM) sur Windows domain,
    /// <see cref="AuthType.Basic"/> sur Linux avec SSL activé.
    /// </summary>
    public AuthType AuthType { get; init; } = AuthType.Negotiate;

    /// <summary>Délai d'attente pour chaque requête LDAP.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Nombre d'objets par page lors des recherches paginées.</summary>
    public int PageSize { get; init; } = 1000;
}
