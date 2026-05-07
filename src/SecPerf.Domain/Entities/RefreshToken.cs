using System;

namespace SecPerf.Domain.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; set; }
        public string Token { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool Revoked { get; set; }

        // FK
        public Guid UserId { get; set; }
        public User? User { get; set; }

        // Domain logic
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsActive => !Revoked && !IsExpired;

        public void Revoke()
        {
            Revoked = true;
        }

        public void Extend(TimeSpan additional)
        {
            if (additional <= TimeSpan.Zero) return;
            ExpiresAt = ExpiresAt.Add(additional);
        }
    }
}
