using Microsoft.WindowsAzure.Storage.Table;

namespace EmailManagementAPI.Models
{
    public class EmailBoxEntity : TableEntity
    {
        public EmailBoxEntity(string partitionKey, string rowKey)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
        }

        public EmailBoxEntity() { }

        public string Email { get; set; }
        public bool IsAutoReply { get; set; }
        public string InternalKnowledgebaseLink { get; set; }
        public string LastReadEmailUniqueId { get; set; }
        public DateTime? LastReadEmailDateTime { get; set; }
        public string EmailBoxServerId { get; set; }
    }
}
