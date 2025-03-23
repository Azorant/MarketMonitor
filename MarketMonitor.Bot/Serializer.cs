using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace MarketMonitor.Bot;

public static class Serializer
{
    public static byte[] Serialize(object obj)
    {
        using var ms = new MemoryStream();
        using (var writer = new BsonDataWriter(ms))
        {
            var serializer = new JsonSerializer();
            serializer.Serialize(writer, obj);
        }

        return ms.ToArray();
    }

    public static T? Deserialize<T>(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        using var reader = new BsonDataReader(ms);
        var serializer = new JsonSerializer();

        return serializer.Deserialize<T>(reader);
    }

    public static bool TryDeserialize<T>(byte[] bytes, out T? obj)
    {
        var value = Deserialize<T>(bytes);
        if (value == null)
        {
            obj = default;
            return false;
        }

        obj = value;
        return true;
    }
}