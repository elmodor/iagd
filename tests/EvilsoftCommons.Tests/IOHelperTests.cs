using EvilsoftCommons;
using Xunit;

namespace EvilsoftCommons.Tests;

public class IOHelperTests {
    [Fact]
    public void GetInt_ReadsLittleEndianInteger() {
        var buffer = new byte[16];
        BitConverter.GetBytes(123456).CopyTo(buffer, 0);
        var result = IOHelper.GetInt(buffer, 0);
        Assert.Equal(123456, result);
    }

    [Fact]
    public void GetShort_ReadsUnsignedShort() {
        var buffer = new byte[16];
        BitConverter.GetBytes((ushort)54321).CopyTo(buffer, 0);
        var result = IOHelper.GetShort(buffer, 0);
        Assert.Equal(54321, result);
    }

    [Fact]
    public void GetNullString_ReadsUntilNullTerminator() {
        var buffer = new byte[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o', 0, (byte)'X' };
        var result = IOHelper.GetNullString(buffer, 0);
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void ToTimestamp_ReturnsZeroForUnixEpoch() {
        var epoch = new DateTime(1970, 1, 1,0, 0, 0, DateTimeKind.Utc);
        var result = epoch.ToTimestamp();
        Assert.Equal(0, result);
    }

    [Fact]
    public void FromTimestamp_ReturnsUnixEpochForZero() {
        var result = DateTimeEpochExtension.FromTimestamp(0);
        Assert.Equal(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), result.ToUniversalTime());
    }

    [Fact]
    public void OpenSharedRead_OpensFileForReading() {
        var testFile = Path.Combine(Path.GetTempPath(), $"evilsoftcommons-test-{Guid.NewGuid()}.bin");
        try {
            File.WriteAllBytes(testFile, new byte[] { 1, 2, 3, 4 });
            using var stream = IOHelper.OpenSharedRead(testFile);
            Assert.Equal(4, stream.Length);
        }
        finally {
            if (File.Exists(testFile)) {
                File.Delete(testFile);
            }
        }
    }
}
