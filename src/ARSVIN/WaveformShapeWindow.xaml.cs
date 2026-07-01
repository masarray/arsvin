using System.Windows;
using AR.Iec61850.SvPublisher.ViewModels;

namespace AR.Iec61850.SvPublisher;

public partial class WaveformShapeWindow : Window
{
    public WaveformShapeWindow()
    {
        InitializeComponent();
    }

    private void ResetManualShape_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SvPublisherViewModel viewModel)
            return;

        foreach (var channel in viewModel.Channels)
            channel.ResetWaveformShape();
    }

    private void ResetStateShape_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SvPublisherViewModel { SelectedSequenceState: { } state })
            return;

        state.CurrentScaleA = 1;
        state.CurrentScaleB = 1;
        state.CurrentScaleC = 1;
        state.CurrentScaleN = 0;
        state.VoltageScaleA = 1;
        state.VoltageScaleB = 1;
        state.VoltageScaleC = 1;
        state.VoltageScaleN = 0;
        state.AngleOffsetA = 0;
        state.AngleOffsetB = 0;
        state.AngleOffsetC = 0;
        state.AngleOffsetN = 0;
        state.CurrentDcOffsetPercent = 0;
        state.VoltageDcOffsetPercent = 0;
        state.CurrentHarmonicPercent = 0;
        state.VoltageHarmonicPercent = 0;
        state.HarmonicOrder = 2;
        state.CurrentClipPercent = 100;
        state.VoltageClipPercent = 100;
        state.ScenarioTag = string.Empty;
    }
}
