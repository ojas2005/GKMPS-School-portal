using SchoolERP.Communication.DTOs;
using SchoolERP.Communication.Entities;
using SchoolERP.Communication.Repositories.Interfaces;
using SchoolERP.Communication.Services.Interfaces;

namespace SchoolERP.Communication.Services;

public class ParentMessageService : IParentMessageService
{
    private readonly IParentMessageRepository _messages;

    public ParentMessageService(IParentMessageRepository messages) => _messages = messages;

    public async Task<ParentMessageSummary> SendAsync(SendMessageRequest request, Guid senderUserId, CancellationToken ct = default)
    {
        var message = new ParentMessage
        {
            StudentId = request.StudentId,
            SenderUserId = senderUserId,
            RecipientUserId = request.RecipientUserId,
            Body = request.Body
        };

        await _messages.AddAsync(message, ct);
        await _messages.SaveChangesAsync(ct);

        return ToSummary(message);
    }

    public async Task<IReadOnlyList<ParentMessageSummary>> GetThreadAsync(Guid studentId, CancellationToken ct = default)
    {
        var items = await _messages.FindByStudentIdAsync(studentId, ct);
        return items.Select(ToSummary).ToList();
    }

    public Task MarkReadAsync(Guid messageId, CancellationToken ct = default) => _messages.MarkReadAsync(messageId, ct);

    private static ParentMessageSummary ToSummary(ParentMessage m) =>
        new(m.Id, m.StudentId, m.SenderUserId, m.RecipientUserId, m.Body, m.IsRead, m.CreatedAtUtc);
}
