using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace DiarsFinal.Helpers
{
    public static class SessionExtensions
    {
        public static void SetObject(
            this ISession session,
            string key,
            object value)
        {
            session.SetString(
                key,
                JsonSerializer.Serialize(value));
        }

        public static T? GetObject<T>(
    this ISession session,
    string key)
        {
            string? value =
                session.GetString(key);

            if (string.IsNullOrEmpty(value))
            {
                return default;
            }

            try
            {
                return JsonSerializer.Deserialize<T>(value);
            }
            catch
            {
                return default;
            }
        }
    }
}