using System;

namespace Game.Shared.Dtos
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Avatar { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public UserStatusDto Status { get; set; }
        public int Level { get; set; }
        public int Experience { get; set; }
    }

    public enum UserStatusDto
    {
        Online,
        Offline,
        InGame,
        Away,
        Busy
    }

    public class UserLoginDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class UserRegisterDto
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
    }

    public class UserTokenDto
    {
        public string Token { get; set; }
        public Guid UserId { get; set; }
        public string Username { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}