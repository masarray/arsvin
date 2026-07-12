using AR.Iec61850.Scl;
using Xunit;

namespace ARSVIN.Tests.Scl;

public sealed class SclParserTests
{
    [Fact]
    public void ParseBuildsTypedSampledValuesStreamAddressAndReportControl()
    {
        var document = new SclParser().Parse(CompleteScl, "demo.scd");

        Assert.Equal("demo.scd", document.SourceName);
        Assert.Equal("DEMO", document.HeaderId);
        Assert.Equal(SclEdition.Edition21, document.Edition);
        Assert.Equal("MU01", Assert.Single(document.Ieds).Name);

        var dataSet = Assert.Single(document.DataSets);
        Assert.Equal("MU01MUnn/LLN0$PhsMeas", dataSet.Reference);
        Assert.Equal(2, dataSet.Entries.Count);
        Assert.Equal("INT32", dataSet.Entries[0].BType);
        Assert.True(dataSet.Entries[1].IsQuality);

        var stream = Assert.Single(document.SampledValuesStreams);
        Assert.Equal("MU01SV01", stream.SvId);
        Assert.Equal((uint)7, stream.ConfigurationRevision);
        Assert.Equal((ushort)80, stream.SampleRate);
        Assert.Equal((ushort)4, stream.NoAsdu);
        Assert.Equal("0x4001", stream.Address.AppIdText);
        Assert.Equal((ushort)0x4001, stream.Address.AppId);
        Assert.Equal("01:0C:CD:04:00:01", stream.Address.DestinationMacText);
        Assert.Equal((ushort)100, stream.Address.VlanId);
        Assert.Equal((byte)4, stream.Address.VlanPriority);

        var report = Assert.Single(document.ReportControls);
        Assert.True(report.Buffered);
        Assert.Equal("MU01MUnn/LLN0$BR$BRCB01", report.ControlBlockReference);
        Assert.Equal((uint)1000, report.IntegrityPeriodMilliseconds);
        Assert.Equal(2, report.Entries.Count);
        Assert.Empty(document.Conflicts);
    }

    [Fact]
    public void MissingDatasetProducesExplicitWarningAndSafeNoAsduDefault()
    {
        const string xml = """
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <IED name="IED1">
                <AccessPoint name="P1">
                  <Server>
                    <LDevice inst="LD0">
                      <LN0 lnClass="LLN0">
                        <SampledValueControl name="SV1" datSet="Missing" svID="IED1SV1" smpRate="4000" nofASDU="0" />
                      </LN0>
                    </LDevice>
                  </Server>
                </AccessPoint>
              </IED>
            </SCL>
            """;

        var document = new SclParser().Parse(xml);

        var stream = Assert.Single(document.SampledValuesStreams);
        Assert.Equal((ushort)1, stream.NoAsdu);
        Assert.Empty(stream.Entries);
        Assert.Contains(document.Warnings, warning => warning.Contains("references missing DataSet 'Missing'", StringComparison.Ordinal));
        Assert.Contains(document.Warnings, warning => warning.Contains("No DataSet element", StringComparison.Ordinal));
    }

    [Fact]
    public void ParseRejectsNonSclRoot()
    {
        var ex = Assert.Throws<InvalidDataException>(() => new SclParser().Parse("<Configuration />"));

        Assert.Contains("not an IEC 61850 SCL document", ex.Message);
    }

    [Fact]
    public void LoadUsesConfigurationFileNameAsSourceName()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"arsvin-scl-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "station.scd");

        try
        {
            File.WriteAllText(path, CompleteScl);

            var document = new SclParser().Load(path);

            Assert.Equal("station.scd", document.SourceName);
            Assert.Single(document.SampledValuesStreams);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    private const string CompleteScl = """
        <SCL xmlns="http://www.iec.ch/61850/2003/SCL" version="2007" revision="B" release="4">
          <Header id="DEMO" version="1" revision="2" />
          <Communication>
            <SubNetwork name="ProcessBus" type="8-MMS">
              <ConnectedAP iedName="MU01" apName="P1">
                <SMV ldInst="MUnn" cbName="MSVCB01">
                  <Address>
                    <P type="MAC-Address">01-0C-CD-04-00-01</P>
                    <P type="APPID">4001</P>
                    <P type="VLAN-ID">100</P>
                    <P type="VLAN-PRIORITY">4</P>
                  </Address>
                </SMV>
              </ConnectedAP>
            </SubNetwork>
          </Communication>
          <IED name="MU01" manufacturer="ARSVIN" type="MU" configVersion="1">
            <AccessPoint name="P1">
              <Server>
                <LDevice inst="MUnn">
                  <LN0 lnClass="LLN0" lnType="LLN0_type">
                    <DataSet name="PhsMeas">
                      <FCDA ldInst="MUnn" lnClass="TCTR" lnInst="1" doName="Amp" daName="instMag.i" fc="MX" />
                      <FCDA ldInst="MUnn" lnClass="TCTR" lnInst="1" doName="Amp" daName="q" fc="MX" />
                    </DataSet>
                    <SampledValueControl name="MSVCB01" datSet="PhsMeas" svID="MU01SV01" confRev="7" smpRate="80" smpMod="SmpPerPeriod" nofASDU="4" />
                    <ReportControl name="BRCB01" datSet="PhsMeas" rptID="MU01_BRCB01" buffered="true" confRev="2" bufTime="100" intgPd="1000" />
                  </LN0>
                  <LN lnClass="TCTR" inst="1" lnType="TCTR_type" />
                </LDevice>
              </Server>
            </AccessPoint>
          </IED>
          <DataTypeTemplates>
            <LNodeType id="LLN0_type" lnClass="LLN0" />
            <LNodeType id="TCTR_type" lnClass="TCTR">
              <DO name="Amp" type="MV_type" />
            </LNodeType>
            <DOType id="MV_type" cdc="MV">
              <DA name="instMag" bType="Struct" type="Vector_type" fc="MX" />
              <DA name="q" bType="Quality" fc="MX" />
            </DOType>
            <DAType id="Vector_type">
              <BDA name="i" bType="INT32" />
            </DAType>
          </DataTypeTemplates>
        </SCL>
        """;
}
