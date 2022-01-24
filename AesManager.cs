using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace aesPass
{
    internal class AesManager
    {
        private byte[] keyS;
        private byte[] ivS;

        public byte[] KEY
        {
            get { return keyS; }
        }

        public byte[] IV 
        {
            get { return ivS; }
        }

        public AesManager(byte[] key, byte[] iv)
        {
            keyS = key;
            ivS = iv;
        }

        public AesManager(string passwd)
        {
            EVP_BytesToKey(Encoding.ASCII.GetBytes(passwd), null, passwd.Length, out keyS, out ivS);
        }

        public AesManager(string passwd, byte[] solt)
        {
            EVP_BytesToKey(Encoding.ASCII.GetBytes(passwd), solt, passwd.Length, out keyS, out ivS);
        }

        public static void EVP_BytesToKey(byte[] data, byte[] salt, int count, out byte[] key, out byte[] iv)
        {
            List<byte> hashList = new List<byte>();
            byte[] currentHash = new byte[0];

            int preHashLength = data.Length + ((salt != null) ? salt.Length : 0);
            byte[] preHash = new byte[preHashLength];

            System.Buffer.BlockCopy(data, 0, preHash, 0, data.Length);
            if (salt != null)
                System.Buffer.BlockCopy(salt, 0, preHash, data.Length, salt.Length);

            MD5 hash = MD5.Create();
            currentHash = hash.ComputeHash(preHash);

            for (int i = 1; i < count; i++)
            {
                currentHash = hash.ComputeHash(currentHash);
            }

            hashList.AddRange(currentHash);

            while (hashList.Count < 48) // for 32-byte key and 16-byte iv
            {
                preHashLength = currentHash.Length + data.Length + ((salt != null) ? salt.Length : 0);
                preHash = new byte[preHashLength];

                System.Buffer.BlockCopy(currentHash, 0, preHash, 0, currentHash.Length);
                System.Buffer.BlockCopy(data, 0, preHash, currentHash.Length, data.Length);
                if (salt != null)
                    System.Buffer.BlockCopy(salt, 0, preHash, currentHash.Length + data.Length, salt.Length);

                currentHash = hash.ComputeHash(preHash);

                for (int i = 1; i < count; i++)
                {
                    currentHash = hash.ComputeHash(currentHash);
                }

                hashList.AddRange(currentHash);
            }
            hash.Clear();
            key = new byte[32];
            iv = new byte[16];
            hashList.CopyTo(0, key, 0, 32);
            hashList.CopyTo(32, iv, 0, 16);
        }
        
        public byte[] Encrypt(string data)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = keyS;
                aesAlg.IV = ivS;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(data);
                        }
                        return msEncrypt.ToArray();
                    }
                }
            }

        }

        public string Decrypt(byte[] data)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = keyS;
                aesAlg.IV = ivS;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                try
                {
                    using (MemoryStream msDecrypt = new MemoryStream(data))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {
                                return srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    return "error password";
                }
            }
        }

        public string EncryptBase64(string data)
        {
            return Convert.ToBase64String(Encrypt(data));
        }

        public string DecryptBase64(string data)
        {
            return Decrypt(Convert.FromBase64String(data));
        }
    }
}
