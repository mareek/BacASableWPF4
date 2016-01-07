using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BacASableWPF4
{
    public static class SecurePasswordHashHelper
    {
        private const int SaltByteLength = 16;
        private const int DerivedKeyLength = 20;
        private const int minIterationCount = 44000;
        private const int maxIterationCount = 50000;

        public static string HashPassword(string password)
        {
            return CreateSecurePasswordHash(password);
        }

        public static bool VerifyHashedPassword(string hashedPassword, string providedPassword)
        {
            providedPassword = Convert.ToBase64String(ComputeHash(providedPassword));
            return ComparePasswordHashes(providedPassword, hashedPassword);
        }

        private static byte[] ComputeHash(string password)
        {
            using (MemoryStream ms = new MemoryStream())
            using (StreamWriter sw = new StreamWriter(ms))
            {
                sw.Write(password);
                sw.Flush();
                ms.Position = 0;

                using (SHA512CryptoServiceProvider provider = new SHA512CryptoServiceProvider())
                    return provider.ComputeHash(ms);
            }
        }

        private static string CreateSecurePasswordHash(string password)
        {
            byte[] hashedPassword = ComputeHash(password);
            byte[] salt = GenerateSecureSalt();
            Random rand = new Random();
            int iterationCount = rand.Next(minIterationCount, maxIterationCount);
            byte[] hashValue = GenerateSecureHashValue(hashedPassword, salt, iterationCount);
            byte[] iterationCountBtyeArr = BitConverter.GetBytes(iterationCount);
            byte[] valueToSave = new byte[SaltByteLength + DerivedKeyLength + iterationCountBtyeArr.Length];
            Buffer.BlockCopy(salt, 0, valueToSave, 0, SaltByteLength);
            Buffer.BlockCopy(hashValue, 0, valueToSave, SaltByteLength, DerivedKeyLength);
            Buffer.BlockCopy(iterationCountBtyeArr, 0, valueToSave, salt.Length + hashValue.Length, iterationCountBtyeArr.Length);
            return Convert.ToBase64String(valueToSave);
        }

        private static byte[] GenerateSecureSalt()
        {
            using (RNGCryptoServiceProvider rngCSP = new RNGCryptoServiceProvider())
            {
                byte[] salt = new byte[SaltByteLength];
                rngCSP.GetBytes(salt);
                return salt;
            }
        }

        private static byte[] GenerateSecureHashValue(byte[] password, byte[] salt, int iterationCount)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterationCount))
            {
                return pbkdf2.GetBytes(DerivedKeyLength);
            }
        }

        private static bool ComparePasswordHashes(string guess, string saved)
        {
            if (string.IsNullOrEmpty(guess) || string.IsNullOrEmpty(saved))
                return false;

            byte[] passwordGuess = Convert.FromBase64String(guess);
            byte[] savedPassword = Convert.FromBase64String(saved);
            byte[] salt = new byte[SaltByteLength];
            byte[] actualPasswordByteArr = new byte[DerivedKeyLength];
            int iterationCount = savedPassword.Length - (salt.Length + actualPasswordByteArr.Length);
            byte[] iterationCountByteArr = new byte[iterationCount];
            Buffer.BlockCopy(savedPassword, 0, salt, 0, SaltByteLength);
            Buffer.BlockCopy(savedPassword, SaltByteLength, actualPasswordByteArr, 0, actualPasswordByteArr.Length);
            Buffer.BlockCopy(savedPassword, (salt.Length + actualPasswordByteArr.Length), iterationCountByteArr, 0, iterationCount);
            byte[] passwordGuessByteArr = GenerateSecureHashValue(passwordGuess, salt, BitConverter.ToInt32(iterationCountByteArr, 0));
            return ConstantTimeComparison(passwordGuessByteArr, actualPasswordByteArr);
        }

        private static bool ConstantTimeComparison(byte[] passwordGuessHash, byte[] savedHash)
        {
            uint difference = (uint)passwordGuessHash.Length ^ (uint)savedHash.Length;

            for (var i = 0; i < passwordGuessHash.Length && i < savedHash.Length; i++)
            {
                difference |= (uint)(passwordGuessHash[i] ^ savedHash[i]);
            }

            return difference == 0;
        }
    }
}
