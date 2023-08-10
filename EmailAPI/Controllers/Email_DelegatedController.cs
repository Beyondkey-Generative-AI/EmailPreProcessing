using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace EmailManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Email_DelegatedController : Controller
    {
        [HttpGet]
        [Route("GetDelegatedEmails")]
        public async Task<IActionResult> GetDelegatedEmails()
        {
            try
            {
                GraphApiService graphApiService = new GraphApiService();
                string accessToken = await graphApiService.GetAccessToken();
                var emails = await graphApiService.GetEmails(accessToken);
                return Ok(emails);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
    public class GraphApiService
    {
        private static readonly string clientId = "api://82f931e4-60f4-49e9-9ef9-22d1ee859aa6";
        private static readonly string tenantId = "6ffae15d-c8f1-44f0-8f29-a9167a3e1f28";
        private static readonly string clientSecret = "iEs8Q~OzPrL3cmC.XgR7VaIIPPkx~Ldq56YqXc1F";
        private static readonly string username = "ds@bk1012.onmicrosoft.com";
        private static readonly string password = "Sharepoint#7";
        private static readonly string graphApiUrl = "https://graph.microsoft.com/v1.0/";

        private readonly HttpClient httpClient;

        public GraphApiService()
        {
            httpClient = new HttpClient();
        }

        public async Task<string> GetAccessToken()
        {
            var tokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
            var requestBody = $"grant_type=password&client_id={clientId}&client_secret={clientSecret}&scope=https%3A%2F%2Fgraph.microsoft.com%2F.default&username={Uri.EscapeDataString(username)}&password={Uri.EscapeDataString(password)}";

            var response = await httpClient.PostAsync(tokenEndpoint, new StringContent(requestBody, System.Text.Encoding.UTF8, "application/x-www-form-urlencoded"));
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var jsonDocument = JsonDocument.Parse(responseContent);
                var accessToken = jsonDocument.RootElement.GetProperty("access_token").GetString();
                return accessToken ?? string.Empty;
            }
            else
            {
                // Handle the error response
                Console.WriteLine($"Error retrieving access token: {responseContent}");
                return string.Empty;
            }
        }

        public async Task<string> GetEmails(string accessToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{graphApiUrl}me/messages");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return responseContent;
            }
            else
            {
                // Handle the error response
                Console.WriteLine($"Error retrieving emails: {responseContent}");
                return string.Empty;
            }
        }
    }

