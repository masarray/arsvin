using System.Windows;
using AR.Iec61850.SampledValues.Field;

namespace ARSVIN.Subscriber;

public partial class MainWindow
{
    private void KnownInjection_Click(object sender, RoutedEventArgs e)
    {
        var selected = _viewModel.SelectedStream;
        if (selected is null)
        {
            MessageBox.Show(this, "Select an SV stream before validating a known injection.", "Known Injection", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!selected.IsAnalysisTrusted)
        {
            MessageBox.Show(
                this,
                $"Waveform and phasor analysis is not trusted yet.\n\n{selected.AnalysisTrustDetail}",
                "Known Injection",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var phasors = selected.GenericPhasors
            .Where(item => item.IsValid && !string.Equals(item.Unit, "count", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (phasors.Length == 0)
        {
            MessageBox.Show(
                this,
                "No engineering phasor is available. Import the matching SCL/CID and resolve engineering scaling first.",
                "Known Injection",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var dialog = new KnownInjectionWindow(phasors) { Owner = this };
        if (dialog.ShowDialog() != true || dialog.Expectation is null || dialog.Measurement is null || dialog.Comparison is null)
            return;

        selected.RecordKnownInjectionEvidence(dialog.Expectation, dialog.Measurement, dialog.Comparison);
        var comparison = dialog.Comparison;
        var error = comparison.AmplitudeErrorPercent.HasValue
            ? $"{comparison.AmplitudeErrorPercent.Value:+0.###;-0.###;0}%"
            : $"{comparison.AbsoluteAmplitudeError:+0.######;-0.######;0}";
        MessageBox.Show(
            this,
            $"Result: {comparison.State.ToString().ToUpperInvariant()}\nAmplitude error: {error}\n\nThe result is stored in selected-stream evidence and included in the support bundle.",
            "Known Injection",
            MessageBoxButton.OK,
            comparison.State == SvKnownInjectionResultState.Fail ? MessageBoxImage.Warning : MessageBoxImage.Information);
    }
}