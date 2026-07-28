using System.Buffers.Binary;
using System.Collections.Specialized;
using System.ComponentModel;
using AR.Iec61850.SampledValues.Measurements;
using ARSVIN.Subscriber.Models;

namespace ARSVIN.Subscriber.ViewModels;

public sealed partial class SvStreamViewModel
{
    private readonly BulkObservableCollection<DecodedValueRow> _genericValues = new();
    private readonly BulkObservableCollection<WaveformPoint> _genericWaveformPoints = new();
    private readonly BulkObservableCollection<PhasorVector> _genericPhasors = new();
    private string _genericMappingState = "Raw seqOfData";
    private string _genericSemanticState = "Unresolved · no assumptions";
    private string _genericWaveformState = "Waiting for stream data";

    public SvStreamViewModel()
    {
        _values.CollectionChanged += SourceCollectionChanged;
        _waveformPoints.CollectionChanged += SourceCollectionChanged;
        _phasors.CollectionChanged += SourceCollectionChanged;
        PropertyChanged += StreamPropertyChanged;
        RefreshGenericPresentation();
        InitializeFieldMode();
    }

    public IReadOnlyList<DecodedValueRow> GenericValues => _genericValues;
    public IReadOnlyList<WaveformPoint> GenericWaveformPoints => _genericWaveformPoints;
    public IReadOnlyList<PhasorVector> GenericPhasors => _genericPhasors;

    public string GenericMappingState
    {
        get => _genericMappingState;
        private set => SetProperty(ref _genericMappingState, value);
    }

    public string GenericSemanticState
    {
        get => _genericSemanticState;
        private set => SetProperty(ref _genericSemanticState, value);
    }

    public string GenericWaveformState
    {
        get => _genericWaveformState;
        private set => SetProperty(ref _genericWaveformState, value);
    }

    private void SourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => RefreshGenericPresentation();

    private void StreamPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Bound) or nameof(WaveformState) or nameof(Scaling))
            RefreshGenericPresentation();
    }

    private void RefreshGenericPresentation()
    {
        if (HasSclSemanticMapping())
        {
            GenericMappingState = "SCL dataset mapping";
            GenericSemanticState = "Resolved from ordered SCL elements";
            GenericWaveformState = WaveformState;
            _genericValues.ReplaceAll(_values);
            _genericWaveformPoints.ReplaceAll(_waveformPoints);
            _genericPhasors.ReplaceAll(_phasors);
            return;
        }

        GenericMappingState = "Raw seqOfData";
        GenericSemanticState = "Unresolved · words shown without channel, unit, or quality claims";
        GenericWaveformState = _values.Count == 0
            ? "Waiting for seqOfData"
            : "Raw words available · import SCL before semantic waveform and phasor analysis";
        _genericValues.ReplaceAll(BuildGenericRows(_values));
        _genericWaveformPoints.ReplaceAll(Array.Empty<WaveformPoint>());
        _genericPhasors.ReplaceAll(Array.Empty<PhasorVector>());
    }

    private bool HasSclSemanticMapping()
        => Bound.StartsWith("SCL:", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<DecodedValueRow> BuildGenericRows(IReadOnlyList<DecodedValueRow> source)
    {
        if (source.Count == 0)
            return Array.Empty<DecodedValueRow>();

        var rows = new List<DecodedValueRow>(source.Count);
        for (var index = 0; index < source.Count; index++)
        {
            var original = source[index];
            var byteOffset = index * 4;
            if (!TryReadWord(original.Raw, out var signed, out var unsigned))
            {
                rows.Add(new DecodedValueRow
                {
                    Index = index + 1,
                    Signal = $"Bytes {index + 1} (+0x{byteOffset:X2})",
                    Kind = "Raw bytes",
                    Value = original.Raw,
                    Raw = original.Raw,
                    ScalingSource = SvEngineeringScaleSource.RawOnly,
                    ScalingConfidence = SvEngineeringScaleConfidence.Unknown,
                    ScalingReason = "No SCL mapping is bound; bytes are preserved without semantic interpretation."
                });
                continue;
            }

            rows.Add(new DecodedValueRow
            {
                Index = index + 1,
                Signal = $"Word {index + 1} (+0x{byteOffset:X2})",
                Kind = index % 2 == 0 ? "INT32 / UINT32 · group word 1" : "INT32 / UINT32 · group word 2",
                Value = $"{signed} / {unsigned}",
                Raw = original.Raw,
                NumericValue = signed,
                ScalingSource = SvEngineeringScaleSource.RawOnly,
                ScalingConfidence = SvEngineeringScaleConfidence.Unknown,
                ScalingReason = "Generic 32-bit representation only. Channel, quality, unit, and scaling semantics are unresolved until SCL or explicit reviewed mapping is available."
            });
        }

        return rows;
    }

    private static bool TryReadWord(string rawHex, out int signed, out uint unsigned)
    {
        signed = 0;
        unsigned = 0;
        if (string.IsNullOrWhiteSpace(rawHex) || rawHex.Length != 8)
            return false;

        try
        {
            var bytes = Convert.FromHexString(rawHex);
            unsigned = BinaryPrimitives.ReadUInt32BigEndian(bytes);
            signed = unchecked((int)unsigned);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
