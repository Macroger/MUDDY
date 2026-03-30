using MuddyClient.Services;

public sealed partial class LoginPage : Page
{
    private FakeAuthService authService = new FakeAuthService();

    public LoginPage()
    {
        this.InitializeComponent();
    }

    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        lblStatus.Text = "Logging in...";

        string result = await authService.Login(
            txtUsername.Text,
            txtPassword.Password
        );

        lblStatus.Text = result;
    }

    private void CreateAccount_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(RegisterPage));
    }
}