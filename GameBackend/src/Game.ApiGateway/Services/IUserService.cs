using Game.Shared.Dtos;

namespace Game.ApiGateway.Services
{
    public interface IUserService
    {
        Task<UserDto> GetUserByIdAsync(Guid userId);
        Task<UserDto> GetUserByUsernameAsync(string username);
        Task<UserDto> CreateUserAsync(UserRegisterDto userDto);
        Task<UserTokenDto> LoginAsync(UserLoginDto loginDto);
        Task<bool> ValidateTokenAsync(string token);
    }
}