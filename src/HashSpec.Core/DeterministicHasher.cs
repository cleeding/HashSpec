using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Text;

namespace HashSpec.Core;

public static class DeterministicHasher
{
    public static string CreateFingerprint(object state)
    {
        var json = JsonConvert.SerializeObject(state);
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