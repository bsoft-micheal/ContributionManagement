using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TeamContributionManagementSystem.Application.Common;

/// <summary>
/// Centralized common utility methods for cryptography, serialization, formatting, trimming, and validation.
/// </summary>
public static class CommonMethods
{
    private static readonly string EncryptionKey = "1a2b3c4d5e6f7a8B9c0d1E2f3a4b5c6D"; // 32 bytes for AES-256
    private static readonly string EncryptionIv = "5F4D3c2b1a0X9d8c"; // 16 bytes for AES-128
    private static readonly Random RandomGenerator = new();
    private static readonly TimeZoneInfo IstZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

    /// <summary>
    /// Serializes an object to JSON.
    /// </summary>
    public static string SerializeObject<T>(T objectValue)
    {
        return JsonSerializer.Serialize(objectValue);
    }

    /// <summary>
    /// Deserializes a JSON string to the specified type.
    /// </summary>
    public static T? DeserializeObject<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(json);
    }

    /// <summary>
    /// Encodes a Base64 string to a URL-safe Base64Url string.
    /// </summary>
    public static string ToBase64Url(string base64)
    {
        return base64.Replace('+', '-').Replace('/', '_').Replace("=", "");
    }

    /// <summary>
    /// Decodes a URL-safe Base64Url string back to Base64.
    /// </summary>
    public static string FromBase64Url(string base64Url)
    {
        base64Url = base64Url.Replace('-', '+').Replace('_', '/');
        return string.Concat(base64Url, "==".AsSpan(0, (4 - (base64Url.Length % 4)) % 4));
    }

    /// <summary>
    /// Encrypts plain text using AES-256.
    /// </summary>
    public static string EncryptValue(string plainText)
    {
        using Aes aesAlg = Aes.Create();
        aesAlg.Key = Encoding.UTF8.GetBytes(EncryptionKey);
        aesAlg.IV = Encoding.UTF8.GetBytes(EncryptionIv);

        ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
        using MemoryStream msEncrypt = new();
        using (CryptoStream csEncrypt = new(msEncrypt, encryptor, CryptoStreamMode.Write))
        using (StreamWriter swEncrypt = new(csEncrypt))
        {
            swEncrypt.Write(plainText);
        }

        string base64 = Convert.ToBase64String(msEncrypt.ToArray());
        return ToBase64Url(base64);
    }

    /// <summary>
    /// Decrypts AES-256 encrypted ciphertext.
    /// </summary>
    public static string DecryptValue(string base64UrlCipherText)
    {
        if (string.IsNullOrWhiteSpace(base64UrlCipherText))
        {
            return string.Empty;
        }

        try
        {
            string base64CipherText = FromBase64Url(base64UrlCipherText);

            using Aes aesAlg = Aes.Create();
            aesAlg.Key = Encoding.UTF8.GetBytes(EncryptionKey);
            aesAlg.IV = Encoding.UTF8.GetBytes(EncryptionIv);

            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
            using MemoryStream msDecrypt = new(Convert.FromBase64String(base64CipherText));
            using CryptoStream csDecrypt = new(msDecrypt, decryptor, CryptoStreamMode.Read);
            using StreamReader srDecrypt = new(csDecrypt);
            return srDecrypt.ReadToEnd();
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Generates a cryptographically secure random numeric OTP.
    /// </summary>
    public static string GenerateOTP(int digits = 6)
    {
        if (digits <= 0)
        {
            throw new ArgumentException("The number of digits must be greater than 0.", nameof(digits));
        }

        var randomNumber = new byte[4];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        var otp = Math.Abs(BitConverter.ToInt32(randomNumber, 0));
        var maxValue = (int)Math.Pow(10, digits);
        otp %= maxValue;
        return otp.ToString($"D{digits}");
    }

    /// <summary>
    /// Generates a unique timestamped file name for uploads.
    /// </summary>
    public static string GenerateUniqueFileName(string fileExtension)
    {
        var uniqueFileName = Guid.NewGuid().ToString("N");
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        var ext = fileExtension.StartsWith('.') ? fileExtension : $".{fileExtension}";
        return $"{uniqueFileName}_{timestamp}{ext}";
    }

    /// <summary>
    /// Generates a strong random temporary password.
    /// </summary>
    public static string GetRandomPassword(int length = 12)
    {
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*()-_=+";

        string allChars = upper + lower + digits + special;
        var password = new StringBuilder();

        password.Append(upper[RandomGenerator.Next(upper.Length)]);
        password.Append(lower[RandomGenerator.Next(lower.Length)]);
        password.Append(digits[RandomGenerator.Next(digits.Length)]);
        password.Append(special[RandomGenerator.Next(special.Length)]);

        for (int i = password.Length; i < length; i++)
        {
            password.Append(allChars[RandomGenerator.Next(allChars.Length)]);
        }

        return new string(password.ToString().OrderBy(_ => RandomGenerator.Next()).ToArray());
    }

    /// <summary>
    /// Calculates precise age from date of birth.
    /// </summary>
    public static int CalculateAge(DateTime birthDate)
    {
        DateTime today = DateTime.UtcNow.Date;
        int age = today.Year - birthDate.Year;
        if (today.Month < birthDate.Month || (today.Month == birthDate.Month && today.Day < birthDate.Day))
        {
            age--;
        }
        return Math.Max(0, age);
    }

    /// <summary>
    /// Converts a birth date to human-readable age string (e.g. "25 Years 4 Months").
    /// </summary>
    public static string ConvertDateTimeToAge(DateTime? birthDate)
    {
        if (!birthDate.HasValue)
        {
            return string.Empty;
        }

        DateTime today = DateTime.UtcNow.Date;
        DateTime birth = birthDate.Value.Date;

        int years = today.Year - birth.Year;
        int months = today.Month - birth.Month;
        int days = today.Day - birth.Day;

        if (months < 0 || (months == 0 && days < 0))
        {
            years--;
            months = (months + 12) % 12;
            days += DateTime.DaysInMonth(today.Year, today.Month);
        }

        return $"{years}Y {Math.Abs(months)}M {Math.Abs(days)}D";
    }

    /// <summary>
    /// Combines date and optional time in IST and converts to UTC DateTime.
    /// </summary>
    public static DateTime? CombineToUtc(DateTime? date, TimeOnly? time = null, bool isEndOfDay = false)
    {
        if (!date.HasValue)
        {
            return null;
        }

        DateTime dateOnly = date.Value.Date;
        TimeOnly defaultTime = time ?? (isEndOfDay ? new TimeOnly(23, 59, 59) : new TimeOnly(0, 0, 0));
        DateTime istDateTime = dateOnly.Add(defaultTime.ToTimeSpan());

        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(istDateTime, DateTimeKind.Unspecified), IstZone);
    }

    /// <summary>
    /// Validates and standardizes mobile phone numbers with country code.
    /// </summary>
    public static string CheckMobileNumberFormat(string mobile)
    {
        string trimmed = mobile.Trim();
        if (trimmed.Length < 10 || trimmed.Length > 15)
        {
            throw new ArgumentException("Invalid mobile phone number format.");
        }

        if (trimmed.Length == 10 && !trimmed.StartsWith('+'))
        {
            return "+91" + trimmed;
        }

        return trimmed;
    }

    /// <summary>
    /// High-performance compiled expression trimmer for object string properties.
    /// </summary>
    public static class ObjectStringTrimmer
    {
        private static readonly ConcurrentDictionary<Type, Action<object>> Cache = new();

        public static void TrimAllStrings(object? obj)
        {
            if (obj == null) return;

            var type = obj.GetType();
            var action = Cache.GetOrAdd(type, CreateTrimAction);
            action(obj);
        }

        private static Action<object> CreateTrimAction(Type type)
        {
            var param = Expression.Parameter(typeof(object), "obj");
            var casted = Expression.Variable(type, "typedObj");
            var assigns = new List<Expression>
            {
                Expression.Assign(casted, Expression.Convert(param, type))
            };

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                         .Where(p => p.CanRead && p.CanWrite && p.PropertyType == typeof(string)))
            {
                var propExp = Expression.Property(casted, prop);
                var assignTrim = Expression.Assign(
                    propExp,
                    Expression.Condition(
                        Expression.Equal(propExp, Expression.Constant(null, typeof(string))),
                        Expression.Constant(null, typeof(string)),
                        Expression.Call(propExp, nameof(string.Trim), Type.EmptyTypes)
                    )
                );

                assigns.Add(assignTrim);
            }

            var body = Expression.Block(new[] { casted }, assigns);
            return Expression.Lambda<Action<object>>(body, param).Compile();
        }
    }
}
