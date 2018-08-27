using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

namespace Com.StartLineSoft.SincronizadorRedCoopcentral
{
    class Cripter
    {
        byte[] vKey;
        String sRsaPub = "";

        public Cripter(String llaveA, String llaveB)
        {
            this.SetKey(llaveA, llaveB);
        }

        //
        // SET SIMETRIC KEY
        //
        // sKeyA = 16 or 32 HEXA Key  Ejm : 0123456789ABCDEF
        // sKeyB = 16 or 32 HEXA Key
        // Key A + Key B = 32 or 64 bytes => hexa to bytes = 128 or 256 bits
        //

        public void SetKey(String sKeyA, String sKeyB)
        {
            vKey = new byte[sKeyA.Length + sKeyB.Length];
            vKey = HexaToByte(sKeyA + sKeyB);
        }

        // ********************************************************
        // HEXA ROUTINES
        // ********************************************************

        public static byte[] HexaToByte(String hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public String ByteToHexa(byte[] vVal)
        {
            return BitConverter.ToString(vVal).Replace("-", string.Empty);
        }

        public String StringToHexa(String sVal)
        {
            return BitConverter.ToString(Encoding.Default.GetBytes(sVal)).Replace("-", string.Empty);
        }

        // ********************************************************
        // HASH ROUTINES
        // ********************************************************

        public String GetSHA1(byte[] vInp)
        {
            byte[] vOut;
            SHA1 sha = new SHA1CryptoServiceProvider();
            vOut = sha.ComputeHash(vInp);
            return ByteToHexa(vOut);
        }

        public String GetSHA1(String sInp)
        {
            byte[] vInp;
            byte[] vOut;

            SHA1 sha = new SHA1CryptoServiceProvider();
            vInp = Encoding.Default.GetBytes(sInp);
            vOut = sha.ComputeHash(vInp);
            return ByteToHexa(vOut);
        }

        // ********************************************************
        // AES ROUTINES
        // ********************************************************

        //
        // sInp = input base64
        // sInpHas = input hash
        // bOut = State
        // sOut = output 
        //
        public void AESDecode(String sInp, String sInpHas, ref bool bOut, ref String sOut)
        {
            bOut = false;
            sOut = "";
            using (Aes cip = Aes.Create())
            {
                cip.Mode = CipherMode.CBC;
                cip.Padding = PaddingMode.Zeros;
                cip.Key = vKey;
                cip.IV = new byte[] { 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0 };
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, cip.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        byte[] inp = Convert.FromBase64String(sInp);
                        cs.Write(inp, 0, inp.Length);
                        cs.Close();
                    }

                    byte[] vOut = ms.ToArray();
                    sOut = Encoding.Default.GetString(vOut);
                    sOut = sOut.Trim('\0');
                    if (sInpHas == GetSHA1(sOut))
                    {
                        bOut = true;
                    }
                }
            }
        }

        //
        // sInp = input base64
        // sInpHas = input hash
        // bOut = State
        // vOut = output byte
        //
        public void AESDecode(String sInp, String sInpHas, ref bool bOut, ref byte[] vOut)
        {
            bOut = false;
            vOut = null;
            using (Aes cip = Aes.Create())
            {
                cip.Mode = CipherMode.CBC;
                cip.Padding = PaddingMode.Zeros;
                cip.Key = vKey;
                cip.IV = new byte[] { 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0 };
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, cip.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        byte[] inp = Convert.FromBase64String(sInp);
                        cs.Write(inp, 0, inp.Length);
                        cs.Close();
                    }

                    int nTri = 0;
                    vOut = ms.ToArray();
                    while ((nTri < 15) && (vOut[vOut.Length - nTri - 1] == 0))
                    {
                        nTri = nTri + 1;
                    }
                    if (nTri > 0)
                    {
                        Array.Resize(ref vOut, vOut.Length - nTri);
                    }

                    if (sInpHas == GetSHA1(vOut))
                    {
                        bOut = true;
                    }
                }
            }
        }

        //
        // sInp = input
        // sOut = output base64
        // sHas = output hash
        //
        public void AESEncode(String sInp, ref String sOut, ref String sHas)
        {
            sHas = "";
            sOut = "";
            using (Aes cip = Aes.Create())
            {
                cip.Mode = CipherMode.CBC;
                cip.Padding = PaddingMode.Zeros;
                cip.Key = vKey;
                cip.IV = new byte[] { 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0 };
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, cip.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        byte[] inp = Encoding.Default.GetBytes(sInp);
                        cs.Write(inp, 0, inp.Length);
                        cs.Close();
                    }
                    sHas = GetSHA1(sInp);
                    sOut = Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        //
        // vInp = input
        // sOut = output base64
        // sHas = output hash
        //
        public void AESEncode(byte[] vInp, ref String sOut, ref String sHas)
        {
            sHas = "";
            sOut = "";
            using (Aes cip = Aes.Create())
            {
                cip.Mode = CipherMode.CBC;
                cip.Padding = PaddingMode.Zeros;
                cip.Key = vKey;
                cip.IV = new byte[] { 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0 };
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, cip.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(vInp, 0, vInp.Length);
                        cs.Close();
                    }
                    sHas = GetSHA1(vInp);
                    sOut = Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        // ********************************************************
        // RSA - RUOTINES
        // ********************************************************

        public void RSASetPubKey(String sKey)
        {
            sRsaPub = sKey;
        }

        public void RSAEncode(String sInp, ref String sOut, ref String sHas)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(sRsaPub);
            byte[] vInp = System.Text.Encoding.ASCII.GetBytes(sInp);
            //byte[] vInp = Encoding.Default.GetBytes(sInp);  
            byte[] vOut = rsa.Encrypt(vInp, false);
            sHas = GetSHA1(sInp);
            sOut = Convert.ToBase64String(vOut);
        }
    }
}
