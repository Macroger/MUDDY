using MuddyClient.Services;

public sealed partial class RegisterPage : Page
{
    private FakeAuthService authService = new FakeAuthService();

    public RegisterPage()
    {
        this.InitializeComponent();
    }

    private async void Register_Click(object sender, RoutedEventArgs e)
    {
        if (txtPassword.Password != txtConfirm.Password)
        {
            lblStatus.Text = "Passwords do not match";
            return;
        }

        lblStatus.Text = "Creating account...";

        string result = await authService.Register(
            txtUsername.Text,
            txtPassword.Password
        );

        lblStatus.Text = result;
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        Frame.GoBack();
    }
}