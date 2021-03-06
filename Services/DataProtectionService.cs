﻿using System;
using System.Security.Cryptography;
using System.Text;

namespace MultiFactor.SelfService.Windows.Portal.Services
{
    /// <summary>
    /// Protect sensitive data with Windows DPAPI SDK
    /// </summary>
    public class DataProtectionService
    {
        private byte[] AdditionalEntropy => StringToBytes(Configuration.Current.MultiFactorApiSecret);

        public string Protect(string data)
        {
            if (string.IsNullOrEmpty(data)) throw new ArgumentNullException(data);

            return ToBase64(ProtectedData.Protect(StringToBytes(data), AdditionalEntropy, DataProtectionScope.LocalMachine));
        }

        public string Unprotect(string data)
        {
            if (string.IsNullOrEmpty(data)) throw new ArgumentNullException(data);
            return BytesToString(ProtectedData.Unprotect(FromBase64(data), AdditionalEntropy, DataProtectionScope.LocalMachine));
        }

        private byte[] StringToBytes(string s)
        {
            return Encoding.UTF8.GetBytes(s);
        }

        private string BytesToString(byte[] b)
        {
            return Encoding.UTF8.GetString(b);
        }

        private string ToBase64(byte[] data)
        {
            return Convert.ToBase64String(data);
        }

        private byte[] FromBase64(string text)
        {
            return Convert.FromBase64String(text);
        }
    }
}