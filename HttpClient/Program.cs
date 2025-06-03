var handler = new HttpClientHandler
{
    ServerCertificateCustomValidationCallback 
        = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
};

using var client = new HttpClient(handler);

var msg = new HttpRequestMessage(HttpMethod.Get, "https://localhost:5000/hello-world");
msg.Headers.Host = "testserver:5000";
msg.Headers.Add("Invalid Header", "value");

var res = await client.SendAsync(msg);
Console.WriteLine(res);