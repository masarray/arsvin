using System.Collections.Specialized;
using System.Text.Json;
using System.Windows;
using System.Windows.Data;
using AR.Iec61850.SampledValues.Measurements;
using AR.Iec61850.SampledValues.Reporting;
using ARSVIN.Subscriber.Controls;
using ARSVIN.Subscriber.ViewModels;
using Microsoft.Win32;

namespace ARSVIN.Subscriber;

public partial class MainWindow : Window
{
    private readonly SvSubscriberViewModel _viewModel;
    private readonly Dictionary<string, SvStreamMeasurementContext> _measurementContexts =
        new(StringComparer.Ordinal);

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new SvSubscriberViewModel();
        DataContext = _viewModel;
        _viewModel.Streams.CollectionChanged += Streams_CollectionChanged;
        AttachLivePlotControls();
    }

    private void AttachLivePlotControls()
    {
        var scope = new OscilloscopePlot();
        BindingOperations.SetBinding(
            scope,
            OscilloscopePlot.PointsProperty,
            new Binding("SelectedStream.WaveformPoints"));
        ScopeHost.Child = scope;

        var phasor = new PhasorPlot();
        BindingOperations.SetBinding(
            phasor,
            PhasorPlot.VectorsProperty,
            new Binding("SelectedStream.Phasors"));
        PhasorHost.Child = phasor;
    }

    private void Streams_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is null)
            return;

        foreach (var row in e.NewItems.OfType<SvStreamViewModel>())
        {
            if (_measurementContexts.TryGetValue(row.Key, out var context))
                row.SetMeasurementContext(context);
        }
    }

    private void MeasurementContext_Click(object sender, RoutedEventArgs e)
    {
        var selected = _viewModel.SelectedStream;
        if (selected is null)
        {
            MessageBox.Show(
                this,
                "Select an SV stream before editing CT/VT measurement context.",
                "SV Measurement Context",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        _measurementContexts.TryGetValue(selected.Key, out var existing);
        var dialog = new MeasurementContextWindow(selected.Key, selected.SvId, existing)
        {
            Owner = this
        };
        if (dialog.ShowDialog() != true)
            return;

        if (dialog.RemoveRequested)
        {
            _measurementContexts.Remove(selected.Key);
            selected.SetMeasurementContext(null);
            return;
        }

        if (dialog.ResultContext is not { } context)
            return;

        _measurementContexts[selected.Key] = context;
        selected.SetMeasurementContext(context);
    }

    private async void ImportMeasurementContext_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import ARSVIN measurement-context JSON",
            Filter = "ARSVIN measurement context (*.json)|*.json|All files (*.*)|*.*",
            Multiselect = false
        };
        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            var json = await File.ReadAllTextAsync(dialog.FileName).ConfigureAwait(true);
            var document = SvMeasurementContextSerializer.FromJson(json);
            foreach (var context in document.Streams)
                _measurementContexts[context.StreamKey] = context;

            foreach (var row in _viewModel.Streams)
            {
                if (_measurementContexts.TryGetValue(row.Key, out var context))
                    row.SetMeasurementContext(context);
            }

            MessageBox.Show(
                this,
                $"Imported {document.Streams.Count} measurement context(s). Existing matching stream keys were updated.",
                "SV Measurement Context",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            MessageBox.Show(
                this,
                $"Measurement-context import failed: {ex.Message}",
                "SV Measurement Context",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void ExportMeasurementContext_Click(object sender, RoutedEventArgs e)
    {
        if (_measurementContexts.Count == 0)
        {
            MessageBox.Show(
                this,
                "No measurement context has been configured yet.",
                "SV Measurement Context",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Export ARSVIN measurement-context JSON",
            Filter = "ARSVIN measurement context (*.json)|*.json|All files (*.*)|*.*",
            DefaultExt = ".json",
            AddExtension = true,
            FileName = $"arsvin-measurement-context-{DateTime.Now:yyyyMMdd-HHmmss}.json"
        };
        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            var document = new SvMeasurementContextDocument
            {
                ExportedAt = DateTimeOffset.UtcNow,
                Streams = _measurementContexts.Values
                    .OrderBy(item => item.SvId, StringComparer.Ordinal)
                    .ThenBy(item => item.StreamKey, StringComparer.Ordinal)
                    .ToArray()
            };
            await File.WriteAllTextAsync(
                dialog.FileName,
                SvMeasurementContextSerializer.ToJson(document)).ConfigureAwait(true);

            MessageBox.Show(
                this,
                $"Exported {_measurementContexts.Count} measurement context(s) to {Path.GetFileName(dialog.FileName)}.",
                "SV Measurement Context",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            MessageBox.Show(
                this,
                $"Measurement-context export failed: {ex.Message}",
                "SV Measurement Context",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void CompareEvidence_Click(object sender, RoutedEventArgs e)
    {
        var baselineDialog = new OpenFileDialog
        {
            Title = "Select baseline ARSVIN evidence JSON",
            Filter = "ARSVIN evidence JSON (*.json)|*.json|All files (*.*)|*.*",
            Multiselect = false
        };
        if (baselineDialog.ShowDialog(this) != true)
            return;

        var candidateDialog = new OpenFileDialog
        {
            Title = "Select candidate ARSVIN evidence JSON",
            Filter = "ARSVIN evidence JSON (*.json)|*.json|All files (*.*)|*.*",
            Multiselect = false,
            InitialDirectory = Path.GetDirectoryName(baselineDialog.FileName)
        };
        if (candidateDialog.ShowDialog(this) != true)
            return;

        var saveDialog = new SaveFileDialog
        {
            Title = "Save ARSVIN evidence comparison bundle",
            Filter = "ARSVIN comparison bundle (*.md)|*.md|Markdown comparison (*.md)|*.md|JSON comparison (*.json)|*.json",
            DefaultExt = ".md",
            AddExtension = true,
            FileName = $"arsvin-subscriber-comparison-{DateTime.Now:yyyyMMdd-HHmmss}.md"
        };
        if (saveDialog.ShowDialog(this) != true)
            return;

        try
        {
            var baselineJsonTask = File.ReadAllTextAsync(baselineDialog.FileName);
            var candidateJsonTask = File.ReadAllTextAsync(candidateDialog.FileName);
            await Task.WhenAll(baselineJsonTask, candidateJsonTask).ConfigureAwait(true);

            var baseline = SvSubscriberEvidenceReportSerializer.FromJson(await baselineJsonTask.ConfigureAwait(true));
            var candidate = SvSubscriberEvidenceReportSerializer.FromJson(await candidateJsonTask.ConfigureAwait(true));
            var comparison = new SvSubscriberEvidenceComparator().Compare(
                baseline,
                candidate,
                DateTimeOffset.Now);

            var markdownPath = Path.ChangeExtension(saveDialog.FileName, ".md");
            var jsonPath = Path.ChangeExtension(saveDialog.FileName, ".json");
            await Task.WhenAll(
                File.WriteAllTextAsync(markdownPath, SvSubscriberEvidenceComparisonSerializer.ToMarkdown(comparison)),
                File.WriteAllTextAsync(jsonPath, SvSubscriberEvidenceComparisonSerializer.ToJson(comparison))).ConfigureAwait(true);

            var status = comparison.Summary.HasRegressions
                ? $"Review required: {comparison.Summary.WarningChangeCount} warning(s), {comparison.Summary.ErrorChangeCount} error(s)."
                : "No regression detected.";
            MessageBox.Show(
                this,
                $"Evidence comparison exported:\n{Path.GetFileName(markdownPath)}\n{Path.GetFileName(jsonPath)}\n\n{status}",
                "ARSVIN Evidence Comparison",
                MessageBoxButton.OK,
                comparison.Summary.HasRegressions ? MessageBoxImage.Warning : MessageBoxImage.Information);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or
                                   ArgumentException or InvalidDataException or
                                   InvalidOperationException or JsonException)
        {
            MessageBox.Show(
                this,
                $"Evidence comparison failed: {ex.Message}",
                "ARSVIN Evidence Comparison",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Streams.CollectionChanged -= Streams_CollectionChanged;
        _viewModel.Dispose();
        base.OnClosed(e);
    }
}
