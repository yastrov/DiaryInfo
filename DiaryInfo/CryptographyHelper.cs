namespace DiaryInfo
{
    using System.Security.Cryptography;
    using System.IO;
    using System.Runtime.Serialization;

    /// <summary>
    /// Cryptography helper for this (low security) application.
    /// </summary>
    public static class CryptographyHelper
    {
        private static byte[] key = { 0xA1, 0x07, 0xB3, 0x02, 0x6F, 0xE1, 0x27, 0x08, 0xE3, 0x1A, 0x11, 0x6A, 0x77, 0x4C, 0x62, 0xF6 };
        private static byte[] iv = { 0xE1, 0x07, 0x53, 0x01, 0xF9, 0x03, 0xF7, 0x88, 0x09, 0xB0, 0x11, 0x1A, 0x13, 0x14, 0x15, 0x16 };

        private static RijndaelManaged rijndael = new RijndaelManaged();

        /// <summary>
        /// Encrypt Stream with Rijndael algorithm.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="obj"></param>
        /// <param name="formatter"></param>
        public static void EncryptStream(Stream stream, object obj, IFormatter formatter)
        {
            var cryptoStream = new CryptoStream(stream,
                                                rijndael.CreateEncryptor(key, iv),
                                                CryptoStreamMode.Write);
            formatter.Serialize(cryptoStream, obj);
            cryptoStream.FlushFinalBlock();
        }

        /// <summary>
        /// Decrypt Stream with Rijndael algorithm.
        /// </summary>
        /// <typeparam name="T">Type for result object.</typeparam>
        /// <param name="stream"></param>
        /// <param name="formatter"></param>
        /// <returns></returns>
        public static T DecryptStream<T>(Stream stream, IFormatter formatter)
        {
            var cryptoStream = new CryptoStream(stream,
                                                rijndael.CreateDecryptor(key, iv),
                                                CryptoStreamMode.Read);
            return (T)formatter.Deserialize(cryptoStream);
        }
    }
}
