using System.Windows;
using System.Windows.Data;
using ARSVIN.Subscriber.Controls;
using ARSVIN.Subscriber.ViewModels;

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

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Dispose();
        base.OnClosed(e);
    }
}
