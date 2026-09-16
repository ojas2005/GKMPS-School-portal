using SchoolERP.Communication.DTOs;

namespace SchoolERP.Communication.Services.Interfaces;

public interface IParentMessageService
{
    Task<ParentMessageSummary> SendAsync(SendMessageRequest request, Guid senderUserId, CancellationToken ct = default);
    Task<IReadOnlyList<ParentMessageSummary>> GetThreadAsync(Guid studentId, CancellationToken ct = default);
    /// <summary>Returns false when no message with this id is addressed to the recipient.</summary>
    Task<bool> MarkReadAsync(Guid messageId, Guid recipientUserId, CancellationToken ct = default);
}
