using System;
using System.Security.Cryptography;

namespace Device.Consumer.KraftShared.Extensions
{
    public static class HashExtension
    {
        public static string CalculateMd5Hash(this byte[] input)
        {
            using MD5 mD = MD5.Create();
            byte[] array = mD.ComputeHash(input);
            return BitConverter.ToString(array).Replace("-", string.Empty);
        }
    }
}