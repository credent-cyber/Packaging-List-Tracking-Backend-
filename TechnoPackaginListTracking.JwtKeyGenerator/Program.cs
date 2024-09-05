using System;
using System.Security.Cryptography;
using System.Text;

class Program
{
    static void Main()
    {
        var key = new byte[32]; // 256 bits
        using (var rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(key);
        }
        var keyString = Convert.ToBase64String(key);
        Console.WriteLine(keyString);
    }
}
