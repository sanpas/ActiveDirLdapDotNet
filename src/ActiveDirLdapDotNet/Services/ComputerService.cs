using System.DirectoryServices.Protocols;
using ActiveDirLdapDotNet.Internal;
using ActiveDirLdapDotNet.Models;

namespace ActiveDirLdapDotNet.Services;

internal sealed class ComputerService : IComputerService
{
    private static readonly string[] LdapAttributes =
    [
        "cn", "dNSHostName", "operatingSystem", "operatingSystemVersion",
        "operatingSystemServicePack", "userAccountControl", "distinguishedName",
        "objectGUID", "whenCreated", "whenChanged", "lastLogon"
    ];

    private readonly LdapConnection _connection;
    private readonly LdapOptions _options;

    internal ComputerService(LdapConnection connection, LdapOptions options)
    {
        _connection = connection;
        _options = options;
    }

    public Task<AdComputer?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var filter = $"(&(objectClass=computer)(cn={LdapHelper.EscapeFilter(name)}))";
        return SearchFirstAsync(filter, cancellationToken);
    }

    public Task<AdComputer?> GetByDnsHostNameAsync(string dnsHostName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dnsHostName);
        var filter = $"(&(objectClass=computer)(dNSHostName={LdapHelper.EscapeFilter(dnsHostName)}))";
        return SearchFirstAsync(filter, cancellationToken);
    }

    public Task<IReadOnlyList<AdComputer>> GetAllAsync(CancellationToken cancellationToken = default)
        => ExecutePagedSearchAsync("(objectClass=computer)", cancellationToken);

    public Task<IReadOnlyList<AdComputer>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchTerm);
        var escaped = LdapHelper.EscapeFilter(searchTerm);
        var filter = $"(&(objectClass=computer)(|(cn={escaped})(dNSHostName={escaped})))";
        return ExecutePagedSearchAsync(filter, cancellationToken);
    }

    private async Task<AdComputer?> SearchFirstAsync(string filter, CancellationToken ct)
    {
        var results = await ExecutePagedSearchAsync(filter, ct);
        return results.Count > 0 ? results[0] : null;
    }

    private Task<IReadOnlyList<AdComputer>> ExecutePagedSearchAsync(string filter, CancellationToken ct)
        => Task.Run(() => (IReadOnlyList<AdComputer>)ExecutePagedSearch(filter, ct), ct);

    private List<AdComputer> ExecutePagedSearch(string filter, CancellationToken ct)
    {
        var results = new List<AdComputer>();
        var pageControl = new PageResultRequestControl(_options.PageSize);
        var request = new SearchRequest(_options.BaseDn, filter, SearchScope.Subtree, LdapAttributes);
        request.Controls.Add(pageControl);

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var response = (SearchResponse)_connection.SendRequest(request);

            foreach (SearchResultEntry entry in response.Entries)
                results.Add(MapComputer(entry));

            var pageResponse = response.Controls.OfType<PageResultResponseControl>().FirstOrDefault();
            if (pageResponse is null || pageResponse.Cookie.Length == 0) break;
            pageControl.Cookie = pageResponse.Cookie;
        }

        return results;
    }

    private static AdComputer MapComputer(SearchResultEntry entry)
    {
        var uac = int.TryParse(LdapHelper.GetString(entry, "userAccountControl"), out var v) ? v : 0;
        return new AdComputer
        {
            DistinguishedName = entry.DistinguishedName ?? string.Empty,
            ObjectGuid = LdapHelper.GetGuid(entry, "objectGUID"),
            CommonName = LdapHelper.GetString(entry, "cn") ?? string.Empty,
            DnsHostName = LdapHelper.GetString(entry, "dNSHostName"),
            OperatingSystem = LdapHelper.GetString(entry, "operatingSystem"),
            OperatingSystemVersion = LdapHelper.GetString(entry, "operatingSystemVersion"),
            OperatingSystemServicePack = LdapHelper.GetString(entry, "operatingSystemServicePack"),
            IsEnabled = (uac & 0x2) == 0,
            WhenCreated = LdapHelper.GetGeneralizedTime(entry, "whenCreated"),
            WhenChanged = LdapHelper.GetGeneralizedTime(entry, "whenChanged"),
            LastLogon = LdapHelper.GetFileTime(entry, "lastLogon")
        };
    }
}
