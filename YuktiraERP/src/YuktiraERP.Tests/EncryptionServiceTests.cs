using System;
using Xunit;
using YuktiraERP.Infrastructure.Security;

namespace YuktiraERP.Tests;

public class EncryptionServiceTests
{
    private static string GenerateKey()
    {
        var key = new byte[32];
        Random.Shared.NextBytes(key);
        return Convert.ToBase64String(key);
    }

    [Fact]
    public void EncryptDecrypt_RoundTrip_ReturnsOriginalText()
    {
        var key = GenerateKey();
        var service = new AesEncryptionService(key);
        var plainText = "Hello, World! This is sensitive data.";

        var encrypted = service.Encrypt(plainText);
        var decrypted = service.Decrypt(encrypted);

        Assert.NotEqual(plainText, encrypted);
        Assert.Equal(plainText, decrypted);
    }

    [Fact]
    public void EncryptDecrypt_EmptyString_ReturnsEmpty()
    {
        var key = GenerateKey();
        var service = new AesEncryptionService(key);

        var encrypted = service.Encrypt("");
        var decrypted = service.Decrypt("");

        Assert.Equal("", encrypted);
        Assert.Equal("", decrypted);
    }

    [Fact]
    public void EncryptDecrypt_NullString_ReturnsEmpty()
    {
        var key = GenerateKey();
        var service = new AesEncryptionService(key);

        var encrypted = service.Encrypt(null!);
        var decrypted = service.Decrypt(null!);

        Assert.Equal("", encrypted);
        Assert.Equal("", decrypted);
    }

    [Fact]
    public void EncryptDecrypt_UnicodeText_RoundTrip()
    {
        var key = GenerateKey();
        var service = new AesEncryptionService(key);
        var plainText = "Unicode: हिन्दी தமிழ் 中文 🎉";

        var encrypted = service.Encrypt(plainText);
        var decrypted = service.Decrypt(encrypted);

        Assert.Equal(plainText, decrypted);
    }

    [Fact]
    public void EncryptDecrypt_LongText_RoundTrip()
    {
        var key = GenerateKey();
        var service = new AesEncryptionService(key);
        var plainText = new string('A', 10000);

        var encrypted = service.Encrypt(plainText);
        var decrypted = service.Decrypt(encrypted);

        Assert.Equal(plainText, decrypted);
    }

    [Fact]
    public void Decrypt_InvalidCiphertext_Throws()
    {
        var key = GenerateKey();
        var service = new AesEncryptionService(key);

        Assert.ThrowsAny<Exception>(() => service.Decrypt("invalid-base64-data"));
    }

    [Fact]
    public void Encrypt_DifferentCalls_ProduceDifferentCiphertext()
    {
        var key = GenerateKey();
        var service = new AesEncryptionService(key);

        var enc1 = service.Encrypt("test");
        var enc2 = service.Encrypt("test");

        Assert.NotEqual(enc1, enc2);
    }

    [Fact]
    public void Constructor_InvalidKeyLength_Throws()
    {
        var shortKey = Convert.ToBase64String(new byte[16]);

        Assert.Throws<ArgumentException>(() => new AesEncryptionService(shortKey));
    }

    [Fact]
    public void Decrypt_DifferentKey_ThrowsOrReturnsWrongData()
    {
        var key1 = GenerateKey();
        var key2 = GenerateKey();
        var service1 = new AesEncryptionService(key1);
        var service2 = new AesEncryptionService(key2);

        var encrypted = service1.Encrypt("secret");

        Assert.ThrowsAny<Exception>(() => service2.Decrypt(encrypted));
    }

    [Fact]
    public void EncryptDecrypt_NumericText_RoundTrip()
    {
        var key = GenerateKey();
        var service = new AesEncryptionService(key);
        var plainText = "12345.67";

        var encrypted = service.Encrypt(plainText);
        var decrypted = service.Decrypt(encrypted);

        Assert.Equal(plainText, decrypted);
    }

    [Fact]
    public void EncryptDecrypt_SpecialCharacters_RoundTrip()
    {
        var key = GenerateKey();
        var service = new AesEncryptionService(key);
        var plainText = "!@#$%^&*()_+-=[]{}|;':\",./<>?`~";

        var encrypted = service.Encrypt(plainText);
        var decrypted = service.Decrypt(encrypted);

        Assert.Equal(plainText, decrypted);
    }

    [Fact]
    public void EncryptDecrypt_Newlines_RoundTrip()
    {
        var key = GenerateKey();
        var service = new AesEncryptionService(key);
        var plainText = "Line 1\nLine 2\r\nLine 3";

        var encrypted = service.Encrypt(plainText);
        var decrypted = service.Decrypt(encrypted);

        Assert.Equal(plainText, decrypted);
    }
}
