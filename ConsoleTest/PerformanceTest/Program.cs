using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


var text = File.ReadAllText("data.json");
var token = JsonConvert.DeserializeObject<JToken>(text);
if (token is null)
{
    Console.WriteLine("Failed to parse JSON");
    return;
}

Console.WriteLine("The URL is:");
Console.WriteLine(token["url"]?.ToObject<string>());