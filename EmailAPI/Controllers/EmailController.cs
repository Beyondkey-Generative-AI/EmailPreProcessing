using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Text;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;


namespace EmailManagementAPI.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class EmailController : Controller
    {
        private readonly IConfiguration _configuration;

        public EmailController(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        [HttpGet]
        [Route("GetSharedEmail")]
        [ProducesResponseType(200, Type = typeof(string))]
        [ProducesResponseType(400, Type = typeof(string))]
        public async Task<ActionResult> GetSharedEmail()
        {
            try
            {
                var graphApiEndpoint = "https://graph.microsoft.com/v1.0";
                var sharedMailboxId = "emailpreprocessing@beyondkey.com";
                var accessToken = await GetAccessToken();
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    // Calculate the date range for 1 day ago
                    DateTime oneDayAgo = DateTime.UtcNow.AddDays(-1);
                    string formattedDate = oneDayAgo.ToString("yyyy-MM-ddTHH:mm:ssZ");

                    string excludeJunkFilter = "$filter=receivedDateTime ge " + formattedDate + " and not categories/any(c: c eq 'Junk')";
                    string recipientEmail = "contact@beyondkey.com";
                    string requestUrl = $"{graphApiEndpoint}/users/{sharedMailboxId}/messages?" +
                        $"{excludeJunkFilter}" +
                        $"&$orderby=receivedDateTime desc&$top=1000";


                    var response = await httpClient.GetAsync(requestUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string connectionString = _configuration.GetConnectionString("BlobStorageConnection");
                        CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
                        CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

                        string containerName = _configuration.GetSection("StorageContainer:ContainerProcessedEmail").Value;
                        BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
                        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

                        var content = await response.Content.ReadAsStringAsync();

                        // Deserialize the JSON response into objects
                        var messages = JsonConvert.DeserializeObject<dynamic>(content);

                        // Initialize a list to store the filtered attributes
                        var filteredAttributesList = new List<dynamic>();

                        foreach (var message in messages.value)
                        {// Extract the 'To' information from recipients array
                            var recipients = message.toRecipients;
                            string toName = string.Empty;
                            var toEmailAddress = string.Empty;
                            if (recipients != null && recipients.HasValues)
                            {
                                // For simplicity, assuming there's only one recipient in 'To' field.
                                // If there can be multiple recipients, you may need to adjust the logic accordingly.
                                toName = recipients[0]?.emailAddress?.name;
                                toEmailAddress = recipients[0]?.emailAddress?.address;
                            }
                            if (toEmailAddress.Trim().ToLower() != "contact@beyondkey.com") continue;
                            var filteredAttributes = new
                            {
                                EmailUniqueId = message.id,
                                EmailCreatedDateTime = message.receivedDateTime,
                                HasAttachment = message.hasAttachments,
                                FromName = "Deepak Sharma",// message.from?.emailAddress?.name,
                                FromEmailAddress = "deepak.sharma@beyondkey.com",// message.from?.emailAddress?.address,
                                ToName = toName,
                                ToEmailAddress = toEmailAddress,
                                conversationId = message.conversationId,
                                parentFolderId = message.parentFolderId,
                                isRead = message.isRead,
                                Body = Utility.MaskSensitiveInformation(Convert.ToString(message.body.content)),
                                Subject = message.subject
                            };


                            filteredAttributesList.Add(filteredAttributes);
                        }

                       
                            // Convert the list of filtered attributes to JSON
                            var filteredJsonList = JsonConvert.SerializeObject(filteredAttributesList);
                        foreach (var filteredAttributes in filteredAttributesList)
                        {
                            var ActualEmailBlobUrl = await UploadJsonToBlobAsync(filteredAttributes, containerClient);

                            try
                            {
                                await ExecuteWithRetry(async () =>
                                {
                                    // Your Azure Table Storage operation here
                                    await SaveJsonToTableStorage(filteredAttributes, ActualEmailBlobUrl, tableClient);
                                }, maxAttempts: 3, retryInterval: TimeSpan.FromSeconds(1));
                            }
                            catch   (Exception ex)
                            {
                                var a = filteredAttributes;
                                var b = ActualEmailBlobUrl;
                            }
                            //await SaveJsonToTableStorage(filteredAttributes, ActualEmailBlobUrl, tableClient);
                        }
                        return Ok(filteredJsonList);
                    }
                    else
                    {
                        // Handle the error response
                        var errorMessage = await response.Content.ReadAsStringAsync();
                        return BadRequest(errorMessage);
                    }

                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"An error occurred: {ex.Message}\nStack trace: {ex.StackTrace}";
                return BadRequest(errorMessage);
            }

        }
        private string GetEmailBodyContent(dynamic message)
        {
            string body = message.body.content;
            return Utility.MaskSensitiveInformation(body);
        }
        async Task<string> UploadJsonToBlobAsync(dynamic filteredAttributes, BlobContainerClient containerClient)
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


        private async Task SaveJsonToTableStorage(dynamic filteredAttributes, dynamic ActualEmailBlobUrl, CloudTableClient tableClient)
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

        private async Task ExecuteWithRetry(Func<Task> operation, int maxAttempts, TimeSpan retryInterval)
        {
            int attempts = 0;

            while (attempts < maxAttempts)
            {
                try
                {
                    await operation();
                    return; // Operation succeeded, exit the loop
                }
                catch (Exception ex)
                {
                    // Log or handle the exception if needed
                    Console.WriteLine($"Error: {ex.Message}");

                    attempts++;

                    if (attempts < maxAttempts)
                    {
                        await Task.Delay(retryInterval); // Wait for a specified interval before retrying
                    }
                }
            }

            throw new Exception($"Operation failed after {maxAttempts} attempts.");
        }




        private async Task<string> GetAccessToken()
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

    }
}

