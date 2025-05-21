using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using ICSharpCode.SharpZipLib.GZip;

namespace GnosiaSaveDecrypt;

class Program
{
    private const string Password = "Playyyism";
    private const string Salt = "Re65zxBa";
    private const int IterationCount = 1000;
    private const int KeySize = 128;
    private const int BlockSize = 128;
    private const int CompressionBufferBytes = /* 1 MB */ 1024 * 1024;


    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Please provide a file path.");
            return;
        }

        string filePath = args[0];
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            Console.WriteLine("File does not exist: " + filePath);
            return;
        }

        // Determine if we should encrypt or decrypt based on the file extension
        if (Path.GetExtension(filePath).Equals(".json", StringComparison.OrdinalIgnoreCase))
        {
            ConvertJsonToData(filePath);
        }
        else
        {
            ConvertFileToJson(filePath);
        }
    }

    private static void ConvertFileToJson(string filePath)
    {
        if (ReadFileBytes(filePath, out byte[] bytes)
            && Decrypt(bytes, out byte[] decryptedBytes)
            && Decompress(decryptedBytes, out MemoryStream outStream)
            && DeserializeLegacy(outStream, out string decryptedString)
            && PrettifyJson(decryptedString, out string prettyJson)
            )
        {
            // Ensure memory streams are disposed even if an exception occurs
            using (outStream)
            {
                string outputFilePath = Path.ChangeExtension(filePath, ".json");
                File.WriteAllText(outputFilePath, prettyJson);
                Console.WriteLine("Decrypted JSON saved to: " + outputFilePath);
            }
        }
    }

    private static void ConvertJsonToData(string filePath)
    {
        if (ReadFileText(filePath, out string text)
            && SerializeLegacy(text, out MemoryStream serializedStream)
            && Compress(serializedStream, out var compressedBytes)
            && Encrypt(compressedBytes, out byte[] encryptedBytes))
        {
            // Ensure memory streams are disposed even if an exception occurs
            using (serializedStream)
            {
                // If we are overwriting an existing .data file, ask the user if they want to back it up
                string outputFilePath = Path.ChangeExtension(filePath, ".data");
                string backupFilePath = Path.ChangeExtension(filePath, ".data.bak");
                if (File.Exists(outputFilePath) && !File.Exists(backupFilePath))
                {
                    Console.WriteLine("It looks like we are about to overwrite an existing save file. Press enter to create a backup, or enter 'S' to skip creating a backup file.");
                    string response = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(response) && (response[0] == 'S' || response[0] == 's'))
                    {
                        Console.WriteLine("Skipping backup.");
                    }
                    else
                    {
                        // Create a backup of the original .data file as .data.bak
                        File.Copy(outputFilePath, backupFilePath);
                        Console.WriteLine("Original save file backed up as: " + backupFilePath);
                    }
                }
                File.WriteAllBytes(outputFilePath, encryptedBytes);
                Console.WriteLine("Encrypted and compressed data saved to: " + outputFilePath);
            }
        }
    }

    private static bool ReadFileBytes(string filePath, out byte[] bytes)
    {
        try
        {
            bytes = File.ReadAllBytes(filePath);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error reading bytes from file: " + ex.Message);
            bytes = null;
            return false;
        }
    }

    private static bool ReadFileText(string filePath, out string text)
    {
        try
        {
            text = File.ReadAllText(filePath);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error reading text from file: " + ex.Message);
            text = null;
            return false;
        }
    }

    private static bool Decrypt(byte[] bytes, out byte[] decrypted)
    {
        try
        {
            using var aes = Aes.Create();
            aes.KeySize = KeySize;
            aes.BlockSize = BlockSize;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            var rfc2898DeriveBytes = new Rfc2898DeriveBytes(
                Password, Encoding.UTF8.GetBytes(Salt), IterationCount, HashAlgorithmName.SHA1);
            aes.Key = rfc2898DeriveBytes.GetBytes(KeySize / 8);
            aes.IV = rfc2898DeriveBytes.GetBytes(BlockSize / 8);
            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            decrypted = decryptor.TransformFinalBlock(bytes, 0, bytes.Length);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error decrypting: " + ex.Message);
            decrypted = null;
            return false;
        }
    }

    private static bool Encrypt(byte[] bytes, out byte[] encrypted)
    {
        try
        {
            using var aes = Aes.Create();
            aes.KeySize = KeySize;
            aes.BlockSize = BlockSize;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            var rfc2898DeriveBytes = new Rfc2898DeriveBytes(
                Password, Encoding.UTF8.GetBytes(Salt), IterationCount, HashAlgorithmName.SHA1);
            aes.Key = rfc2898DeriveBytes.GetBytes(KeySize / 8);
            aes.IV = rfc2898DeriveBytes.GetBytes(BlockSize / 8);
            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            encrypted = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error encrypting: " + ex.Message);
            encrypted = null;
            return false;
        }
    }

    private static bool Decompress(byte[] bytes, out MemoryStream outStream)
    {
        try
        {
            byte[] buffer = new byte[CompressionBufferBytes];
            outStream = new MemoryStream();
            using GZipInputStream gzipInputStream = new GZipInputStream(new MemoryStream(bytes));
            int bytesRead = 0;
            while ((bytesRead = gzipInputStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                outStream.Write(buffer, 0, bytesRead);
            }
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error decompressing: " + ex.Message);
            outStream = null;
            return false;
        }
    }

    private static bool Compress(MemoryStream inStream, out byte[] compressedBytes)
    {
        try
        {
            inStream.Position = 0; // Reset the stream position to the beginning
            using var outStream = new MemoryStream();
            using var gzipOutputStream = new GZipOutputStream(outStream);

            byte[] buffer = new byte[CompressionBufferBytes];
            int bytesRead;
            while ((bytesRead = inStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                gzipOutputStream.Write(buffer, 0, bytesRead);
            }
            gzipOutputStream.Finish();
            compressedBytes = outStream.ToArray();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error compressing: " + ex.Message);
            compressedBytes = null;
            return false;
        }
    }

    private static bool PrettifyJson(string json, out string prettyJson)
    {
        prettyJson = json;
        try
        {
            prettyJson = Newtonsoft.Json.JsonConvert.SerializeObject(
                Newtonsoft.Json.JsonConvert.DeserializeObject(prettyJson), Newtonsoft.Json.Formatting.Indented);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error parsing JSON: " + ex.Message);
            return true; // Return the original JSON if parsing fails
        }
    }

    private static bool SerializeLegacy(string obj, out MemoryStream outStream)
    {
        outStream = new MemoryStream();
#pragma warning disable SYSLIB0011 // Type or member is obsolete
        new BinaryFormatter().Serialize(outStream, obj); // We must use this for compatibility with the game application
#pragma warning restore SYSLIB0011 // Type or member is obsolete
        return true;
    }

    private static bool DeserializeLegacy(MemoryStream inStream, out string deserialized)
    {
        inStream.Position = 0; // Reset the stream position to the beginning
#pragma warning disable SYSLIB0011 // Type or member is obsolete
        BinaryFormatter binaryFormatter = new BinaryFormatter(); // We must use this for compatibility with the game application
#pragma warning restore SYSLIB0011 // Type or member is obsolete
        deserialized = default;
        try
        {
            deserialized = (string)Convert.ChangeType(binaryFormatter.Deserialize(inStream), typeof(string));
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error deserializing: " + ex.Message);
            return false;
        }
    }
}
