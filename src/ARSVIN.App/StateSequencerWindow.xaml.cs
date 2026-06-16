using System.Windows;
using ARSVIN.App.Models;
using ARSVIN.App.ViewModels;

namespace ARSVIN.App;

public partial class StateSequencerWindow : Window
{
    public StateSequencerWindow()
    {
        InitializeComponent();
    }

    private void UseSequencer_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is SvPublisherViewModel viewModel)
            viewModel.Mode = InjectionMode.Sequencer;

        DialogResult = true;
    }
}
