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
using BusinessLayer;
using CommonLayer;
using System.Dynamic;
using Microsoft.Azure.Documents;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Graph.Models.ExternalConnectors;

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
                var emailBusiness = new EmailBusiness(_configuration);
                var graphApiEndpoint = "https://graph.microsoft.com/v1.0";
                var sharedMailboxId = "emailpreprocessing@beyondkey.com";
                var accessToken = await emailBusiness.GetAccessToken();
                //string lastProcessedEmailId = LoadLastProcessedEmailId();
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    // Calculate the date range for 1 day ago
                    DateTime oneDayAgo = DateTime.UtcNow.AddDays(-1);
                    string formattedDate = oneDayAgo.ToString("yyyy-MM-ddTHH:mm:ssZ");

                    string excludeJunkFilter = "$filter=receivedDateTime ge " + formattedDate + " and not categories/any(c: c eq 'Junk')";
                    string requestUrl = $"{graphApiEndpoint}/users/{sharedMailboxId}/messages?" +
                        $"{excludeJunkFilter}" +
                        $"&$orderby=receivedDateTime desc&$top=1000";


                    var response = await httpClient.GetAsync(requestUrl);

                    if (response.IsSuccessStatusCode)
                    {
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
                            var filteredAttributes = new FilteredEmailAttributes
                            {
                                EmailUniqueId = message.id,
                                EmailCreatedDateTime = ((DateTime)message.receivedDateTime).ToUniversalTime(),
                                HasAttachment = message.hasAttachments == null ? false : Convert.ToBoolean(message.hasAttachments),
                                FromName = "Deepak Sharma",// message.from?.emailAddress?.name,
                                FromEmailAddress = "deepak.sharma@beyondkey.com",// message.from?.emailAddress?.address,
                                ToName = toName,
                                ToEmailAddress = toEmailAddress,
                                ConversationId = message.conversationId,
                                ParentFolderId = message.parentFolderId,
                                IsRead = message.isRead,
                                Body = Utility.MaskSensitiveInformation(Convert.ToString(message.body.content)),
                                Subject = message.subject
                            };
                            filteredAttributesList.Add(filteredAttributes);
                        }
                        var lastProcessedEmailUniqueId = string.Empty;
                        // Convert the list of filtered attributes to JSON
                        var filteredJsonList = JsonConvert.SerializeObject(filteredAttributesList);
                        foreach (var filteredAttributes in filteredAttributesList)
                        {
                                var ActualEmailBlobUrl = await emailBusiness.UploadJsonToBlobAsync(filteredAttributes);
                                await Utility.ExecuteWithRetry(async () =>
                                {
                                    // Your Azure Table Storage operation here
                                    await emailBusiness.SaveJsonToTableStorage(filteredAttributes, ActualEmailBlobUrl);
                                }, maxAttempts: 3, retryInterval: TimeSpan.FromSeconds(1));
                            //lastProcessedEmailId = filteredAttributes.EmailUniqueId;
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

      
    }
    public class FilteredEmailAttributes
    {
        public string EmailUniqueId { get; set; }
        public DateTime EmailCreatedDateTime { get; set; }
        public bool HasAttachment { get; set; }
        public string FromName { get; set; }
        public string FromEmailAddress { get; set; }
        public string ToName { get; set; }
        public string ToEmailAddress { get; set; }
        public string ConversationId { get; set; }
        public string ParentFolderId { get; set; }
        public bool IsRead { get; set; }
        public string Body { get; set; }
        public string Subject { get; set; }
    }

}

