using System.Security.Cryptography;
using System.Text;

public static class SecurityHelper
{
    private static readonly string secret = "9A7f!2kL#pX91@zqQn!W3$8dS7kA0bH";

    public static string GenerateSignature(string data)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var message = Encoding.UTF8.GetBytes(data);

        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(message);

        return Convert.ToBase64String(hash);
    }
}