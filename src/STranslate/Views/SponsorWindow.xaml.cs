using System.Windows;

namespace STranslate.Views;

public partial class SponsorWindow : Window
{
    public SponsorWindow()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
