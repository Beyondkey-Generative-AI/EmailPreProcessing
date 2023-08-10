using Bogus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System.Net;


namespace EmailManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JsonController : Controller
    {
        private readonly Faker _faker;

        public JsonController()
        {
            _faker = new Faker();
        }

        [HttpGet]
        [Route("GetFakeOrgData")]
        public IActionResult GetFakeOrgData()
        {
            var testRecords = new List<object>();
            for (int i = 0; i < 3; i++)
            {
                var record = new
                {
                    ID = _faker.Random.Guid().ToString(),
                    OrgName = _faker.Company.CompanyName(),
                    Address = _faker.Address.FullAddress(),
                    Date_Created = _faker.Date.Past().ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    Date_Modified = _faker.Date.Recent().ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                testRecords.Add(record);
            }

            return Ok(testRecords);
        }
        [HttpGet]
        [Route("GetFakeEmailData")]
        public IActionResult GetFakeEmailData()
        {
            var testRecords = new List<object>();
            for (int i = 0; i < 3; i++)
            {
                var record = new
                {
                    ID = _faker.Random.Guid().ToString(),
                    EmailUniqueId = _faker.Random.Guid().ToString(),
                    EmailCreatedDateTime = _faker.Date.Past().ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    HasAttachment = _faker.Random.Bool(),
                    FromName = _faker.Name.FullName(),
                    FromEmailAddress = _faker.Internet.Email(),
                    conversationId = _faker.Random.Guid().ToString(),
                    parentFolderId = _faker.Random.Guid().ToString(),
                    isRead = _faker.Random.Bool(),
                    EmailCategory = _faker.Lorem.Word(),
                    Body = _faker.Lorem.Paragraph(),
                    Subject = _faker.Lorem.Sentence(),
                    Summary = _faker.Lorem.Sentence(),
                    Sentiment = _faker.Lorem.Word(),
                    Entities = _faker.Lorem.Sentence()
                };

                testRecords.Add(record);
            }

            return Ok(testRecords);
        }

        [HttpPost]
        [Route("ValidateJsonSchema")]
        public IActionResult ValidateJsonSchema([FromForm] string jsonUrl, [FromForm] string schemaFilePath)
        {
            try
            {
                // Read the JSON Schema from the provided file path
                string schemaContent = System.IO.File.ReadAllText(schemaFilePath);

                // Load the JSON Schema
                JSchema schema = JSchema.Parse(schemaContent);

                // Fetch JSON data from the provided URL
                using (var webClient = new WebClient())
                {
                    string jsonData = webClient.DownloadString(jsonUrl);

                    // Parse JSON data into a JObject
                    JObject jsonObject = JObject.Parse(jsonData);

                    // Validate the JSON data against the schema
                    IList<string> errorMessages;
                    bool isValid = jsonObject.IsValid(schema, out errorMessages);

                    if (!isValid)
                    {
                        // Handle validation errors, log or return appropriate response
                        return BadRequest(errorMessages);
                    }

                    // Continue processing since the JSON data is valid
                    // ...

                    return Ok("JSON data is valid.");
                }
            }
            catch (Exception ex)
            {
                // Handle any exception that may occur during URL fetching, file reading, or JSON parsing
                return BadRequest("Error: " + ex.Message);
            }
        }

        [HttpPost]
        [Route("ValidateJsonSchemaUrl")]
        public IActionResult ValidateJsonSchemaUrl([FromForm] string jsonUrl, [FromForm] string schemaFileUrl)
        {
            try
            {
                // Fetch the JSON Schema from the provided schema URL
                using (var webClient = new WebClient())
                {
                    // Fetch JSON data from the provided URL
                    string jsonData;
                    jsonData = webClient.DownloadString(jsonUrl);
                    string schemaContent = webClient.DownloadString(schemaFileUrl);

                    var schemaResolver = new JSchemaUrlResolver();
                    var schema = JSchema.Parse(schemaContent, schemaResolver);

                    JToken jsonToken = JToken.Parse(jsonData);
                    IList<string> validationErrors;
                    if (!jsonToken.IsValid(schema, out validationErrors))
                    {
                        return BadRequest(validationErrors);
                    }
                    return Ok("JSON data is valid.");

                }
            }
            catch (Exception ex)
            {
                // Handle any exception that may occur during URL fetching, file reading, or JSON parsing
                return BadRequest("Error: " + ex.Message);
            }
        }

    }
    
}
