namespace SecPerf.Application.Dtos.Auth
{
    public record LoginRequest
    {
        public string Email { get; init; } = null!;
        public string Password { get; init; } = null!;
    }
}
