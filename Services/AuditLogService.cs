using Logistic_Shipment_tracker.Data;
using Logistic_Shipment_tracker.Models;

namespace Logistic_Shipment_tracker.Services
{
    public interface IAuditLogService
    {
        Task LogActionAsync(Guid userId, string action, string? targetTable = null, Guid? targetId = null);
    }

    public class AuditLogService : IAuditLogService
    {
        private readonly ApplicationDBContext _context;

        public AuditLogService(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task LogActionAsync(Guid userId, string action, string? targetTable = null, Guid? targetId = null)
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = action,
                TargetTable = targetTable,
                TargetId = targetId,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
    }
}
