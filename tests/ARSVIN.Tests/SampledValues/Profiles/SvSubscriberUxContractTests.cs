using AR.Iec61850.Ethernet;
using AR.Iec61850.SampledValues;
using AR.Iec61850.SampledValues.Profiles;
using Xunit;

namespace ARSVIN.Tests.SampledValues.Profiles;

public sealed class SvSubscriberUxContractTests
{
    [Fact]
    public void GenericLayer2ProfileRemainsUnknownWithInsufficientEvidence()
    {
        var manager = new SvStreamObservationManager();

        Assert.True(manager.TryObserve(
            DateTimeOffset.UnixEpoch,
            CreateFrame(sampleCount: 1),
            SvObservationInputKind.LiveCapture,
            profile: null,
            out var snapshot));

        var detection = Assert.IsType<SvProfileDetectionResult>(snapshot.ProfileDetection);
        Assert.Equal("Generic Layer-2 SV", detection.Profile.Family);
        Assert.Equal(SvProfileConfidence.Unknown, detection.Confidence);
        Assert.Equal(5, detection.EvaluatedWeight);
        Assert.Equal(100, detection.ScorePercent);
    }

    [Fact]
    public void ObservationSnapshotCarriesWindowFactsForCompactState()
    {
        var manager = new SvStreamObservationManager();

        manager.TryObserve(
            DateTimeOffset.UnixEpoch,
            CreateFrame(sampleCount: 10),
            SvObservationInputKind.PcapReplay,
            profile: null,
            out _);
        manager.TryObserve(
            DateTimeOffset.UnixEpoch.AddSeconds(2),
            CreateFrame(sampleCount: 11),
            SvObservationInputKind.PcapReplay,
            profile: null,
            out var snapshot);

        Assert.Equal(2, snapshot.Facts.ObservationCount);
        Assert.Equal(1, snapshot.Facts.AsduPerFrame);
        Assert.Equal(DateTimeOffset.UnixEpoch, snapshot.Facts.FirstTimestamp);
        Assert.Equal(DateTimeOffset.UnixEpoch.AddSeconds(2), snapshot.Facts.LastTimestamp);
    }

    private static SampledValuesFrame CreateFrame(ushort sampleCount)
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
                        DataSetReference = "MU01MUnn/LLN0$PhsMeas",
                        SampleCount = sampleCount,
                        ConfigurationRevision = 7,
                        SampleRate = 80,
                        SampleMode = 0,
                        SamplePayload = new byte[8]
                    }
                ]
            }
        };
}
