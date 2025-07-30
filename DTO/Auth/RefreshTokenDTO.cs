namespace E_learning.DTO.Auth
{
    public class RefreshTokenDTO
    {

        public string UserId { get; set; }
        public string RefreshToken { get; set; }

        public RefreshTokenDTO(string userId, string refreshToken)
        {
            UserId = userId;
            RefreshToken = refreshToken;
        }
        public RefreshTokenDTO() { }
    }
}
