namespace ActiveDirLdapDotNet.Models;

/// <summary>Ordinateur Active Directory.</summary>
public sealed class AdComputer : AdObject
{
    public string? DnsHostName { get; internal set; }
    public string? OperatingSystem { get; internal set; }
    public string? OperatingSystemVersion { get; internal set; }
    public string? OperatingSystemServicePack { get; internal set; }

    /// <summary>Vrai si le compte ordinateur est activé.</summary>
    public bool IsEnabled { get; internal set; }

    /// <summary>Dernière connexion au domaine (attribut lastLogon, résolution locale DC).</summary>
    public DateTime? LastLogon { get; internal set; }
}
