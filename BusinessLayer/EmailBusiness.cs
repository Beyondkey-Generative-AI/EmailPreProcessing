using Azure.Storage.Blobs;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System.Text;
using CommonLayer;
using Microsoft.Identity.Client;

namespace BusinessLayer
{
    public class EmailBusiness
    {
        public async Task<string> GetAccessToken()
        {
            //string clientId = "api://82f931e4-60f4-49e9-9ef9-22d1ee859aa6";
            //string tenantId = "6ffae15d-c8f1-44f0-8f29-a9167a3e1f28";
            //string clientSecret = "iEs8Q~OzPrL3cmC.XgR7VaIIPPkx~Ldq56YqXc1F";

            //Contact@beyondkey.com
            string clientId = "b39630db-a081-4381-9fba-d61958840eca";
            string tenantId = "2956020b-89d7-495b-bbc4-1ceac9034674";
            string clientSecret = "zTt8Q~wSjxRaLFZNZ5OzMFMwDSrZYgp-Pt4XMcaX";

            var confidentialClientApplication = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithClientSecret(clientSecret)
                .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
                .Build();

            var scopes = new[] { "https://graph.microsoft.com/.default" };

            var authenticationResult = await confidentialClientApplication
                .AcquireTokenForClient(scopes)
                .ExecuteAsync();

            return authenticationResult.AccessToken;
        }
        public async Task<string> UploadJsonToBlobAsync(dynamic filteredAttributes, BlobContainerClient containerClient)
        {
            // Convert the current element to JSON
            var filteredJson = JsonConvert.SerializeObject(filteredAttributes);
            // Generate a unique blob name for each JSON file based on EmailUniqueId
            var emailUniqueId = filteredAttributes.EmailUniqueId;
            DateTime currentDate = DateTime.UtcNow.Date;
            string folderName = currentDate.ToString("yyyyMMdd");

            // Include the folder name as part of the blob name
            var blobName = $"{folderName}/{emailUniqueId}.json";

            // Upload the JSON content to Azure Blob Storage
            BlobClient blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes(filteredJson)), true);
            // Get the URL of the uploaded blob
            string blobUrl = blobClient.Uri.ToString();
            return blobUrl;
        }
        public async Task SaveJsonToTableStorage(dynamic filteredAttributes, dynamic ActualEmailBlobUrl, CloudTableClient tableClient)
        {
            // Create or reference a table in Azure Table Storage
            CloudTable table = tableClient.GetTableReference("EmailProcessed");
            await table.CreateIfNotExistsAsync();

            // Extract properties from the filteredAttributes object
            string rowKey = filteredAttributes.EmailUniqueId;
            string partitionKey = filteredAttributes.ToEmailAddress;
            DynamicTableEntity entity = new DynamicTableEntity(partitionKey, rowKey);
            var propertyName = "ActualEmailJsonLink";
            var propertyValue = ActualEmailBlobUrl;
            entity.Properties.Add(propertyName, EntityProperty.CreateEntityPropertyFromObject(propertyValue));

            foreach (var property in filteredAttributes.GetType().GetProperties())
            {
                propertyName = property.Name;
                propertyValue = property.GetValue(filteredAttributes, null);
                if (property.Name == "Body")
                {
                    int maxPropertySize = 5000;
                    string truncatedValue = propertyValue != null ? Utility.TruncateHtmlString(propertyValue.ToString(), maxPropertySize) : string.Empty;
                    entity.Properties.Add(propertyName, EntityProperty.CreateEntityPropertyFromObject(truncatedValue));
                }
                else
                {
                    entity.Properties.Add(propertyName, EntityProperty.CreateEntityPropertyFromObject(propertyValue));
                }
                propertyValue = string.Empty;
            }

            TableOperation insertOperation = TableOperation.Insert(entity);

            await table.ExecuteAsync(insertOperation);
        }
    }
}