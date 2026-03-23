using Api.Domain.Entities;
using Api.Domain.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Api.Application.Services
{
    public interface IJwtService
    {
        string GenerateToken(User user, IEnumerable<string> roles);
        (string Token, string TokenHash) GenerateRefreshToken();
        string HashRefreshToken(string refreshToken);
    }

    public class JwtService : IJwtService
    {
        private readonly JwtSettings _jwtSettings;

        public JwtService(IOptions<JwtSettings> jwtSettings)
        {
            _jwtSettings = jwtSettings.Value;
        }

        public string GenerateToken(User user, IEnumerable<string> roles)
        {
            // Tạo danh sách claim cho người dùng và quyền để nhúng vào JWT.
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Thêm roles vào claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Ký token bằng khóa bí mật và thuật toán HMAC SHA256.
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));

            // SigningCredentials = thông tin chứng thực ký
            // SigningCredentials (biến credentials) dùng để chỉ định “cách ký” JWT khi tạo token
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Tạo JWT với issuer, audience, claims và thời gian hết hạn.
            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryInMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public (string Token, string TokenHash) GenerateRefreshToken()
        {
            // Sinh refresh token ngẫu nhiên và trả về kèm hash để lưu trữ an toàn.
            var tokenBytes = RandomNumberGenerator.GetBytes(64);
            var token = Convert.ToBase64String(tokenBytes);
            var tokenHash = HashRefreshToken(token);

            return (token, tokenHash);
        }

        public string HashRefreshToken(string refreshToken)
        {
            // Băm refresh token để tránh lưu trữ giá trị gốc.
            var bytes = Encoding.UTF8.GetBytes(refreshToken);
            var hashBytes = SHA256.HashData(bytes);
            return Convert.ToHexString(hashBytes);
        }
    }
}