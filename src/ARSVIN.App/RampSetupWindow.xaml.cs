using System.Windows;
using ARSVIN.App.Models;
using ARSVIN.App.ViewModels;

namespace ARSVIN.App;

public partial class RampSetupWindow : Window
{
    public RampSetupWindow()
    {
        InitializeComponent();
    }

    private void UseRamp_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is SvPublisherViewModel viewModel)
            viewModel.Mode = InjectionMode.Ramp;

        DialogResult = true;
    }
}
