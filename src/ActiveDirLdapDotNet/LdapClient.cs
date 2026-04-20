using System.DirectoryServices.Protocols;
using System.Net;
using ActiveDirLdapDotNet.Services;

namespace ActiveDirLdapDotNet;

/// <summary>
/// Point d'entrée principal pour interroger un Active Directory via LDAP.
/// </summary>
/// <example>
/// <code>
/// using var client = new LdapClient(new LdapOptions
/// {
///     Server  = "dc.exemple.com",
///     BaseDn  = "DC=exemple,DC=com",
///     Username = "svc-ldap@exemple.com",
///     Password = "••••••"
/// });
///
/// var user = await client.Users.GetByUsernameAsync("jdupont");
/// var pcs  = await client.Computers.SearchAsync("PC-*");
/// var members = await client.Groups.GetMembersAsync("Domain Admins");
/// </code>
/// </example>
public sealed class LdapClient : IDisposable
{
    private readonly LdapConnection _connection;
    private bool _disposed;

    /// <summary>Opérations sur les utilisateurs AD.</summary>
    public IUserService Users { get; }

    /// <summary>Opérations sur les ordinateurs AD.</summary>
    public IComputerService Computers { get; }

    /// <summary>Opérations sur les groupes AD.</summary>
    public IGroupService Groups { get; }

    /// <summary>
    /// Crée un client LDAP et établit la liaison (Bind) avec le serveur.
    /// </summary>
    /// <exception cref="LdapException">Échec d'authentification ou de connexion.</exception>
    public LdapClient(LdapOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _connection = CreateAndBind(options);
        Users = new UserService(_connection, options);
        Computers = new ComputerService(_connection, options);
        Groups = new GroupService(_connection, options);
    }

    private static LdapConnection CreateAndBind(LdapOptions options)
    {
        var identifier = new LdapDirectoryIdentifier(options.Server, options.Port);
        NetworkCredential? credential = options.Username is not null
            ? new NetworkCredential(options.Username, options.Password)
            : null;

        var connection = new LdapConnection(identifier, credential, options.AuthType);
        connection.SessionOptions.ProtocolVersion = 3;
        connection.SessionOptions.SecureSocketLayer = options.UseSsl;
        connection.Timeout = options.Timeout;
        connection.Bind();
        return connection;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _connection.Dispose();
            _disposed = true;
        }
    }
}
