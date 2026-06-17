using System.Windows;
using AR.Iec61850.SvPublisher.Models;
using AR.Iec61850.SvPublisher.ViewModels;

namespace AR.Iec61850.SvPublisher;

public partial class SvConfigWindow : Window
{
    public SvConfigWindow()
    {
        InitializeComponent();
    }

    private SvPublisherViewModel? ViewModel => DataContext as SvPublisherViewModel;

    private void Check_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } viewModel)
            return;

        if (viewModel.RunPreflightCommand.CanExecute(null))
            viewModel.RunPreflightCommand.Execute(null);

        new PreflightResultsWindow
        {
            Owner = this,
            DataContext = viewModel
        }.ShowDialog();
    }

    private void SourceManual_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not { SelectedPublisherSlot: { } slot } viewModel)
            return;

        slot.SignalSource = PublisherSignalSource.Manual;
        viewModel.Mode = InjectionMode.Manual;
    }

    private void SourceRamp_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not { SelectedPublisherSlot: { } slot } viewModel)
            return;

        slot.SignalSource = PublisherSignalSource.Manual;
        viewModel.Mode = InjectionMode.Ramp;
    }

    private void SourceSequence_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not { SelectedPublisherSlot: { } slot } viewModel)
            return;

        slot.SignalSource = PublisherSignalSource.Manual;
        viewModel.Mode = InjectionMode.Sequencer;
    }

    private void SourceComtrade_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not { SelectedPublisherSlot: { } slot } viewModel)
            return;

        slot.SignalSource = PublisherSignalSource.ComtradeReplay;
        viewModel.Mode = InjectionMode.Manual;
    }
}
