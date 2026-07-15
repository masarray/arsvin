using AR.Iec61850.Ethernet;
using AR.Iec61850.SampledValues;
using AR.Iec61850.SampledValues.Profiles;
using AR.Iec61850.Scl;
using Xunit;

namespace ARSVIN.Tests.SampledValues.Profiles;

public sealed class SvStreamObservationManagerTests
{
    [Fact]
    public void LiveAndPcapInputsShareOneStableStreamWindow()
    {
        var manager = new SvStreamObservationManager();
        var live = CreateFrame(sampleCount: 10);
        var replay = CreateFrame(sampleCount: 11);

        Assert.True(manager.TryObserve(
            DateTimeOffset.UnixEpoch,
            live,
            SvObservationInputKind.LiveCapture,
            profile: null,
            out _));
        Assert.True(manager.TryObserve(
            DateTimeOffset.UnixEpoch.AddMilliseconds(1),
            replay,
            SvObservationInputKind.PcapReplay,
            profile: null,
            out var snapshot));

        Assert.Equal(1, manager.Count);
        Assert.Equal(2, snapshot.Facts.ObservationCount);
        Assert.Equal(
            new[] { SvObservationInputKind.LiveCapture, SvObservationInputKind.PcapReplay },
            snapshot.InputKinds);
        Assert.Equal(SvObservationInputKind.PcapReplay, snapshot.LastInputKind);
        Assert.Equal("SV|4001|02:00:00:00:00:01|01:0C:CD:04:00:01|100|MU01SV01|MU01MUnn/LLN0$PhsMeas", snapshot.Key.Id);
    }

    [Fact]
    public void ConfigurationRevisionChangesRemainInOneStreamAndBecomeUnstableFacts()
    {
        var manager = new SvStreamObservationManager();

        manager.TryObserve(
            DateTimeOffset.UnixEpoch,
            CreateFrame(sampleCount: 20, configurationRevision: 1),
            SvObservationInputKind.LiveCapture,
            profile: null,
            out _);
        manager.TryObserve(
            DateTimeOffset.UnixEpoch.AddMilliseconds(1),
            CreateFrame(sampleCount: 21, configurationRevision: 2),
            SvObservationInputKind.LiveCapture,
            profile: null,
            out var snapshot);

        Assert.Equal(1, manager.Count);
        Assert.Null(snapshot.Facts.ConfigurationRevision);
        Assert.Contains(snapshot.Diagnostics, item => item.Contains("confRev changed", StringComparison.Ordinal));
    }

    [Fact]
    public void DatasetReferenceIsPartOfTheStreamIdentity()
    {
        var manager = new SvStreamObservationManager();

        manager.TryObserve(
            DateTimeOffset.UnixEpoch,
            CreateFrame(sampleCount: 1, dataSetReference: "MU01MUnn/LLN0$PhsMeas"),
            SvObservationInputKind.PcapReplay,
            profile: null,
            out _);
        manager.TryObserve(
            DateTimeOffset.UnixEpoch.AddMilliseconds(1),
            CreateFrame(sampleCount: 1, dataSetReference: "MU01MUnn/LLN0$Protection"),
            SvObservationInputKind.PcapReplay,
            profile: null,
            out _);

        Assert.Equal(2, manager.Count);
        Assert.Equal(2, manager.SnapshotAll().Count);
    }

    [Fact]
    public void SclBindingAddsDatasetSignatureAndProvenance()
    {
        var manager = new SvStreamObservationManager();
        var profile = CreateProfile();

        manager.TryObserve(
            DateTimeOffset.UnixEpoch,
            CreateFrame(sampleCount: 30),
            SvObservationInputKind.LiveCapture,
            profile,
            out var snapshot,
            nominalFrequencyHz: 50);

        Assert.True(snapshot.IsBoundToScl);
        Assert.Equal("MU01MUnn/LLN0$SV$MSVCB01", snapshot.ControlBlockReference);
        Assert.Equal(2, snapshot.Facts.DataSetSignature.Count);
        Assert.Equal(
            SvFactSource.SclDerived,
            snapshot.Facts.Provenance[nameof(SvObservedStreamFacts.DataSetSignature)]);
        Assert.Equal(
            SvFactSource.TrustedContext,
            snapshot.Facts.Provenance[nameof(SvObservedStreamFacts.NominalFrequencyHz)]);
    }

    [Fact]
    public void EmptyAsduFrameIsRejectedWithoutCreatingAStream()
    {
        var manager = new SvStreamObservationManager();
        var frame = new SampledValuesFrame
        {
            Source = MacAddress.Parse("02:00:00:00:00:01"),
            Destination = MacAddress.Parse("01:0C:CD:04:00:01"),
            AppId = 0x4001,
            Pdu = new SampledValuesPdu()
        };

        var accepted = manager.TryObserve(
            DateTimeOffset.UnixEpoch,
            frame,
            SvObservationInputKind.LiveCapture,
            profile: null,
            out _);

        Assert.False(accepted);
        Assert.Equal(0, manager.Count);
    }

    [Fact]
    public void ClearRemovesAllInputWindows()
    {
        var manager = new SvStreamObservationManager();
        manager.TryObserve(
            DateTimeOffset.UnixEpoch,
            CreateFrame(sampleCount: 1),
            SvObservationInputKind.LiveCapture,
            profile: null,
            out _);

        manager.Clear();

        Assert.Equal(0, manager.Count);
        Assert.Empty(manager.SnapshotAll());
    }

    private static SampledValuesFrame CreateFrame(
        ushort sampleCount,
        uint configurationRevision = 7,
        string dataSetReference = "MU01MUnn/LLN0$PhsMeas")
        => new()
        {
            Source = MacAddress.Parse("02:00:00:00:00:01"),
            Destination = MacAddress.Parse("01:0C:CD:04:00:01"),
            Vlan = new VlanTag(4, 100),
            AppId = 0x4001,
            Pdu = new SampledValuesPdu
            {
                Asdus =
                [
                    new SampledValueAsdu
                    {
                        SvId = "MU01SV01",
                        DataSetReference = dataSetReference,
                        SampleCount = sampleCount,
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
                new SclDataSetEntry { Index = 1, SignalReference = "TCTR1.Amp.instMag.i", BType = "INT32", Cdc = "SAV" },
                new SclDataSetEntry { Index = 2, SignalReference = "TCTR1.Amp.q", BType = "Quality", Cdc = "SAV", IsQuality = true }
            ]
        });
}
