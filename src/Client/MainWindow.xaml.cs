public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
        MainFrame.Navigate(typeof(LoginPage));
    }
}