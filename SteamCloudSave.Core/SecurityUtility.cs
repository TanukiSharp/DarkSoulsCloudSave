using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace SteamCloudSave.Core;

/// <summary>
/// Provides helper methods related to security.
/// </summary>
public static class SecurityUtility
{
    private static readonly byte[] ProtectionEntropy = [0x5b, 0x50, 0x9f, 0x82, 0xf1, 0x4b, 0x4c, 0x4d, 0x92, 0xee, 0x0, 0xac, 0xb1, 0xd2, 0xee, 0x6f];

    /// <summary>
    /// Protects (encrypts) a string on Windows, returns the input string as is on other platforms.
    /// </summary>
    /// <param name="unprotectedValue">The unprotected string value to protect.</param>
    /// <returns>Returns a base64 encoded protected string, or null on error.</returns>
    public static string ProtectString(string unprotectedValue)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                var unprotectedBinaryValue = Encoding.UTF8.GetBytes(unprotectedValue);
                var protectedBinaryValue = ProtectedData.Protect(unprotectedBinaryValue, ProtectionEntropy, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(protectedBinaryValue);
            }
            catch
            {
                return null;
            }
        }

        return unprotectedValue;
    }

    /// <summary>
    /// Protects (encrypts) a byte array on Windows, encodes to base 64 on other platforms.
    /// </summary>
    /// <param name="unprotectedBinaryValue">The unprotected byte array value to protect.</param>
    /// <returns>Returns a base64 encoded protected string, or null on error.</returns>
    public static string ProtectBuffer(byte[] unprotectedBinaryValue)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                var protectedBinaryValue = ProtectedData.Protect(unprotectedBinaryValue, ProtectionEntropy, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(protectedBinaryValue);
            }
            catch
            {
                return null;
            }
        }

        return Convert.ToBase64String(unprotectedBinaryValue, Base64FormattingOptions.None);
    }

    /// <summary>
    /// Unprotects (decrypts) a string on Windows, returns the input string as is on other platforms.
    /// </summary>
    /// <param name="protectedValue">The base64 encoded protected string to unprotect.</param>
    /// <returns>Returns unprotected string value, or null on error.</returns>
    public static string UnprotectString(string protectedValue)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                var protectedBinaryValue = Convert.FromBase64String(protectedValue);
                var unprotectedBinaryValue = ProtectedData.Unprotect(protectedBinaryValue, ProtectionEntropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(unprotectedBinaryValue);
            }
            catch
            {
                return null;
            }
        }

        return protectedValue;
    }

    /// <summary>
    /// Unprotects (decrypts) a byte array on Windows, decode from base 64 on other platforms.
    /// </summary>
    /// <param name="protectedValue">The base64 encoded protected byte array to unprotect.</param>
    /// <returns>Returns unprotected byte array value, or null on error.</returns>
    public static byte[] UnprotectBuffer(string protectedValue)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                var protectedBinaryValue = Convert.FromBase64String(protectedValue);
                var unprotectedBinaryValue = ProtectedData.Unprotect(protectedBinaryValue, ProtectionEntropy, DataProtectionScope.CurrentUser);
                return unprotectedBinaryValue;
            }
            catch
            {
                return null;
            }
        }

        return Convert.FromBase64String(protectedValue);
    }
}
