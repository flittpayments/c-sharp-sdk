using System;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace FlittSDK.Utils
{
#pragma warning disable CS0618 // Null-key overloads retain the legacy Config fallback.
    public static class Signature
    {
        /// <summary>
        /// Generate the signature version 2
        /// </summary>
        [Obsolete("This overload reads legacy static credentials. Use the overload with secretKey.")]
        public static string GetRequestSignatureV2(string data, bool credit = false)
        {
            return GetRequestSignatureV2(
                data,
                credit,
                LegacyConfigClientFactory.GetSecretKey(credit)
            );
        }

        public static string GetRequestSignatureV2(string data, bool credit, string secretKey)
        {
            if (secretKey == null)
            {
                throw new ArgumentNullException(nameof(secretKey));
            }

            string signature = secretKey + "|" + data;
            return GetSha1(signature).ToLower();
        }
        /// <summary>
        /// Generate the signature version 1
        /// </summary>
        [Obsolete("This overload reads legacy static credentials. Use the overload with secretKey.")]
        public static string GetRequestSignature(IEnumerable<string> hashKeys, bool credit = false)
        {
            return GetRequestSignature(
                hashKeys,
                credit,
                LegacyConfigClientFactory.GetSecretKey(credit)
            );
        }

        public static string GetRequestSignature(
            IEnumerable<string> hashKeys,
            bool credit,
            string secretKey
        )
        {
            if (secretKey == null)
            {
                throw new ArgumentNullException(nameof(secretKey));
            }

            string signature = string.Join("|", hashKeys);
            signature = secretKey + "|" + signature;
            return GetSha1(signature).ToLower();
        }

        /// <summary>
        /// Generate the SHA-512 signature used by the separate Company Reports API.
        /// </summary>
        public static string GetReportsSignature(string key, string applicationId, string date)
        {
            return GetSha512(string.Join("|", key, applicationId, date)).ToLowerInvariant();
        }

        /// <summary>
        /// Generate Sha1
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string GetSha1(string value)
        {
            var data = Encoding.UTF8.GetBytes(value);
            using (var sha1 = SHA1.Create())
            {
                return BytesToHex(sha1.ComputeHash(data));
            }
        }

        private static string GetSha512(string value)
        {
            var data = Encoding.UTF8.GetBytes(value);
            using (var sha512 = SHA512.Create())
            {
                return BytesToHex(sha512.ComputeHash(data));
            }
        }

        private static string BytesToHex(byte[] bytes)
        {
            var result = new StringBuilder(bytes.Length * 2);
            foreach (var value in bytes)
            {
                result.Append(value.ToString("x2"));
            }

            return result.ToString();
        }
        /// <summary>
        /// Encode base64 String
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string Base64Encode(string data) {
            var plainTextBytes = Encoding.UTF8.GetBytes(data);
            return Convert.ToBase64String(plainTextBytes);
        }
        
        /// <summary>
        /// Decode base64 String
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string Base64Decode(string data) {
            byte[] decoded = Convert.FromBase64String(data);
            return Encoding.UTF8.GetString(decoded);
        }

        /// <summary>
        /// Compare signatures without data-dependent early exit.
        /// </summary>
        public static bool ConstantTimeEquals(string left, string right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            byte[] leftBytes = Encoding.UTF8.GetBytes(left);
            byte[] rightBytes = Encoding.UTF8.GetBytes(right);
            int difference = leftBytes.Length ^ rightBytes.Length;
            int length = Math.Max(leftBytes.Length, rightBytes.Length);
            for (int index = 0; index < length; index++)
            {
                byte leftByte = index < leftBytes.Length ? leftBytes[index] : (byte) 0;
                byte rightByte = index < rightBytes.Length ? rightBytes[index] : (byte) 0;
                difference |= leftByte ^ rightByte;
            }

            return difference == 0;
        }
    }
#pragma warning restore CS0618
}
