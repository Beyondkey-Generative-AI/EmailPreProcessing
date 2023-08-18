using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace DataAccessLayer
{
    public class StorageManager
    {
        private readonly IConfiguration _configuration;

        public StorageManager(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public CloudTableClient GetCloudTableClient()
        {
            string connectionString = _configuration.GetConnectionString("BlobStorageConnection");
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            return storageAccount.CreateCloudTableClient();
        }

        public BlobContainerClient GetBlobContainerClient()
        {
            string connectionString = _configuration.GetConnectionString("BlobStorageConnection");
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            string containerName = _configuration.GetSection("StorageContainer:ContainerProcessedEmail").Value;
            return blobServiceClient.GetBlobContainerClient(containerName);
        }
    }
}