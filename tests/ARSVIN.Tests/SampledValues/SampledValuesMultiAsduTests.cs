using AR.Iec61850.Ethernet;
using AR.Iec61850.Mms;
using AR.Iec61850.SampledValues;
using AR.Iec61850.Scl;
using Xunit;

namespace ARSVIN.Tests.SampledValues;

public sealed class SampledValuesMultiAsduTests
{
    [Fact]
    public void PublisherProfileBuildsAndParsesMultiAsduFrame()
    {
        var profile = SampledValuesPublisherProfile.Create(CreateStream(noAsdu: 4));
        var payloads = Enumerable.Range(0, 4)
            .Select(i => profile.BuildPayload(new[]
            {
                MmsDataValue.Integer(100 + i),
                MmsDataValue.BitString(0, SampledValueQuality.Good.ToBytes())
            }))
            .ToArray();

        var frameBytes = profile.BuildEthernetFrame(
            MacAddress.Parse("02:00:00:00:20:01"),
            sampleCount: 65534,
            samplePayloads: payloads,
            sampleSynchronization: 2);

        Assert.True(SampledValuesFrameParser.TryParseEthernetFrame(frameBytes, out var parsed));
        Assert.Equal(0x4001, parsed.AppId);
        Assert.Equal(4, parsed.Pdu.Asdus.Count);
        Assert.Equal(new ushort[] { 65534, 65535, 0, 1 }, parsed.Pdu.Asdus.Select(a => a.SampleCount).ToArray());
        Assert.All(parsed.Pdu.Asdus, asdu => Assert.Equal("MU01SV01", asdu.SvId));
        Assert.Equal(payloads[2], parsed.Pdu.Asdus[2].SamplePayload);
    }

    [Fact]
    public void PublisherProfileRejectsWrongPayloadBatchSize()
    {
        var profile = SampledValuesPublisherProfile.Create(CreateStream(noAsdu: 4));

        var ex = Assert.Throws<ArgumentException>(() => profile.BuildEthernetFrame(
            MacAddress.Parse("02:00:00:00:20:01"),
            sampleCount: 0,
            samplePayloads: new[] { new byte[profile.PayloadLayout.PayloadByteLength] }));

        Assert.Contains("nofASDU=4", ex.Message);
    }

    [Fact]
    public void FramePreviewUsesPublicationRateAfterAsduPacking()
    {
        var profile = SampledValuesPublisherProfile.Create(CreateStream(noAsdu: 8));
        var preview = SampledValuesFramePreview.FromProfile(profile, sampleRateHz: 12800);

        Assert.Equal((ushort)8, preview.NoAsdu);
        Assert.Equal(1600, preview.PublicationRateHz);
        Assert.True(preview.EstimatedEthernetBytes > 0);
        Assert.True(preview.EstimatedBandwidthBitsPerSecond > 0);
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
            DataSetName = "MSVCB01Dataset",
            DataSetReference = "MU01MUnn/LLN0$MSVCB01Dataset",
            ConfigurationRevision = 1,
            SampleRate = 80,
            SampleMode = "SmpPerPeriod",
            NoAsdu = noAsdu,
            Address = new SclStreamAddress
            {
                AppIdText = "4001",
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
                    Index = 0,
                    SignalReference = "MU01MUnn/TCTR1.Amp.instMag.i",
                    LnClass = "TCTR",
                    LnInst = "1",
                    DoName = "Amp",
                    DaName = "instMag.i",
                    Fc = "MX",
                    BType = "INT32"
                },
                new SclDataSetEntry
                {
                    Index = 1,
                    SignalReference = "MU01MUnn/TCTR1.Amp.q",
                    LnClass = "TCTR",
                    LnInst = "1",
                    DoName = "Amp",
                    DaName = "q",
                    Fc = "MX",
                    BType = "Quality",
                    IsQuality = true
                }
            }
        };
}
