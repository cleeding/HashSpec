using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Reflection;

namespace HashSpec.Core;

public static class DeterministicHasher
{
    private static readonly JsonSerializerSettings _settings = new()
    {
        ContractResolver = new HashSpecResolver()
    };

    public static string CreateFingerprint(object state)
    {
        // Use the custom settings to ignore [HashIgnore] properties
        var json = JsonConvert.SerializeObject(state, _settings);
        var jObj = JToken.Parse(json);
        string canonicalJson = SortJToken(jObj).ToString(Formatting.None);

        using var sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(canonicalJson));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    private static JToken SortJToken(JToken token)
    {
        if (token is JObject obj)
        {
            var sortedObj = new JObject();
            foreach (var prop in obj.Properties().OrderBy(p => p.Name))
                sortedObj.Add(prop.Name, SortJToken(prop.Value));
            return sortedObj;
        }
        return token is JArray arr ? new JArray(arr.Select(SortJToken)) : token;
    }
}

// Internal helper to filter out the ignored properties
internal class HashSpecResolver : DefaultContractResolver
{
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization);
        if (member.GetCustomAttribute<HashIgnoreAttribute>() != null)
        {
            property.ShouldSerialize = _ => false;
        }
        return property;
    }
}