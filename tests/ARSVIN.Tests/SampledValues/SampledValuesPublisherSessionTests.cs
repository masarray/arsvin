using AR.Iec61850.Ethernet;
using AR.Iec61850.SampledValues;
using AR.Iec61850.Scl;
using AR.Iec61850.Transports;
using Xunit;

namespace ARSVIN.Tests.SampledValues;

public sealed class SampledValuesPublisherSessionTests
{
    [Fact]
    public async Task PublishNextAsyncSendsFrameAndWrapsSampleCounter()
    {
        var profile = SampledValuesPublisherProfile.Create(CreateStream(noAsdu: 1));
        var transport = new InMemoryProcessBusTransport();
        var session = new SampledValuesPublisherSession(
            profile,
            MacAddress.Parse("02:00:00:00:20:01"),
            transport,
            initialSampleCount: 3999,
            sampleCounterWrap: 4000);
        var payload = new byte[profile.PayloadLayout.PayloadByteLength];

        var frame = await session.PublishNextAsync(payload);

        Assert.Equal((ushort)0, session.NextSampleCount);
        Assert.Equal(frame, Assert.Single(transport.Frames));
        Assert.True(SampledValuesFrameParser.TryParseEthernetFrame(frame, out var parsed));
        Assert.Equal((ushort)3999, Assert.Single(parsed.Pdu.Asdus).SampleCount);
    }

    [Fact]
    public async Task PublishNextBatchAsyncAdvancesByAsduCountAcrossWrap()
    {
        var profile = SampledValuesPublisherProfile.Create(CreateStream(noAsdu: 4));
        var transport = new InMemoryProcessBusTransport();
        var session = new SampledValuesPublisherSession(
            profile,
            MacAddress.Parse("02:00:00:00:20:02"),
            transport,
            initialSampleCount: 3998,
            sampleCounterWrap: 4000);
        var payloads = Enumerable.Range(0, 4)
            .Select(_ => new byte[profile.PayloadLayout.PayloadByteLength])
            .ToArray();

        var frame = await session.PublishNextBatchAsync(payloads);

        Assert.Equal((ushort)2, session.NextSampleCount);
        Assert.True(SampledValuesFrameParser.TryParseEthernetFrame(frame, out var parsed));
        Assert.Equal(new ushort[] { 3998, 3999, 0, 1 }, parsed.Pdu.Asdus.Select(asdu => asdu.SampleCount).ToArray());
    }

    [Fact]
    public async Task SinglePayloadApiRejectsMultiAsduProfile()
    {
        var profile = SampledValuesPublisherProfile.Create(CreateStream(noAsdu: 4));
        var session = new SampledValuesPublisherSession(
            profile,
            MacAddress.Parse("02:00:00:00:20:03"),
            new InMemoryProcessBusTransport());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await session.PublishNextAsync(new byte[profile.PayloadLayout.PayloadByteLength]));

        Assert.Contains("Use PublishNextBatchAsync", ex.Message);
    }

    [Fact]
    public void ConstructorRejectsDegenerateSampleCounterWrap()
    {
        var profile = SampledValuesPublisherProfile.Create(CreateStream(noAsdu: 1));

        Assert.Throws<ArgumentOutOfRangeException>(() => new SampledValuesPublisherSession(
            profile,
            MacAddress.Parse("02:00:00:00:20:04"),
            new InMemoryProcessBusTransport(),
            sampleCounterWrap: 1));
    }

    private static SclSampledValuesStream CreateStream(ushort noAsdu)
        => new()
        {
            Kind = "SV",
            IedName = "MU01",
            LdInst = "MUnn",
            ControlName = "MSVCB01",
            ControlBlockReference = "MU01MUnn/LLN0$SV$MSVCB01",
            SvId = "MU01SV01",
            DataSetName = "PhsMeas",
            DataSetReference = "MU01MUnn/LLN0$PhsMeas",
            ConfigurationRevision = 1,
            SampleRate = 80,
            SampleMode = "SmpPerPeriod",
            NoAsdu = noAsdu,
            Address = new SclStreamAddress
            {
                AppIdText = "0x4001",
                AppId = 0x4001,
                DestinationMacText = "01:0C:CD:04:00:01",
                DestinationMac = MacAddress.Parse("01:0C:CD:04:00:01"),
                VlanId = 100,
                VlanPriority = 4
            },
            Entries = new[]
            {
                new SclDataSetEntry
                {
                    Index = 1,
                    SignalReference = "MU01/MUnn/TCTR1.Amp.instMag.i [MX]",
                    LnClass = "TCTR",
                    LnInst = "1",
                    DoName = "Amp",
                    DaName = "instMag.i",
                    Fc = "MX",
                    BType = "INT32"
                }
            }
        };
}
