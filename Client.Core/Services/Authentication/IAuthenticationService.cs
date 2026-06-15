using Shared.Identity;

namespace Client.Core.Services.Authentication
{
    public interface IAuthenticationService: IDisposable
    {
        SessionId SessionId { get; }
        bool IsAuthenticated { get; }
    }
}
