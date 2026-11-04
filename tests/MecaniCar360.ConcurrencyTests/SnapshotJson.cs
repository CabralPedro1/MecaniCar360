using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MecaniCar360.Concurrency;

internal static class SnapshotJson
{
    // Only an explicitly empty table becomes []; unexpected scalar results fail closed.
    public static string Query(string quotedTable) =>
        $"SELECT CASE WHEN EXISTS (SELECT 1 FROM {quotedTable}) THEN (SELECT * FROM {quotedTable} FOR JSON PATH, INCLUDE_NULL_VALUES) ELSE N'[]' END";

    public static void Append(StringBuilder text, string table, object? scalar)
    {
        var type = scalar?.GetType().FullName ?? "null";
        var length = scalar is string value ? value.Length.ToString() : "n/a";
        InvalidOperationException Invalid() => new(
            $"Snapshot JSON invalid: table={table}; type={type}; length={length}. Expected an array of rows.");
        if (scalar is not string json || string.IsNullOrWhiteSpace(json)) throw Invalid();
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array ||
                document.RootElement.EnumerateArray().Any(row => row.ValueKind != JsonValueKind.Object))
                throw Invalid();
            var rows = document.RootElement.EnumerateArray()
                .Select(row => row.GetRawText()).OrderBy(row => row, StringComparer.Ordinal);
            text.Append(table).AppendJoin('\n', rows);
        }
        catch (JsonException)
        {
            // Do not retain the parser exception: its message can contain row content.
            throw Invalid();
        }
    }

    public static byte[] Hash(StringBuilder text) => SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString()));
}
