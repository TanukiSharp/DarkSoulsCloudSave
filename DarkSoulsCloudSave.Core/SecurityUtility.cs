using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DarkSoulsCloudSave.Core
{
    /// <summary>
    /// Provides helper methods related to security.
    /// </summary>
    public static class SecurityUtility
    {
        private static readonly byte[] ProtectionEntropy = { 0x5b, 0x50, 0x9f, 0x82, 0xf1, 0x4b, 0x4c, 0x4d, 0x92, 0xee, 0x0, 0xac, 0xb1, 0xd2, 0xee, 0x6f };

        /// <summary>
        /// Protects (encrypts) a string.
        /// </summary>
        /// <param name="unprotectedValue">The unprotected string value to protect.</param>
        /// <param name="scope">The protection scope.</param>
        /// <returns>Returns a base64 encoded protected string, or null on error.</returns>
        /// <seealso cref="UnprotectString(string, DataProtectionScope)"/>
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

        /// <summary>
        /// Protects (encrypts) a byte array.
        /// </summary>
        /// <param name="unprotectedBinaryValue">The unprotected byte array value to protect.</param>
        /// <param name="scope">The protection scope.</param>
        /// <returns>Returns a base64 encoded protected string, or null on error.</returns>
        /// <seealso cref="UnprotectBuffer(string, DataProtectionScope)"/>
        public static string ProtectBuffer(byte[] unprotectedBinaryValue, DataProtectionScope scope)
        {
            try
            {
                var protectedBinaryValue = ProtectedData.Protect(unprotectedBinaryValue, ProtectionEntropy, scope);
                return Convert.ToBase64String(protectedBinaryValue);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Unprotects (decrypts) a string.
        /// </summary>
        /// <param name="protectedValue">The base64 encoded protected string to unprotect.</param>
        /// <param name="scope">The protection scope.</param>
        /// <returns>Returns unprotected string value, or null on error.</returns>
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

        /// <summary>
        /// Unprotects (decrypts) a byte array.
        /// </summary>
        /// <param name="protectedValue">The base64 encoded protected byte array to unprotect.</param>
        /// <param name="scope">The protection scope.</param>
        /// <returns>Returns unprotected byte array value, or null on error.</returns>
        public static byte[] UnprotectBuffer(string protectedValue, DataProtectionScope scope)
        {
            try
            {
                var protectedBinaryValue = Convert.FromBase64String(protectedValue);
                var unprotectedBinaryValue = ProtectedData.Unprotect(protectedBinaryValue, ProtectionEntropy, scope);
                return unprotectedBinaryValue;
            }
            catch
            {
                return null;
            }
        }
    }
}
