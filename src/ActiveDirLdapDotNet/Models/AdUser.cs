namespace ActiveDirLdapDotNet.Models;

/// <summary>Utilisateur Active Directory.</summary>
public sealed class AdUser : AdObject
{
    public string SamAccountName { get; internal set; } = string.Empty;
    public string? UserPrincipalName { get; internal set; }
    public string? DisplayName { get; internal set; }
    public string? GivenName { get; internal set; }
    public string? Surname { get; internal set; }
    public string? Email { get; internal set; }
    public string? TelephoneNumber { get; internal set; }
    public string? Department { get; internal set; }
    public string? Title { get; internal set; }

    /// <summary>Vrai si le compte est activé (userAccountControl bit 0x2 = désactivé).</summary>
    public bool IsEnabled { get; internal set; }

    /// <summary>Liste des DN des groupes dont l'utilisateur est membre direct.</summary>
    public IReadOnlyList<string> MemberOf { get; internal set; } = [];
}
