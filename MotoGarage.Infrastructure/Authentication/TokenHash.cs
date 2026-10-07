using System.Security.Cryptography;
using System.Text;

namespace MotoGarage.Infrastructure.Authentication;

public static class TokenHash
{
    public static string Compute(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
