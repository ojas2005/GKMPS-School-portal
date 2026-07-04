using SchoolERP.Communication.DTOs;

namespace SchoolERP.Communication.Services.Interfaces;

public interface IParentMessageService
{
    Task<ParentMessageSummary> SendAsync(SendMessageRequest request, Guid senderUserId, CancellationToken ct = default);
    Task<IReadOnlyList<ParentMessageSummary>> GetThreadAsync(Guid studentId, CancellationToken ct = default);
    Task MarkReadAsync(Guid messageId, CancellationToken ct = default);
}
