using System.Text;
using System.Text.Json;

var endpoint = "https://dmkorolev-aifoundry-francecentral.openai.azure.com"; // your Azure OpenAI / Foundry endpoint
var deploymentName = "dmkorolev-aifoundry-francecentral"; // the deployment you made in Azure (not a model name)
var apiKey = Environment.GetEnvironmentVariable("DMKOROLEV_FOUNDRY_OPENAI_KEY");

using var http = new HttpClient();
http.DefaultRequestHeaders.Add("api-key", apiKey); // or use Bearer token if using AAD

var apiVersion = "2024-12-01";

// 1) Create a response (POST)
var createUrl = $"{endpoint}/openai/deployments/{deploymentName}/responses?api-version={apiVersion}";

var payload = new
{
    input = new[] {
        new { role = "user", content = "Say hi and return a short id." }
    }
};

var json = JsonSerializer.Serialize(payload);
var postResp = await http.PostAsync(createUrl, new StringContent(json, Encoding.UTF8, "application/json"));
postResp.EnsureSuccessStatusCode();

var postBody = await postResp.Content.ReadAsStringAsync();
using var doc = JsonDocument.Parse(postBody);
var responseId = doc.RootElement.GetProperty("id").GetString();

Console.WriteLine($"Created response id: {responseId}");
Console.WriteLine("Create response body:");
Console.WriteLine(postBody);

// 2) Retrieve that response (GET) — returns the snapshot for that responseId
var getUrl = $"{endpoint}/openai/responses/{responseId}?api-version={apiVersion}";
var getResp = await http.GetAsync(getUrl);
getResp.EnsureSuccessStatusCode();

var getBody = await getResp.Content.ReadAsStringAsync();
Console.WriteLine("GET by id returns snapshot:");
Console.WriteLine(getBody);