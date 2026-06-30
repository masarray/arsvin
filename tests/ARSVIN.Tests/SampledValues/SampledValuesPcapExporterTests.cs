using AR.Iec61850.SampledValues;
using Xunit;

namespace ARSVIN.Tests.SampledValues;

public sealed class SampledValuesPcapExporterTests
{
    [Fact]
    public void WritesPcapHeaderAndPacket()
    {
        var path = Path.Combine(Path.GetTempPath(), $"arsvin-sv-{Guid.NewGuid():N}.pcap");
        try
        {
            SampledValuesPcapExporter.WriteGeneratedFrames(path, new[]
            {
                (DateTimeOffset.UnixEpoch, new byte[] { 1, 2, 3, 4 })
            });

            var bytes = File.ReadAllBytes(path);
            Assert.True(bytes.Length >= 24 + 16 + 4);
            Assert.Equal(new byte[] { 0xD4, 0xC3, 0xB2, 0xA1 }, bytes.Take(4).ToArray());
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
