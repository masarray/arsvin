using AR.Iec61850.Comtrade;
using Xunit;

namespace ARSVIN.Tests.Comtrade;

public sealed class ComtradeReaderTests
{
    [Fact]
    public void LoadAsciiParsesConfigurationScalesChannelsAndPreservesTiming()
    {
        using var fixture = ComtradeFixture.Create(
            dataFileType: "ASCII",
            dataLines:
            [
                "1,0,10,20",
                "2,1000,11,22",
                "3,2000,12,24"
            ]);

        var dataset = new ComtradeReader().Load(fixture.ConfigurationPath);

        Assert.Equal("ARSVIN-LAB", dataset.Configuration.StationName);
        Assert.Equal("MU01", dataset.Configuration.DeviceId);
        Assert.Equal(2, dataset.Configuration.AnalogChannelCount);
        Assert.Equal(3, dataset.SampleCount);
        Assert.Equal(0.002, dataset.DurationSeconds, precision: 9);
        Assert.Equal(1000, dataset.NominalSampleRateHz);
        Assert.Equal(new double[] { 21, 9 }, dataset.Samples[0].AnalogValues);
        Assert.Equal(new double[] { 23, 10 }, dataset.Samples[1].AnalogValues);
        Assert.Equal(0.001, dataset.Samples[1].TimestampSeconds, precision: 9);
        Assert.Contains("A=2", dataset.Summary);
    }

    [Fact]
    public void NonIncreasingAsciiTimestampFallsBackToConfiguredSampleRate()
    {
        using var fixture = ComtradeFixture.Create(
            dataFileType: "ASCII",
            dataLines:
            [
                "1,0,1,2",
                "2,0,3,4"
            ]);

        var dataset = new ComtradeReader().Load(fixture.ConfigurationPath);

        Assert.Equal(0, dataset.Samples[0].TimestampSeconds);
        Assert.Equal(0.001, dataset.Samples[1].TimestampSeconds, precision: 9);
    }

    [Fact]
    public void DatasetIndexingSupportsLoopingAndClamping()
    {
        using var fixture = ComtradeFixture.Create(
            dataFileType: "ASCII",
            dataLines:
            [
                "1,0,1,2",
                "2,1000,3,4",
                "3,2000,5,6"
            ]);

        var dataset = new ComtradeReader().Load(fixture.ConfigurationPath);

        Assert.Equal(1, dataset.GetSampleByIndex(3, loop: true).Number);
        Assert.Equal(3, dataset.GetSampleByIndex(99, loop: false).Number);
        Assert.Equal(3, dataset.GetSampleByIndex(-1, loop: true).Number);
    }

    [Fact]
    public void UnsupportedDataTypeFailsExplicitly()
    {
        using var fixture = ComtradeFixture.Create(
            dataFileType: "BINARY64",
            dataLines: ["1,0,1,2"]);

        var ex = Assert.Throws<NotSupportedException>(() => new ComtradeReader().Load(fixture.ConfigurationPath));

        Assert.Contains("BINARY64", ex.Message);
        Assert.Contains("Supported DAT types", ex.Message);
    }

    private sealed class ComtradeFixture : IDisposable
    {
        private ComtradeFixture(string directory, string configurationPath)
        {
            Directory = directory;
            ConfigurationPath = configurationPath;
        }

        public string Directory { get; }
        public string ConfigurationPath { get; }

        public static ComtradeFixture Create(string dataFileType, IReadOnlyList<string> dataLines)
        {
            var directory = Path.Combine(Path.GetTempPath(), $"arsvin-comtrade-{Guid.NewGuid():N}");
            System.IO.Directory.CreateDirectory(directory);
            var configurationPath = Path.Combine(directory, "sample.cfg");
            var dataPath = Path.Combine(directory, "sample.dat");

            File.WriteAllLines(configurationPath,
            [
                "ARSVIN-LAB,MU01,1999",
                "2,2A,0D",
                "1,VA,A,,V,2,1,0,-32768,32767,100,1,P",
                "2,IA,A,,A,0.5,-1,0,-32768,32767,100,1,P",
                "50",
                "1",
                "1000,3",
                "01/01/2026,00:00:00.000000",
                "01/01/2026,00:00:00.000000",
                dataFileType,
                "1"
            ]);
            File.WriteAllLines(dataPath, dataLines);

            return new ComtradeFixture(directory, configurationPath);
        }

        public void Dispose()
        {
            if (System.IO.Directory.Exists(Directory))
                System.IO.Directory.Delete(Directory, recursive: true);
        }
    }
}
