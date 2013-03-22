using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace BacASableWPF4
{
    public class EncryptPulseMachin
    {
        #region Private members

        /// <summary>
        /// Crypto service
        /// </summary>
        private static readonly TripleDESCryptoServiceProvider s_clientDESCryptoServiceProvider = new TripleDESCryptoServiceProvider();

        #endregion

        #region Methodes

        /// <summary>
        /// Encrypte une chaine de texte
        /// </summary>
        /// <param name="dataToEncrypt">dataToEncrypt en clair</param>
        /// <returns>dataToEncrypt cryptée</returns>
        /// <remarks></remarks>
        public string Encrypt(string dataToEncrypt)
        {
            ICryptoTransform AesDecrypt = s_clientDESCryptoServiceProvider.CreateEncryptor();

            // Buffer temp
            byte[] bufferIn;

            // dataToEncrypt buffer to store final result
            string dataOut = string.Empty;

            // buffer in contain non crypted data
            bufferIn = Encoding.UTF8.GetBytes(dataToEncrypt.ToCharArray());

            // working workflow
            MemoryStream memoryOut = new MemoryStream();

            // Modification workflow
            CryptoStream cryptostream = new CryptoStream(memoryOut, AesDecrypt, CryptoStreamMode.Write);

            // Pipe uncrypted data in crypted stream
            cryptostream.Write(bufferIn, 0, bufferIn.Length);
            cryptostream.FlushFinalBlock();

            // Get result
            dataOut = Convert.ToBase64String(memoryOut.ToArray());

            // close streams
            memoryOut.Close();
            cryptostream.Close();

            return dataOut;

        }

        /// <summary>
        /// Encrypte un stream
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public MemoryStream Encrypt(Stream stream)
        {
            string dataToEncrypt = this.StreamToString(stream);
            return new MemoryStream(Encoding.UTF8.GetBytes(this.Encrypt(dataToEncrypt)));
        }


        /// <summary>
        /// Decrypte une chaine de texte
        /// </summary>
        /// <param name="data">dataToEncrypt cryptée</param>
        /// <returns>dataToEncrypt décryptée</returns>
        /// <remarks></remarks>
        public string Decrypt(string data)
        {
            ICryptoTransform AesDecrypt = s_clientDESCryptoServiceProvider.CreateDecryptor();
            MemoryStream dataStream = new MemoryStream();
            // Buffers
            byte[] bufferIN;
            byte[] bufferOUT;

            // convert crypted data to get the buffer in
            bufferIN = Convert.FromBase64String(data);

            // Decrypter
            CryptoStream cryptostream = new CryptoStream(dataStream, AesDecrypt, CryptoStreamMode.Write);
            cryptostream.Write(bufferIN, 0, bufferIN.Length);
            cryptostream.FlushFinalBlock();

            bufferOUT = new byte[((int)dataStream.Length) + 1];

            dataStream.Position = 0;
            dataStream.Read(bufferOUT, 0, bufferOUT.Length);

            // Close streams
            cryptostream.Close();
            dataStream.Close();
            // GetResults
            return Encoding.UTF8.GetString(bufferOUT, 0, bufferOUT.Length);

        }

        /// <summary>
        /// Decripte un stream
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public MemoryStream Decrypt(Stream stream)
        {
            string dataToDecript = StreamToString(stream);
            return new MemoryStream(Encoding.UTF8.GetBytes(this.Decrypt(dataToDecript)));
        }

        private string StreamToString(Stream stream)
        {
            using (StreamReader sReader = new StreamReader(stream))
            {
                return sReader.ReadToEnd();
            }
        }

        #endregion

        #region Constructor + Singleton

        /// <summary>
        /// On new instance, initializes the keys
        /// </summary>
        private EncryptPulseMachin()
        {
            EncryptPulseMachin.s_clientDESCryptoServiceProvider.Key = new Byte[] { 107, 222, 121, 81, 172, 21, 185, 152, 228, 37, 72, 132, 123, 112, 131, 64 };
            EncryptPulseMachin.s_clientDESCryptoServiceProvider.IV = new Byte[] { 172, 223, 13, 41, 128, 107, 81, 211 };
        }

        private static readonly EncryptPulseMachin s_instance = new EncryptPulseMachin();

        /// <summary>
        /// Get current instance (Singleton)
        /// </summary>
        /// <returns>Encrypter / Decrypter instance</returns>
        public static EncryptPulseMachin GetInstance()
        {
            return s_instance;
        }


Cordialement,

 
Julien Pomez
R&D Software Engineer
Phone: (33) 3 85 29 33 23
Fax: (33) 3 85 29 38 73
Knowledge to Shape Your Future
      



    }
}
