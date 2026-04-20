namespace ActiveDirLdapDotNet.Models;

/// <summary>Objet Active Directory de base.</summary>
public abstract class AdObject
{
    public string DistinguishedName { get; internal set; } = string.Empty;
    public Guid ObjectGuid { get; internal set; }
    public string CommonName { get; internal set; } = string.Empty;
    public DateTime? WhenCreated { get; internal set; }
    public DateTime? WhenChanged { get; internal set; }
}
