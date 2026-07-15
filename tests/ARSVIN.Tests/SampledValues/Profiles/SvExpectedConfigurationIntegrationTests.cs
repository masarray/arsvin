using AR.Iec61850.Ethernet;
using AR.Iec61850.SampledValues;
using AR.Iec61850.SampledValues.Profiles;
using AR.Iec61850.Scl;
using Xunit;

namespace ARSVIN.Tests.SampledValues.Profiles;

public sealed class SvExpectedConfigurationIntegrationTests
{
    [Fact]
    public void FactoryConvertsSclProfileIntoExpectedConfiguration()
    {
        var expected = SvExpectedStreamConfigurationFactory.Create(CreateProfile());

        Assert.Equal((ushort)0x88BA, expected.EtherType);
        Assert.Equal((ushort)0x4001, expected.AppId);
        Assert.Equal("01:0C:CD:04:00:01", expected.DestinationMac);
        Assert.Equal((ushort)100, expected.VlanId);
        Assert.Equal((byte)4, expected.VlanPriority);
        Assert.Equal("MU01SV01", expected.SvId);
        Assert.Equal("MU01MUnn/LLN0$PhsMeas", expected.DataSetReference);
        Assert.Equal((uint)7, expected.ConfigurationRevision);
        Assert.Equal(1, expected.AsduPerFrame);
        Assert.Equal(8, expected.PayloadBytesPerAsdu);
        Assert.Equal((ushort)80, expected.DeclaredSampleRate);
        Assert.Equal((ushort)0, expected.DeclaredSampleMode);
        Assert.Equal(2, expected.DataSetSignature.Count);
    }

    [Fact]
    public void CompatibleComparisonIsDefaultAndReportsWarnings()
    {
        var manager = new SvStreamObservationManager();

        Assert.True(manager.TryObserve(
            DateTimeOffset.UnixEpoch,
            CreateFrame(configurationRevision: 8),
            SvObservationInputKind.LiveCapture,
            CreateProfile(),
            out var snapshot));

        var comparison = Assert.IsType<SvConfigurationComparisonResult>(snapshot.ConfigurationComparison);
        Assert.Equal(SvComparisonMode.Compatible, comparison.Mode);
        Assert.False(comparison.HasBlockingErrors);
        Assert.Equal(1, comparison.WarningCount);
        Assert.Equal("1 warning", snapshot.ConfigurationMatchSummary);
        Assert.Equal("SV_CONFREV_MISMATCH", Assert.Single(comparison.Findings).Code);
    }

    [Fact]
    public void StrictComparisonReportsBlockingErrors()
    {
        var manager = new SvStreamObservationManager();

        Assert.True(manager.TryObserve(
            DateTimeOffset.UnixEpoch,
            CreateFrame(configurationRevision: 8),
            SvObservationInputKind.PcapReplay,
            CreateProfile(),
            out var snapshot,
            comparisonMode: SvComparisonMode.Strict));

        var comparison = Assert.IsType<SvConfigurationComparisonResult>(snapshot.ConfigurationComparison);
        Assert.Equal(SvComparisonMode.Strict, comparison.Mode);
        Assert.True(comparison.HasBlockingErrors);
        Assert.Equal(1, comparison.ErrorCount);
        Assert.Equal("1 error", snapshot.ConfigurationMatchSummary);
    }

    [Fact]
    public void ExactComparisonPersistsInManagerSnapshots()
    {
        var manager = new SvStreamObservationManager();

        Assert.True(manager.TryObserve(
            DateTimeOffset.UnixEpoch,
            CreateFrame(configurationRevision: 7),
            SvObservationInputKind.LiveCapture,
            CreateProfile(),
            out var observed));

        Assert.Equal("Exact", observed.ConfigurationMatchSummary);
        var persisted = Assert.Single(manager.SnapshotAll());
        Assert.Equal("Exact", persisted.ConfigurationMatchSummary);
        var comparison = Assert.IsType<SvConfigurationComparisonResult>(persisted.ConfigurationComparison);
        Assert.True(comparison.IsExactMatch);
    }

    [Fact]
    public void AddressMismatchRejectsUnsafeSclCandidate()
    {
        var manager = new SvStreamObservationManager();
        var frame = CreateFrame(
            configurationRevision: 7,
            destinationMac: "01:0C:CD:04:00:02");

        Assert.True(manager.TryObserve(
            DateTimeOffset.UnixEpoch,
            frame,
            SvObservationInputKind.LiveCapture,
            CreateProfile(),
            out var snapshot));

        Assert.False(snapshot.IsBoundToScl);
        Assert.Null(snapshot.ExpectedConfiguration);
        Assert.Null(snapshot.ConfigurationComparison);
        Assert.Equal("Not configured", snapshot.ConfigurationMatchSummary);
        Assert.Contains(snapshot.Diagnostics, item => item.Contains("Rejected SCL candidate", StringComparison.Ordinal));
    }

    private static SampledValuesFrame CreateFrame(
        uint configurationRevision,
        string destinationMac = "01:0C:CD:04:00:01")
        => new()
        {
            Source = MacAddress.Parse("02:00:00:00:00:01"),
            Destination = MacAddress.Parse(destinationMac),
            Vlan = new VlanTag(4, 100),
            AppId = 0x4001,
            Pdu = new SampledValuesPdu
            {
                Asdus =
                [
                    new SampledValueAsdu
                    {
                        SvId = "MU01SV01",
                        DataSetReference = "MU01MUnn/LLN0$PhsMeas",
                        SampleCount = 1,
                        ConfigurationRevision = configurationRevision,
                        SampleRate = 80,
                        SampleMode = 0,
                        SamplePayload = new byte[8]
                    }
                ]
            }
        };

    private static SampledValuesPublisherProfile CreateProfile()
        => SampledValuesPublisherProfile.Create(new SclSampledValuesStream
        {
            Kind = "SV",
            IedName = "MU01",
            LdInst = "MUnn",
            ControlName = "MSVCB01",
            ControlBlockReference = "MU01MUnn/LLN0$SV$MSVCB01",
            SvId = "MU01SV01",
            DataSetName = "PhsMeas",
            DataSetReference = "MU01MUnn/LLN0$PhsMeas",
            ConfigurationRevision = 7,
            SampleRate = 80,
            SampleMode = "SmpPerPeriod",
            NoAsdu = 1,
            Address = new SclStreamAddress
            {
                AppIdText = "0x4001",
                AppId = 0x4001,
                DestinationMacText = "01:0C:CD:04:00:01",
                DestinationMac = MacAddress.Parse("01:0C:CD:04:00:01"),
                VlanId = 100,
                VlanPriority = 4
            },
            Entries =
            [
                new SclDataSetEntry
                {
                    Index = 1,
                    SignalReference = "TCTR1.Amp.instMag.i",
                    BType = "INT32",
                    Cdc = "SAV"
                },
                new SclDataSetEntry
                {
                    Index = 2,
                    SignalReference = "TCTR1.Amp.q",
                    BType = "Quality",
                    Cdc = "SAV",
                    IsQuality = true
                }
            ]
        });
}
