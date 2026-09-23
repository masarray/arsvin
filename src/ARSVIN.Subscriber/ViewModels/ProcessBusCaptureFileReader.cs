using AR.Iec61850.Capture;
using AR.Iec61850.Ethernet;

namespace ARSVIN.Subscriber.ViewModels;

/// <summary>
/// Subscriber-facing capture projection. The shared engine reads every Ethernet packet from
/// PCAP/PCAPNG; ArSubsv forwards only Sampled Values EtherType candidates into its SV parser so
/// unrelated LLDP, PTP, ARP, or management traffic is not mislabeled as an SV parse error.
/// </summary>
internal static class ProcessBusCaptureFileReader
{
    public static IEnumerable<PcapPacket> Read(string path)
    {
        foreach (var packet in AR.Iec61850.Capture.ProcessBusCaptureFileReader.Read(path))
        {
            if (!EthernetFrameCodec.TryDecode(packet.Frame, out var ethernet))
                continue;
            if (ethernet.EtherType != EthernetConstants.SampledValuesEtherType)
                continue;

            yield return packet;
        }
    }
}
