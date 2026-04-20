namespace ActiveDirLdapDotNet.Models;

/// <summary>Groupe Active Directory.</summary>
public sealed class AdGroup : AdObject
{
    public string? Description { get; internal set; }
    public string? Email { get; internal set; }

    /// <summary>DNs des membres directs du groupe.</summary>
    public IReadOnlyList<string> Members { get; internal set; } = [];

    /// <summary>DNs des groupes parents.</summary>
    public IReadOnlyList<string> MemberOf { get; internal set; } = [];

    public GroupScope Scope { get; internal set; }

    /// <summary>Vrai = groupe de sécurité ; Faux = groupe de distribution.</summary>
    public bool IsSecurity { get; internal set; }
}

/// <summary>Étendue (portée) d'un groupe AD.</summary>
public enum GroupScope
{
    Unknown = 0,
    DomainLocal = 1,
    Global = 2,
    Universal = 3
}
