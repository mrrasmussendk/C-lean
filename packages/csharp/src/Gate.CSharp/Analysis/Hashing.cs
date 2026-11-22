using System;
using System.Security.Cryptography;
using System.Text;

namespace Gate.CSharp.Analysis;

internal static class Hashing
{
    public static string Sha256(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
}
