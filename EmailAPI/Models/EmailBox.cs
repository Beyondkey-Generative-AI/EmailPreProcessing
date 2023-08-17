using Microsoft.WindowsAzure.Storage.Table;

namespace EmailManagementAPI.Models
{
    public class EmailBoxEntity : TableEntity
    {
        public string? Email { get; set; }
        public bool IsAutoReply { get; set; }
        public string? InternalKnowledgebaseLink { get; set; }
        public DateTime LastReadEmailDateTime { get; set; }
    }
}
