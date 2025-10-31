using Logistic_Shipment_tracker.Models;
using System.ComponentModel.DataAnnotations;

namespace Logistic_Shipment_tracker.DTOs
{
    public class AuditLogResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
 public string UserName { get; set; } = string.Empty;
 public string Action { get; set; } = string.Empty;
        public string? TargetTable { get; set; }
        public Guid? TargetId { get; set; }
        public DateTime Timestamp { get; set; }
    }

 public class AuditLogQueryRequest
    {
   public Guid? UserId { get; set; }
        public string? Action { get; set; }
 public string? TargetTable { get; set; }
     public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
     public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

  public class AuditLogPagedResponse
    {
 public List<AuditLogResponse> Logs { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
