using System;
using System.IO;
using System.Security.Cryptography;

namespace LicensingClient
{
    public static class AntiTamper
    {
        private static readonly string ExpectedHash = "4ABC340D140D842E6117F47A7D065BCC01AA83F2ADCF1F4A28AD8B1AB9BDCC59";

        public static void Check()
        {
            try
            {
                var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;

                using var sha = SHA256.Create();
                using var stream = File.OpenRead(exePath);

                var hash = sha.ComputeHash(stream);

                var currentHash = Convert.ToHexString(hash);

                if (!currentHash.Equals(ExpectedHash, StringComparison.OrdinalIgnoreCase))
                {
                    Environment.FailFast("Tampering detected");
                }
            }
            catch
            {
                Environment.FailFast("Security violation");
            }
        }
    }
}