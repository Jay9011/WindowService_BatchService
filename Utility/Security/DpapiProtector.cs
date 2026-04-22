using System;
using System.Security.Cryptography;
using System.Text;

namespace Utility.Security
{
    /// <summary>
    /// Windows DPAPI (LocalMachine scope) protector.
    /// Used to protect secrets (DB password) stored in appsettings.json.
    /// LocalMachine scope lets both LocalSystem (Worker) and interactive users (SettingsUI)
    /// decrypt the same ciphertext on the same machine.
    /// </summary>
    public static class DpapiProtector
    {
        /// <summary>
        /// Prefix marking an already-protected value.
        /// Plain values without this prefix are returned as-is by <see cref="Decrypt"/>
        /// to support migration from unencrypted settings files.
        /// </summary>
        private const string EncryptedPrefix = "ENC:";

        /// <summary>
        /// Application-specific entropy. Combined with the DPAPI master key.
        /// Changing this value will invalidate previously protected secrets.
        /// </summary>
        private static readonly byte[] Entropy = new byte[]
        {
            0x53, 0x45, 0x43, 0x55, 0x69, 0x44, 0x45, 0x41,
            0x42, 0x61, 0x74, 0x63, 0x68, 0x53, 0x76, 0x63
        };

        /// <summary>
        /// Encrypts a plaintext string and returns "ENC:" prefixed base64.
        /// Empty or null input returns an empty string unchanged.
        /// </summary>
        public static string Encrypt(string plaintext)
        {
            if (string.IsNullOrEmpty(plaintext))
            {
                return string.Empty;
            }

            if (IsEncrypted(plaintext))
            {
                return plaintext;
            }

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(plaintext);
                byte[] encrypted = ProtectedData.Protect(data, Entropy, DataProtectionScope.LocalMachine);
                return EncryptedPrefix + Convert.ToBase64String(encrypted);
            }
            catch (Exception ex)
            {
                throw new CryptographicException(Resources.Strings.ErrorEncryption, ex);
            }
        }

        /// <summary>
        /// Decrypts a protected string.
        /// Values without the "ENC:" prefix are returned as-is (supports unencrypted migration).
        /// </summary>
        public static string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
            {
                return string.Empty;
            }

            if (!IsEncrypted(encryptedText))
            {
                return encryptedText;
            }

            try
            {
                string base64 = encryptedText.Substring(EncryptedPrefix.Length);
                byte[] encrypted = Convert.FromBase64String(base64);
                byte[] data = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.LocalMachine);
                return Encoding.UTF8.GetString(data);
            }
            catch (Exception ex)
            {
                throw new CryptographicException(Resources.Strings.ErrorDecryption, ex);
            }
        }

        /// <summary>
        /// Whether the given string looks like a DPAPI-protected value.
        /// </summary>
        public static bool IsEncrypted(string value)
        {
            return !string.IsNullOrEmpty(value) && value.StartsWith(EncryptedPrefix, StringComparison.Ordinal);
        }
    }
}
