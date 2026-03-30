public class FakeAuthService
{
    public async Task<string> Login(string username, string password)
    {
        await Task.Delay(500);

        if (username == "admin" && password == "1234")
            return "Login successful";
        else
            return "Invalid credentials";
    }

    public async Task<string> Register(string username, string password)
    {
        await Task.Delay(500);
        return "Account created successfully";
    }
}
