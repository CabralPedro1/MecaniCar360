using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace MecaniCar360.Concurrency;

// Pure tests: no ConcurrencyEnvironment, connection, opt-in or claim.
public class SnapshotJsonTests
{
    public static IEnumerable<object?[]> InvalidScalars()
    {
        yield return new object?[] { null, "null", "n/a" };
        yield return new object?[] { DBNull.Value, "System.DBNull", "n/a" };
        foreach (var text in new[] { "", "   ", "[{secret", "{}", "null", "42", "[1]" })
            yield return new object?[] { text, "System.String", text.Length.ToString() };
        yield return new object?[] { 42, "System.Int32", "n/a" };
    }

    [Theory]
    [MemberData(nameof(InvalidScalars))]
    public void InvalidResultFailsWithoutLeakingContent(object? scalar, string type, string length)
    {
        var buffer = new StringBuilder("previous");
        var ex = Assert.Throws<InvalidOperationException>(() => SnapshotJson.Append(buffer, "[dbo].[Fixture]", scalar));
        Assert.Equal($"Snapshot JSON invalid: table=[dbo].[Fixture]; type={type}; length={length}. Expected an array of rows.", ex.Message);
        Assert.Null(ex.InnerException);
        Assert.Equal("previous", buffer.ToString());
    }

    [Theory]
    [InlineData("[]", "[dbo].[Fixture]")]
    [InlineData(" [ ] ", "[dbo].[Fixture]")]
    [InlineData("[{\"Id\":2},{\"Id\":1}]", "[dbo].[Fixture]{\"Id\":1}\n{\"Id\":2}")]
    [InlineData("[{\"Id\":1,\"Value\":null}]", "[dbo].[Fixture]{\"Id\":1,\"Value\":null}")]
    public void ValidRowsPreserveExistingCanonicalFormat(string json, string expected)
    {
        var buffer = new StringBuilder();
        SnapshotJson.Append(buffer, "[dbo].[Fixture]", json);
        Assert.Equal(expected, buffer.ToString());
        Assert.Equal(SHA256.HashData(Encoding.UTF8.GetBytes(expected)), SnapshotJson.Hash(buffer));
    }

    [Fact]
    public void HashIsOrderIndependentButDetectsChangesAndDuplicateRows()
    {
        byte[] Digest(string table, string json)
        {
            var text = new StringBuilder();
            SnapshotJson.Append(text, table, json);
            return SnapshotJson.Hash(text);
        }
        var baseline = Digest("[dbo].[Fixture]", "[{\"Id\":2},{\"Id\":1}]");
        Assert.Equal(baseline, Digest("[dbo].[Fixture]", "[{\"Id\":1},{\"Id\":2}]"));
        Assert.NotEqual(baseline, Digest("[dbo].[Fixture]", "[{\"Id\":1},{\"Id\":3}]"));
        Assert.NotEqual(baseline, Digest("[dbo].[Fixture]", "[{\"Id\":1},{\"Id\":2},{\"Id\":2}]"));
        Assert.NotEqual(baseline, Digest("[dbo].[Other]", "[{\"Id\":1},{\"Id\":2}]"));
    }

    [Fact]
    public void QueryOnlyUsesEmptyArrayWhenTableHasNoRows()
    {
        Assert.Equal("SELECT CASE WHEN EXISTS (SELECT 1 FROM [dbo].[Fixture]) THEN (SELECT * FROM [dbo].[Fixture] FOR JSON PATH, INCLUDE_NULL_VALUES) ELSE N'[]' END",
            SnapshotJson.Query("[dbo].[Fixture]"));
    }
}
