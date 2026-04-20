using System.DirectoryServices.Protocols;
using ActiveDirLdapDotNet.Internal;
using ActiveDirLdapDotNet.Models;

namespace ActiveDirLdapDotNet.Services;

internal sealed class GroupService : IGroupService
{
    private static readonly string[] LdapAttributes =
    [
        "cn", "description", "mail", "member", "memberOf", "groupType",
        "distinguishedName", "objectGUID", "whenCreated", "whenChanged"
    ];

    private readonly LdapConnection _connection;
    private readonly LdapOptions _options;

    internal GroupService(LdapConnection connection, LdapOptions options)
    {
        _connection = connection;
        _options = options;
    }

    public Task<AdGroup?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var filter = $"(&(objectClass=group)(cn={LdapHelper.EscapeFilter(name)}))";
        return SearchFirstAsync(filter, cancellationToken);
    }

    public Task<IReadOnlyList<AdGroup>> GetAllAsync(CancellationToken cancellationToken = default)
        => ExecutePagedSearchAsync("(objectClass=group)", cancellationToken);

    public async Task<IReadOnlyList<AdUser>> GetMembersAsync(string groupName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);
        var group = await GetByNameAsync(groupName, cancellationToken);
        if (group is null) return [];

        return await Task.Run(
            () => (IReadOnlyList<AdUser>)SearchUsersInGroup(group.DistinguishedName, cancellationToken),
            cancellationToken);
    }

    public Task<IReadOnlyList<AdGroup>> GetGroupsForUserAsync(string username, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        return Task.Run(() => (IReadOnlyList<AdGroup>)GetGroupsForUser(username, cancellationToken), cancellationToken);
    }

    // ─── Internals ────────────────────────────────────────────────────────────

    private async Task<AdGroup?> SearchFirstAsync(string filter, CancellationToken ct)
    {
        var results = await ExecutePagedSearchAsync(filter, ct);
        return results.Count > 0 ? results[0] : null;
    }

    private Task<IReadOnlyList<AdGroup>> ExecutePagedSearchAsync(string filter, CancellationToken ct)
        => Task.Run(() => (IReadOnlyList<AdGroup>)ExecutePagedSearch(filter, ct), ct);

    private List<AdGroup> ExecutePagedSearch(string filter, CancellationToken ct)
    {
        var results = new List<AdGroup>();
        var pageControl = new PageResultRequestControl(_options.PageSize);
        var request = new SearchRequest(_options.BaseDn, filter, SearchScope.Subtree, LdapAttributes);
        request.Controls.Add(pageControl);

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var response = (SearchResponse)_connection.SendRequest(request);

            foreach (SearchResultEntry entry in response.Entries)
                results.Add(MapGroup(entry));

            var cookie = LdapHelper.ExtractPageCookie(response);
            if (cookie == null) break;
            pageControl.Cookie = cookie;
        }

        return results;
    }

    /// <summary>
    /// Recherche les utilisateurs membres d'un groupe via le filtre memberOf.
    /// Plus efficace que de résoudre chaque DN de l'attribut member.
    /// </summary>
    private List<AdUser> SearchUsersInGroup(string groupDn, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var escaped = LdapHelper.EscapeFilter(groupDn);
        var filter = $"(&(objectCategory=person)(objectClass=user)(memberOf={escaped}))";

        var results = new List<AdUser>();
        var pageControl = new PageResultRequestControl(_options.PageSize);
        var request = new SearchRequest(_options.BaseDn, filter, SearchScope.Subtree, UserService.LdapAttributes);
        request.Controls.Add(pageControl);

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var response = (SearchResponse)_connection.SendRequest(request);

            foreach (SearchResultEntry entry in response.Entries)
                results.Add(UserService.MapUser(entry));

            var cookie = LdapHelper.ExtractPageCookie(response);
            if (cookie == null) break;
            pageControl.Cookie = cookie;
        }

        return results;
    }

    private List<AdGroup> GetGroupsForUser(string username, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Résoudre le DN de l'utilisateur
        var escaped = LdapHelper.EscapeFilter(username);
        var userRequest = new SearchRequest(
            _options.BaseDn,
            $"(&(objectCategory=person)(objectClass=user)(sAMAccountName={escaped}))",
            SearchScope.Subtree,
            new[] { "distinguishedName" });

        var userResponse = (SearchResponse)_connection.SendRequest(userRequest);
        if (userResponse.Entries.Count == 0) return [];

        var userDn = userResponse.Entries[0].DistinguishedName;
        if (string.IsNullOrEmpty(userDn)) return [];

        // Chercher les groupes dont le membre est cet utilisateur
        var filter = $"(&(objectClass=group)(member={LdapHelper.EscapeFilter(userDn)}))";
        return ExecutePagedSearch(filter, ct);
    }

    private static AdGroup MapGroup(SearchResultEntry entry)
    {
        var groupTypeValue = int.TryParse(LdapHelper.GetString(entry, "groupType"), out var v) ? v : 0;
        return new AdGroup
        {
            DistinguishedName = entry.DistinguishedName ?? string.Empty,
            ObjectGuid = LdapHelper.GetGuid(entry, "objectGUID"),
            CommonName = LdapHelper.GetString(entry, "cn") ?? string.Empty,
            Description = LdapHelper.GetString(entry, "description"),
            Email = LdapHelper.GetString(entry, "mail"),
            Members = LdapHelper.GetStringList(entry, "member"),
            MemberOf = LdapHelper.GetStringList(entry, "memberOf"),
            WhenCreated = LdapHelper.GetGeneralizedTime(entry, "whenCreated"),
            WhenChanged = LdapHelper.GetGeneralizedTime(entry, "whenChanged"),
            Scope = ParseScope(groupTypeValue),
            IsSecurity = (groupTypeValue & unchecked((int)0x80000000)) != 0
        };
    }

    private static GroupScope ParseScope(int groupType) => (groupType & 0xF) switch
    {
        0x4 => GroupScope.DomainLocal,
        0x2 => GroupScope.Global,
        0x8 => GroupScope.Universal,
        _ => GroupScope.Unknown
    };
}
