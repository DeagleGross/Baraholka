using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

var msiTokenCred = new ManagedIdentityCredential(clientId: "2231b37d-bc17-4a15-9461-e5ca4b964bbb");
var client = new CosmosClient(accountEndpoint: "https://dmkorolev-cosmos.documents.azure.com:443/", tokenCredential: msiTokenCred);

var container = client.GetContainer("Orleans", "destinationtest");