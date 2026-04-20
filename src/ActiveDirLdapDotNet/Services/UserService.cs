using System.DirectoryServices.Protocols;
using ActiveDirLdapDotNet.Internal;
using ActiveDirLdapDotNet.Models;

namespace ActiveDirLdapDotNet.Services;

internal sealed class UserService : IUserService
{
    internal static readonly string[] LdapAttributes =
    [
        "sAMAccountName", "cn", "displayName", "givenName", "sn", "mail",
        "telephoneNumber", "department", "title", "userPrincipalName",
        "userAccountControl", "distinguishedName", "objectGUID",
        "whenCreated", "whenChanged", "memberOf"
    ];

    private readonly LdapConnection _connection;
    private readonly LdapOptions _options;

    internal UserService(LdapConnection connection, LdapOptions options)
    {
        _connection = connection;
        _options = options;
    }

    public Task<AdUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        var filter = $"(&(objectCategory=person)(objectClass=user)(sAMAccountName={LdapHelper.EscapeFilter(username)}))";
        return SearchFirstAsync(filter, cancellationToken);
    }

    public Task<AdUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        var filter = $"(&(objectCategory=person)(objectClass=user)(mail={LdapHelper.EscapeFilter(email)}))";
        return SearchFirstAsync(filter, cancellationToken);
    }

    public Task<AdUser?> GetByDistinguishedNameAsync(string distinguishedName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(distinguishedName);
        var filter = $"(&(objectCategory=person)(objectClass=user)(distinguishedName={LdapHelper.EscapeFilter(distinguishedName)}))";
        return SearchFirstAsync(filter, cancellationToken);
    }

    public Task<IReadOnlyList<AdUser>> GetAllAsync(CancellationToken cancellationToken = default)
        => ExecutePagedSearchAsync("(&(objectCategory=person)(objectClass=user))", cancellationToken);

    public Task<IReadOnlyList<AdUser>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchTerm);
        var escaped = LdapHelper.EscapeFilter(searchTerm);
        var filter = $"(&(objectCategory=person)(objectClass=user)(|(sAMAccountName={escaped})(cn={escaped})(mail={escaped})(displayName={escaped})))";
        return ExecutePagedSearchAsync(filter, cancellationToken);
    }

    public async Task<bool> IsInGroupAsync(string username, string groupName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);
        var user = await GetByUsernameAsync(username, cancellationToken);
        if (user is null) return false;
        return user.MemberOf.Any(dn => dn.StartsWith($"CN={groupName},", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<AdUser?> SearchFirstAsync(string filter, CancellationToken ct)
    {
        var results = await ExecutePagedSearchAsync(filter, ct);
        return results.Count > 0 ? results[0] : null;
    }

    private Task<IReadOnlyList<AdUser>> ExecutePagedSearchAsync(string filter, CancellationToken ct)
        => Task.Run(() => (IReadOnlyList<AdUser>)ExecutePagedSearch(filter, ct), ct);

    private List<AdUser> ExecutePagedSearch(string filter, CancellationToken ct)
    {
        var results = new List<AdUser>();
        var pageControl = new PageResultRequestControl(_options.PageSize);
        var request = new SearchRequest(_options.BaseDn, filter, SearchScope.Subtree, LdapAttributes);
        request.Controls.Add(pageControl);

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var response = (SearchResponse)_connection.SendRequest(request);

            foreach (SearchResultEntry entry in response.Entries)
                results.Add(MapUser(entry));

            var pageResponse = response.Controls.OfType<PageResultResponseControl>().FirstOrDefault();
            if (pageResponse is null || pageResponse.Cookie.Length == 0) break;
            pageControl.Cookie = pageResponse.Cookie;
        }

        return results;
    }

    internal static AdUser MapUser(SearchResultEntry entry)
    {
        var uac = int.TryParse(LdapHelper.GetString(entry, "userAccountControl"), out var v) ? v : 0;
        return new AdUser
        {
            DistinguishedName = entry.DistinguishedName ?? string.Empty,
            ObjectGuid = LdapHelper.GetGuid(entry, "objectGUID"),
            CommonName = LdapHelper.GetString(entry, "cn") ?? string.Empty,
            SamAccountName = LdapHelper.GetString(entry, "sAMAccountName") ?? string.Empty,
            UserPrincipalName = LdapHelper.GetString(entry, "userPrincipalName"),
            DisplayName = LdapHelper.GetString(entry, "displayName"),
            GivenName = LdapHelper.GetString(entry, "givenName"),
            Surname = LdapHelper.GetString(entry, "sn"),
            Email = LdapHelper.GetString(entry, "mail"),
            TelephoneNumber = LdapHelper.GetString(entry, "telephoneNumber"),
            Department = LdapHelper.GetString(entry, "department"),
            Title = LdapHelper.GetString(entry, "title"),
            IsEnabled = (uac & 0x2) == 0,
            WhenCreated = LdapHelper.GetGeneralizedTime(entry, "whenCreated"),
            WhenChanged = LdapHelper.GetGeneralizedTime(entry, "whenChanged"),
            MemberOf = LdapHelper.GetStringList(entry, "memberOf")
        };
    }
}
