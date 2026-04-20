using System.DirectoryServices.Protocols;
using System.Globalization;
using System.Text;

namespace ActiveDirLdapDotNet.Internal;

internal static class LdapHelper
{
    internal static string? GetString(SearchResultEntry entry, string attribute)
    {
        if (!entry.Attributes.Contains(attribute)) return null;
        var attr = entry.Attributes[attribute];
        return attr.Count > 0 ? attr[0]?.ToString() : null;
    }

    internal static IReadOnlyList<string> GetStringList(SearchResultEntry entry, string attribute)
    {
        if (!entry.Attributes.Contains(attribute)) return [];
        var attr = entry.Attributes[attribute];
        var list = new List<string>(attr.Count);
        for (int i = 0; i < attr.Count; i++)
        {
            var value = attr[i]?.ToString();
            if (!string.IsNullOrEmpty(value))
                list.Add(value);
        }
        return list;
    }

    internal static Guid GetGuid(SearchResultEntry entry, string attribute)
    {
        if (!entry.Attributes.Contains(attribute)) return Guid.Empty;
        var attr = entry.Attributes[attribute];
        if (attr.Count == 0) return Guid.Empty;
        return attr[0] is byte[] bytes && bytes.Length == 16 ? new Guid(bytes) : Guid.Empty;
    }

    /// <summary>Convertit un attribut au format Generalized Time AD (ex: "20240115083000.0Z").</summary>
    internal static DateTime? GetGeneralizedTime(SearchResultEntry entry, string attribute)
    {
        var value = GetString(entry, attribute);
        if (value is null) return null;

        string[] formats = ["yyyyMMddHHmmss.fZ", "yyyyMMddHHmmss.f+0000", "yyyyMMddHHmmssZ", "yyyyMMddHHmmss.0Z"];
        if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt))
            return dt;

        return null;
    }

    /// <summary>Convertit un attribut Windows File Time (Int64) en DateTime UTC.</summary>
    internal static DateTime? GetFileTime(SearchResultEntry entry, string attribute)
    {
        var value = GetString(entry, attribute);
        if (value is null || !long.TryParse(value, out var ticks)) return null;
        // 0 et Int64.MaxValue signifient "jamais connecté"
        if (ticks <= 0 || ticks == long.MaxValue) return null;
        return DateTime.FromFileTimeUtc(ticks);
    }

    /// <summary>Échappe les caractères spéciaux dans une valeur de filtre LDAP (RFC 4515).</summary>
    internal static string EscapeFilter(string value)
    {
        var sb = new StringBuilder(value.Length + 4);
        foreach (char c in value)
        {
            sb.Append(c switch
            {
                '\\' => "\\5c",
                '*' => "\\2a",
                '(' => "\\28",
                ')' => "\\29",
                '\0' => "\\00",
                _ => c
            });
        }
        return sb.ToString();
    }
}
