using System.Text;
using System.Windows;
using AR.Iec61850.SvPublisher.Models;
using AR.Iec61850.SvPublisher.ViewModels;

namespace AR.Iec61850.SvPublisher;

public partial class PreflightResultsWindow : Window
{
    public PreflightResultsWindow()
    {
        InitializeComponent();
    }

    private void CopyReport_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SvPublisherViewModel viewModel)
            return;

        var builder = new StringBuilder();
        builder.AppendLine("ARSVIN preflight results");
        builder.AppendLine(viewModel.LivePreflightSummaryText);
        builder.AppendLine($"Fatal={viewModel.LivePreflightErrorCount}; Warnings={viewModel.LivePreflightWarningCount}; Info={viewModel.LivePreflightInfoCount}");
        builder.AppendLine();

        foreach (LivePreflightDiagnostic diagnostic in viewModel.LivePreflightDiagnostics)
            builder.AppendLine(diagnostic.ToString());

        Clipboard.SetText(builder.ToString());
    }
}
