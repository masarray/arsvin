using System.Text.Json;
using System.Windows;
using System.Windows.Data;
using AR.Iec61850.SampledValues.Reporting;
using ARSVIN.Subscriber.Controls;
using ARSVIN.Subscriber.ViewModels;
using Microsoft.Win32;

namespace ARSVIN.Subscriber;

public partial class MainWindow : Window
{
    private readonly SvSubscriberViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new SvSubscriberViewModel();
        DataContext = _viewModel;
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
        _viewModel.Dispose();
        base.OnClosed(e);
    }
}
