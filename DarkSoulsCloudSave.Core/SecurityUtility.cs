using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DarkSoulsCloudSave.Core
{
    public static class SecurityUtility
    {
        private static readonly byte[] ProtectionEntropy = { 0x5b, 0x50, 0x9f, 0x82, 0xf1, 0x4b, 0x4c, 0x4d, 0x92, 0xee, 0x0, 0xac, 0xb1, 0xd2, 0xee, 0x6f };

        public static string ProtectString(string unprotectedValue, DataProtectionScope scope)
        {
            try
            {
                var unprotectedBinaryValue = Encoding.UTF8.GetBytes(unprotectedValue);
                var protectedBinaryValue = ProtectedData.Protect(unprotectedBinaryValue, ProtectionEntropy, scope);
                return Convert.ToBase64String(protectedBinaryValue);
            }
            catch
            {
                return null;
            }
        }

        public static string UnprotectString(string protectedValue, DataProtectionScope scope)
        {
            try
            {
                var protectedBinaryValue = Convert.FromBase64String(protectedValue);
                var unprotectedBinaryValue = ProtectedData.Unprotect(protectedBinaryValue, ProtectionEntropy, scope);
                return Encoding.UTF8.GetString(unprotectedBinaryValue);
            }
            catch
            {
                return null;
            }
        }
    }
}
