using AR.Iec61850.Mms;
using System.Buffers.Binary;
using AR.Iec61850.SampledValues;
using AR.Iec61850.Scl;

namespace ARSVIN.Subscriber.Models;

internal sealed class SvStreamRuntime
{
    private const int MaxWaveformPoints = 640;

    private readonly object _gate = new();
    private readonly Queue<WaveformPoint> _waveform = new(MaxWaveformPoints + 8);
    private readonly List<string> _diagnostics = new();
    private DateTimeOffset? _firstSeen;
    private DateTimeOffset? _lastSeen;
    private ushort? _expectedNext;
    private ushort? _lastSampleCount;
    private long _frameCount;
    private long _asduCount;
    private double _gapTotalMs;
    private double _maxGapMs;
    private int _gapSamples;
    private int _sequenceGaps;
    private int _duplicates;
    private int _outOfOrder;
    private int _payloadIssues;
    private int _sclMismatches;
    private int _waveformIndex;
    private int _qualityGood;
    private int _qualityNonZero;
    private string _lastHealthDetail = string.Empty;
    private string _layoutBinding = string.Empty;
    private IReadOnlyList<DecodedValueRow> _decodedValues = Array.Empty<DecodedValueRow>();

    public SvStreamRuntime(string key)
    {
        Key = key;
    }

    public string Key { get; }
    public ushort AppId { get; private set; }
    public string Source { get; private set; } = string.Empty;
    public string Destination { get; private set; } = string.Empty;
    public ushort? VlanId { get; private set; }
    public byte? VlanPriority { get; private set; }
    public string SvId { get; private set; } = string.Empty;
    public string DataSet { get; private set; } = string.Empty;
    public uint? ConfRev { get; private set; }
    public int NofAsdu { get; private set; }
    public ushort? SampleRate { get; private set; }
    public ushort? SampleMode { get; private set; }
    public byte? SmpSynch { get; private set; }
    public bool IsBoundToScl { get; private set; }
    public string ControlBlockReference { get; private set; } = string.Empty;
    public string LayoutBinding { get; private set; } = string.Empty;

    public void Observe(DateTimeOffset timestamp, SampledValuesFrame frame, SampledValuesPublisherProfile? profile)
    {
        var asdus = frame.Pdu.Asdus;
        var first = asdus.FirstOrDefault();
        var diagnostics = new List<string>();
        var latestRows = new List<DecodedValueRow>();
        var points = new List<WaveformPoint>();
        var qualityGood = 0;
        var qualityNonZero = 0;

        if (asdus.Count == 0)
        {
            diagnostics.Add("SV frame contains no ASDU.");
            IncrementPayloadIssue();
        }

        var layoutBinding = string.Empty;
        if (profile is not null)
        {
            layoutBinding = $"SCL: {profile.Stream.ControlBlockReference}";
            ValidateAgainstScl(frame, asdus, profile, diagnostics);
            foreach (var asdu in asdus)
            {
                var rows = DecodePayload(asdu, profile.PayloadLayout, diagnostics).ToArray();
                if (latestRows.Count == 0)
                    latestRows.AddRange(rows);

                points.Add(BuildWaveformPoint(asdu.SampleCount, rows));
                CountQuality(rows, ref qualityGood, ref qualityNonZero);
            }
        }
        else
        {
            foreach (var asdu in asdus)
            {
                if (TryDecodeAutoPayload(asdu, diagnostics, out var rows, out var binding))
                {
                    layoutBinding = binding;
                    if (latestRows.Count == 0)
                        latestRows.AddRange(rows);

                    points.Add(BuildWaveformPoint(asdu.SampleCount, rows));
                    CountQuality(rows, ref qualityGood, ref qualityNonZero);
                }
            }

            if (latestRows.Count == 0)
                diagnostics.Add("No SCL binding and payload layout is unknown. Import SCL or use a fixed 9-2LE/UCA-style stream.");
            else
                diagnostics.Add($"{layoutBinding}. SCL not loaded; channel names are inferred from the fixed payload profile.");
        }

        lock (_gate)
        {
            if (_firstSeen is null)
                _firstSeen = timestamp;

            if (_lastSeen is { } previous)
            {
                var gap = (timestamp - previous).TotalMilliseconds;
                if (gap >= 0)
                {
                    _gapTotalMs += gap;
                    _gapSamples++;
                    _maxGapMs = Math.Max(_maxGapMs, gap);
                }
            }

            _lastSeen = timestamp;
            _frameCount++;
            _asduCount += asdus.Count;
            AppId = frame.AppId;
            Source = frame.Source.ToString();
            Destination = frame.Destination.ToString();
            VlanId = frame.Vlan?.VlanId;
            VlanPriority = frame.Vlan?.PriorityCodePoint;
            NofAsdu = asdus.Count;
            IsBoundToScl = profile is not null;
            ControlBlockReference = profile?.Stream.ControlBlockReference ?? string.Empty;
            LayoutBinding = layoutBinding;

            if (first is not null)
            {
                SvId = first.SvId;
                DataSet = first.DataSetReference;
                ConfRev = first.ConfigurationRevision;
                SampleRate = first.SampleRate;
                SampleMode = first.SampleMode;
                SmpSynch = first.SampleSynchronization;
            }

            foreach (var asdu in asdus)
                RecordSample(asdu.SampleCount, ResolveCounterWrap(asdu, profile));

            foreach (var point in points)
            {
                _waveform.Enqueue(point);
                while (_waveform.Count > MaxWaveformPoints)
                    _waveform.Dequeue();
            }

            _qualityGood += qualityGood;
            _qualityNonZero += qualityNonZero;

            if (diagnostics.Any(x => x.Contains("payload", StringComparison.OrdinalIgnoreCase) || x.Contains("decode", StringComparison.OrdinalIgnoreCase)))
                _payloadIssues++;

            if (diagnostics.Any(x => x.Contains("mismatch", StringComparison.OrdinalIgnoreCase) || x.Contains("differs", StringComparison.OrdinalIgnoreCase)))
                _sclMismatches++;

            _diagnostics.Clear();
            _diagnostics.AddRange(diagnostics.Take(12));
            _decodedValues = latestRows.ToArray();
            _lastHealthDetail = diagnostics.Count == 0 ? "SV stream is stable." : diagnostics[0];
        }
    }

    public SvStreamSnapshot Snapshot()
    {
        lock (_gate)
        {
            var duration = _firstSeen.HasValue && _lastSeen.HasValue
                ? Math.Max(0.001, (_lastSeen.Value - _firstSeen.Value).TotalSeconds)
                : 0.001;
            var fps = _frameCount / duration;
            var issues = _sequenceGaps + _duplicates + _outOfOrder + _payloadIssues + _sclMismatches;
            var health = issues > 0
                ? (_payloadIssues > 0 || _sclMismatches > 0 || _outOfOrder > 0 ? "BAD" : "WARN")
                : IsBoundToScl ? "GOOD" : "WARN";
            var allPoints = _waveform.ToArray();
            var visiblePoints = BuildLockedTwoCycleWindow(allPoints, SampleRate);
            var phasors = ComputePhasors(visiblePoints, SampleRate).ToArray();

            return new SvStreamSnapshot
            {
                Key = Key,
                Health = health,
                HealthDetail = _lastHealthDetail,
                AppId = AppId,
                Source = Source,
                Destination = Destination,
                VlanId = VlanId,
                VlanPriority = VlanPriority,
                SvId = SvId,
                DataSet = DataSet,
                ConfRev = ConfRev,
                NofAsdu = NofAsdu,
                LastSmpCnt = _lastSampleCount,
                SampleRate = SampleRate,
                SampleMode = SampleMode,
                SmpSynch = SmpSynch,
                FrameCount = _frameCount,
                AsduCount = _asduCount,
                ActualFps = fps,
                AverageFrameGapMilliseconds = _gapSamples == 0 ? 0 : _gapTotalMs / _gapSamples,
                MaxFrameGapMilliseconds = _maxGapMs,
                SequenceGapCount = _sequenceGaps,
                DuplicateCount = _duplicates,
                OutOfOrderCount = _outOfOrder,
                PayloadIssueCount = _payloadIssues,
                SclMismatchCount = _sclMismatches,
                IsBoundToScl = IsBoundToScl,
                ControlBlockReference = ControlBlockReference,
                LayoutBinding = LayoutBinding,
                LastSeen = _lastSeen?.ToLocalTime().ToString("HH:mm:ss.fff") ?? "-",
                Diagnostics = _diagnostics.ToArray(),
                Values = _decodedValues,
                WaveformPoints = visiblePoints,
                Phasors = phasors,
                CursorSummary = BuildCursorSummary(visiblePoints),
                QualitySummary = _qualityGood + _qualityNonZero == 0
                    ? "Quality not decoded"
                    : $"Quality good {_qualityGood:N0}, non-zero {_qualityNonZero:N0}"
            };
        }
    }

    private static bool TryDecodeAutoPayload(SampledValueAsdu asdu, ICollection<string> diagnostics, out DecodedValueRow[] rows, out string layoutBinding)
    {
        rows = Array.Empty<DecodedValueRow>();
        layoutBinding = string.Empty;

        var payload = asdu.SamplePayload.AsSpan();
        if (payload.Length < 8 || payload.Length % 8 != 0)
        {
            diagnostics.Add($"Auto payload decode skipped: expected value+quality pairs of 8 bytes, got {payload.Length} byte(s).");
            return false;
        }

        var pairCount = payload.Length / 8;
        var channels = ResolveAutoAnalogChannels(pairCount);
        if (channels.Count == 0)
        {
            diagnostics.Add($"Auto payload decode skipped: unsupported fixed pair count {pairCount}.");
            return false;
        }

        var result = new List<DecodedValueRow>(pairCount * 2);
        for (var pair = 0; pair < pairCount; pair++)
        {
            var offset = pair * 8;
            var channel = channels[pair];
            var valueBytes = payload.Slice(offset, 4);
            var qualityBytes = payload.Slice(offset + 4, 4);
            var value = BinaryPrimitives.ReadInt32BigEndian(valueBytes);
            var signalReference = BuildAutoSignalReference(channel);

            result.Add(new DecodedValueRow
            {
                Index = result.Count + 1,
                Signal = signalReference,
                Kind = channel.StartsWith('V') ? "Voltage" : "Current",
                Value = value.ToString(CultureInfo.InvariantCulture),
                Raw = Convert.ToHexString(valueBytes),
                NumericValue = value
            });

            result.Add(new DecodedValueRow
            {
                Index = result.Count + 1,
                Signal = signalReference + ".q",
                Kind = "Quality",
                Value = Convert.ToHexString(qualityBytes),
                Raw = Convert.ToHexString(qualityBytes),
                NumericValue = null
            });
        }

        rows = result.ToArray();
        layoutBinding = pairCount == 8
            ? "Auto 9-2LE/UCA fixed 4I+4V layout"
            : $"Auto fixed value+quality layout ({pairCount} analog channels)";
        return true;
    }

    private static IReadOnlyList<string> ResolveAutoAnalogChannels(int pairCount)
    {
        return pairCount switch
        {
            3 => new[] { "Ia", "Ib", "Ic" },
            8 => new[] { "Ia", "Ib", "Ic", "In", "Va", "Vb", "Vc", "Vn" },
            12 => new[] { "Ia", "Ib", "Ic", "I4", "I5", "I6", "I7", "I8", "Va", "Vb", "Vc", "Vn" },
            15 => new[] { "Ia", "Ib", "Ic", "I4", "I5", "I6", "I7", "I8", "I9", "Va", "Vb", "Vc", "V4", "V5", "V6" },
            16 => new[] { "Ia", "Ib", "Ic", "I4", "I5", "I6", "I7", "I8", "I9", "I10", "I11", "I12", "Va", "Vb", "Vc", "Vn" },
            20 => new[] { "Ia", "Ib", "Ic", "I4", "I5", "I6", "I7", "I8", "I9", "I10", "I11", "I12", "Va", "Vb", "Vc", "Vn", "V5", "V6", "V7", "V8" },
            _ => Array.Empty<string>()
        };
    }

    private static string BuildAutoSignalReference(string channel)
    {
        return channel switch
        {
            "Ia" => "TCTR1/AmpSv.instMag.i",
            "Ib" => "TCTR2/AmpSv.instMag.i",
            "Ic" => "TCTR3/AmpSv.instMag.i",
            "In" => "TCTR4/AmpSv.instMag.i",
            "Va" => "TVTR1/VolSv.instMag.i",
            "Vb" => "TVTR2/VolSv.instMag.i",
            "Vc" => "TVTR3/VolSv.instMag.i",
            "Vn" => "TVTR4/VolSv.instMag.i",
            _ when channel.StartsWith('V') => channel + "/VolSv.instMag.i",
            _ => channel + "/AmpSv.instMag.i"
        };
    }

    private WaveformPoint BuildWaveformPoint(ushort? smpCnt, IEnumerable<DecodedValueRow> rows)
    {
        var point = new WaveformPoint { Index = _waveformIndex++, SampleCount = smpCnt };
        foreach (var row in rows)
        {
            if (!row.NumericValue.HasValue)
                continue;

            var channel = ClassifyAnalogChannel(row.Signal, row.Kind);
            switch (channel)
            {
                case "Ia": point.Ia = row.NumericValue.Value; break;
                case "Ib": point.Ib = row.NumericValue.Value; break;
                case "Ic": point.Ic = row.NumericValue.Value; break;
                case "In": point.In = row.NumericValue.Value; break;
                case "Va": point.Va = row.NumericValue.Value; break;
                case "Vb": point.Vb = row.NumericValue.Value; break;
                case "Vc": point.Vc = row.NumericValue.Value; break;
                case "Vn": point.Vn = row.NumericValue.Value; break;
            }
        }

        return point;
    }

    private static string ClassifyAnalogChannel(string reference, string kind)
    {
        if (kind.Contains("Quality", StringComparison.OrdinalIgnoreCase) || kind.Contains("Timestamp", StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        var text = reference.Replace('$', '.').Replace('/', '.').ToLowerInvariant();
        if (text is "ia" or "amp.ia" or "current.ia") return "Ia";
        if (text is "ib" or "amp.ib" or "current.ib") return "Ib";
        if (text is "ic" or "amp.ic" or "current.ic") return "Ic";
        if (text is "in" or "amp.in" or "current.in") return "In";
        if (text is "va" or "vol.va" or "voltage.va") return "Va";
        if (text is "vb" or "vol.vb" or "voltage.vb") return "Vb";
        if (text is "vc" or "vol.vc" or "voltage.vc") return "Vc";
        if (text is "vn" or "vol.vn" or "voltage.vn") return "Vn";

        var isVoltage = text.Contains("tvtr", StringComparison.Ordinal) || text.Contains("vol", StringComparison.Ordinal) || text.Contains("voltage", StringComparison.Ordinal);
        var prefix = isVoltage ? "V" : "I";

        if (text.Contains("tctr4", StringComparison.Ordinal) || text.Contains("tvtr4", StringComparison.Ordinal) || text.Contains("neut", StringComparison.Ordinal) || text.Contains("phsn", StringComparison.Ordinal) || text.Contains(".n", StringComparison.Ordinal))
            return prefix + "n";
        if (text.Contains("tctr3", StringComparison.Ordinal) || text.Contains("tvtr3", StringComparison.Ordinal) || text.Contains("phsc", StringComparison.Ordinal) || text.Contains("ic", StringComparison.Ordinal) || text.Contains("vc", StringComparison.Ordinal))
            return prefix + "c";
        if (text.Contains("tctr2", StringComparison.Ordinal) || text.Contains("tvtr2", StringComparison.Ordinal) || text.Contains("phsb", StringComparison.Ordinal) || text.Contains("ib", StringComparison.Ordinal) || text.Contains("vb", StringComparison.Ordinal))
            return prefix + "b";
        if (text.Contains("tctr1", StringComparison.Ordinal) || text.Contains("tvtr1", StringComparison.Ordinal) || text.Contains("phsa", StringComparison.Ordinal) || text.Contains("ia", StringComparison.Ordinal) || text.Contains("va", StringComparison.Ordinal))
            return prefix + "a";

        return string.Empty;
    }

    private static void CountQuality(IEnumerable<DecodedValueRow> rows, ref int good, ref int nonZero)
    {
        foreach (var row in rows.Where(x => x.Kind.Contains("Quality", StringComparison.OrdinalIgnoreCase)))
        {
            if (string.IsNullOrWhiteSpace(row.Raw) || row.Raw.All(c => c is '0'))
                good++;
            else
                nonZero++;
        }
    }

    private static IEnumerable<PhasorVector> ComputePhasors(IReadOnlyList<WaveformPoint> points, ushort? sampleRate)
    {
        var cyclePoints = ResolvePointsPerCycle(sampleRate, points.Count);
        var window = points.TakeLast(cyclePoints).ToArray();
        if (window.Length < 8)
            return Array.Empty<PhasorVector>();

        var phasors = new List<PhasorVector>();
        foreach (var item in new[]
        {
            (Name: "Ia", Kind: "Current", Values: window.Select(x => x.Ia)),
            (Name: "Ib", Kind: "Current", Values: window.Select(x => x.Ib)),
            (Name: "Ic", Kind: "Current", Values: window.Select(x => x.Ic)),
            (Name: "In", Kind: "Current", Values: window.Select(x => x.In)),
            (Name: "Va", Kind: "Voltage", Values: window.Select(x => x.Va)),
            (Name: "Vb", Kind: "Voltage", Values: window.Select(x => x.Vb)),
            (Name: "Vc", Kind: "Voltage", Values: window.Select(x => x.Vc)),
            (Name: "Vn", Kind: "Voltage", Values: window.Select(x => x.Vn))
        })
        {
            var values = item.Values.Where(x => x.HasValue).Select(x => x!.Value).ToArray();
            if (values.Length < 8)
                continue;

            var rms = Math.Sqrt(values.Select(x => x * x).Average());
            var peak = values.Select(Math.Abs).DefaultIfEmpty(0).Max();
            var sin = 0.0;
            var cos = 0.0;
            for (var i = 0; i < values.Length; i++)
            {
                var theta = 2.0 * Math.PI * i / values.Length;
                sin += values[i] * Math.Sin(theta);
                cos += values[i] * Math.Cos(theta);
            }

            var angle = Math.Atan2(cos, sin) * 180.0 / Math.PI;
            phasors.Add(new PhasorVector
            {
                Channel = item.Name,
                Kind = item.Kind,
                Rms = rms,
                Peak = peak,
                AngleDegrees = NormalizeAngle(angle)
            });
        }

        var va = phasors.FirstOrDefault(x => string.Equals(x.Channel, "Va", StringComparison.OrdinalIgnoreCase) && x.Rms > 0);
        if (va is null)
            return phasors;

        return phasors.Select(item => new PhasorVector
        {
            Channel = item.Channel,
            Kind = item.Kind,
            Rms = item.Rms,
            Peak = item.Peak,
            AngleDegrees = NormalizeAngle(item.AngleDegrees - va.AngleDegrees)
        }).ToArray();
    }


    private static WaveformPoint[] BuildLockedTwoCycleWindow(IReadOnlyList<WaveformPoint> points, ushort? sampleRate)
    {
        if (points.Count == 0)
            return Array.Empty<WaveformPoint>();

        var pointsPerCycle = ResolvePointsPerCycle(sampleRate, points.Count);
        if (pointsPerCycle <= 0)
            return points.ToArray();

        var window = Math.Clamp(pointsPerCycle * 2, 32, 512);
        var slots = new WaveformPoint?[window];

        foreach (var point in points)
        {
            var slot = point.SampleCount.HasValue
                ? point.SampleCount.Value % window
                : point.Index % window;
            slots[slot] = point;
        }

        var result = new List<WaveformPoint>(window);
        for (var slot = 0; slot < slots.Length; slot++)
        {
            if (slots[slot] is { } point)
            {
                result.Add(new WaveformPoint
                {
                    Index = slot,
                    SampleCount = point.SampleCount,
                    Ia = point.Ia,
                    Ib = point.Ib,
                    Ic = point.Ic,
                    In = point.In,
                    Va = point.Va,
                    Vb = point.Vb,
                    Vc = point.Vc,
                    Vn = point.Vn
                });
            }
        }

        return result.Count >= Math.Min(16, window) ? result.ToArray() : points.TakeLast(Math.Min(window, points.Count)).ToArray();
    }

    private static int ResolvePointsPerCycle(ushort? sampleRate, int available)
    {
        if (available <= 0)
            return 0;

        var candidate = sampleRate is > 1000 ? (int)Math.Round(sampleRate.Value / 50.0) : sampleRate ?? 80;
        candidate = Math.Clamp(candidate, 16, 256);
        return Math.Min(candidate, available);
    }

    private static double NormalizeAngle(double angle)
    {
        while (angle > 180) angle -= 360;
        while (angle <= -180) angle += 360;
        return angle;
    }

    private static string BuildCursorSummary(IReadOnlyList<WaveformPoint> points)
    {
        if (points.Count < 2)
            return "Cursor compare: waiting for waveform samples.";

        var a = points[Math.Max(0, points.Count - 2)];
        var b = points[^1];
        var iaDelta = Delta(a.Ia, b.Ia);
        var vaDelta = Delta(a.Va, b.Va);
        return $"Cursor compare: ΔIa={iaDelta}, ΔVa={vaDelta}, smpCnt {a.SampleCount?.ToString() ?? "-"} → {b.SampleCount?.ToString() ?? "-"}";
    }

    private static string Delta(double? a, double? b)
        => a.HasValue && b.HasValue ? (b.Value - a.Value).ToString("0.###") : "-";

    private void RecordSample(ushort sampleCount, ushort? wrap)
    {
        if (!_lastSampleCount.HasValue)
        {
            _lastSampleCount = sampleCount;
            _expectedNext = NextSample(sampleCount, wrap);
            return;
        }

        var previous = _lastSampleCount.Value;
        if (sampleCount == previous)
        {
            _duplicates++;
            return;
        }

        var expected = _expectedNext ?? NextSample(previous, wrap);
        if (sampleCount == expected)
        {
            _lastSampleCount = sampleCount;
            _expectedNext = NextSample(sampleCount, wrap);
            return;
        }

        if (IsForwardJump(expected, sampleCount, wrap))
            _sequenceGaps++;
        else
            _outOfOrder++;

        _lastSampleCount = sampleCount;
        _expectedNext = NextSample(sampleCount, wrap);
    }

    private static ushort NextSample(ushort sampleCount, ushort? wrap)
    {
        if (wrap is > 0)
            return (ushort)((sampleCount + 1) % wrap.Value);

        return unchecked((ushort)(sampleCount + 1));
    }

    private static bool IsForwardJump(ushort expected, ushort actual, ushort? wrap)
    {
        if (actual > expected)
            return true;

        if (wrap is > 0 && expected > wrap.Value * 0.8 && actual < wrap.Value * 0.2)
            return true;

        return false;
    }

    private static ushort? ResolveCounterWrap(SampledValueAsdu asdu, SampledValuesPublisherProfile? profile)
    {
        if (profile is not null)
            return profile.ResolveSampleCounterWrap(50);

        if (asdu.SampleMode is 1 && asdu.SampleRate is > 0)
            return asdu.SampleRate;

        return null;
    }

    private void ValidateAgainstScl(SampledValuesFrame frame, IReadOnlyList<SampledValueAsdu> asdus, SampledValuesPublisherProfile profile, ICollection<string> diagnostics)
    {
        if (!string.Equals(profile.Destination.ToString(), frame.Destination.ToString(), StringComparison.OrdinalIgnoreCase))
            diagnostics.Add($"SCL mismatch: destination MAC expected {profile.Destination}, got {frame.Destination}.");

        if (profile.Vlan?.VlanId != frame.Vlan?.VlanId)
            diagnostics.Add($"SCL mismatch: VLAN expected {profile.Vlan?.VlanId.ToString() ?? "untagged"}, got {frame.Vlan?.VlanId.ToString() ?? "untagged"}.");

        if (profile.AppId != frame.AppId)
            diagnostics.Add($"SCL mismatch: APPID expected 0x{profile.AppId:X4}, got 0x{frame.AppId:X4}.");

        if (profile.AsduPerFrame != asdus.Count)
            diagnostics.Add($"SCL mismatch: nofASDU expected {profile.AsduPerFrame}, got {asdus.Count}.");

        foreach (var asdu in asdus)
        {
            if (!string.Equals(profile.Stream.SvId, asdu.SvId, StringComparison.OrdinalIgnoreCase))
                diagnostics.Add($"SCL mismatch: svID expected {profile.Stream.SvId}, got {asdu.SvId}.");

            if (profile.Stream.ConfigurationRevision != asdu.ConfigurationRevision)
                diagnostics.Add($"SCL mismatch: confRev expected {profile.Stream.ConfigurationRevision}, got {asdu.ConfigurationRevision}.");

            if (profile.Stream.SampleRate != 0 && asdu.SampleRate.HasValue && profile.Stream.SampleRate != asdu.SampleRate.Value)
                diagnostics.Add($"SCL mismatch: sample rate expected {profile.Stream.SampleRate}, got {asdu.SampleRate.Value}.");

            if (asdu.SamplePayload.Length != profile.PayloadLayout.PayloadByteLength)
                diagnostics.Add($"Payload length mismatch: expected {profile.PayloadLayout.PayloadByteLength} byte(s), got {asdu.SamplePayload.Length}.");
        }
    }

    private static IEnumerable<DecodedValueRow> DecodePayload(SampledValueAsdu asdu, SampledValuesPayloadLayout layout, ICollection<string> diagnostics)
    {
        var decode = SampledValuesPayloadDecoder.Decode(layout, asdu.SamplePayload);
        foreach (var issue in decode.Diagnostics)
            diagnostics.Add(issue);

        foreach (var value in decode.Values)
        {
            yield return new DecodedValueRow
            {
                Index = value.Element.Index,
                Signal = value.Element.SignalReference,
                Kind = value.Element.Kind.ToString(),
                Value = MmsDataValueRenderer.ToCompactString(value.Value),
                Raw = Convert.ToHexString(value.RawBytes),
                NumericValue = ExtractNumeric(value.Value)
            };
        }
    }

    private static double? ExtractNumeric(MmsDataValue value)
        => value.Kind switch
        {
            MmsDataKind.Integer => Convert.ToDouble(value.Value, System.Globalization.CultureInfo.InvariantCulture),
            MmsDataKind.Unsigned => Convert.ToDouble(value.Value, System.Globalization.CultureInfo.InvariantCulture),
            MmsDataKind.FloatingPoint => Convert.ToDouble(value.Value, System.Globalization.CultureInfo.InvariantCulture),
            _ => null
        };

    private void IncrementPayloadIssue()
    {
        lock (_gate)
            _payloadIssues++;
    }
}
