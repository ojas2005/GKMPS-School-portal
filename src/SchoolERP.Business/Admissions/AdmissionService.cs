using SchoolERP.Business.Fee.DTOs;
using SchoolERP.Business.Fee.Services.Interfaces;
using SchoolERP.Business.Student.DTOs;
using SchoolERP.Business.Student.Services.Interfaces;
using SchoolERP.Business.Transport.DTOs;
using SchoolERP.Business.Transport.Services.Interfaces;
using SchoolERP.Common;
using SchoolERP.Common.Events;

namespace SchoolERP.Business.Admissions;

/// <summary>
/// Admits a student across the modules it touches: the student record (Student), the route
/// (Transport) and the fee account with its first fees (Fee). Each module keeps its own
/// database, so there's no single transaction across them; instead everything that can be
/// checked is checked first, and if a later step still fails the earlier ones are undone, so
/// a failed admission leaves no student, no route assignment and no fees behind.
/// </summary>
public class AdmissionService
{
    private readonly IStudentService _students;
    private readonly IFeeAccountService _fees;
    private readonly IStudentRouteMappingService _transport;
    private readonly IEventPublisher _events;
    private readonly TimeProvider _clock;
    private readonly ILogger<AdmissionService> _logger;

    public AdmissionService(IStudentService students, IFeeAccountService fees, IStudentRouteMappingService transport,
        IEventPublisher events, TimeProvider clock, ILogger<AdmissionService> logger)
    {
        _students = students;
        _fees = fees;
        _transport = transport;
        _events = events;
        _clock = clock;
        _logger = logger;
    }

    public async Task<AdmittedStudent> AdmitAsync(CreateStudentRequest request, Guid? actorUserId, string? actorRole, CancellationToken ct = default)
    {
        if (!SchoolClasses.IsKnown(request.ClassId))
            throw new InvalidOperationException("Choose one of the school's classes.");

        var today = SchoolClock.Today(_clock);

        // 1. Check the fees can be worked out (class fee structure, an active route) before saving anything.
        await _fees.PreviewAsync(request.ClassId, request.RouteId, today, request.PreviousDues, ct);

        // 2. The student record (its own checks: unique admission number, the parent's login).
        var student = await _students.CreateAdmissionAsync(request, ct);

        var routeAssigned = false;
        try
        {
            // 3. Transport, then 4. the fee account and its first fees (one transaction in the fee database).
            if (request.RouteId is { } routeId)
            {
                await _transport.AssignAsync(new AssignStudentRouteRequest(student.Id, routeId, request.PickupPoint), ct);
                routeAssigned = true;
            }

            var fees = await _fees.OpenAccountAsync(new OpenFeeAccountCommand(student.Id, student.ClassId, today, request.RouteId,
                ChargeAdmissionFee: true, request.PreviousDues, actorUserId, actorRole), ct);

            // Only now, with everything saved: the welcome email to the parents.
            await _events.PublishAsync(new StudentEnrolledEvent
            {
                StudentId = student.Id,
                AdmissionNumber = student.AdmissionNumber,
                FullName = student.FullName,
                ClassId = student.ClassId,
                SectionId = student.SectionId,
                ParentEmails = new[] { student.FatherEmail, student.MotherEmail }
                    .Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e!).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            }, ct);

            _logger.LogInformation("Student admitted: {StudentId} ({AdmissionNumber}); ₹{Pending} payable now, ₹{Upcoming} later this session",
                student.Id, student.AdmissionNumber, fees.InitialPending, fees.UpcomingTotal);
            return new AdmittedStudent(student, fees);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Admission of {AdmissionNumber} failed after the student record was saved; undoing it", student.AdmissionNumber);
            await UndoAsync(student.Id, routeAssigned);
            throw new InvalidOperationException(
                $"The admission wasn't saved: {(ex is InvalidOperationException or KeyNotFoundException ? ex.Message : "the fee account couldn't be set up")}. Nothing was kept; please try again.", ex);
        }
    }

    // Best effort, each step on its own: an undo that fails is logged for the office to fix by hand.
    private async Task UndoAsync(Guid studentId, bool routeAssigned)
    {
        var none = CancellationToken.None; // finish undoing even if the request was abandoned
        try { await _fees.DiscardAccountAsync(studentId, none); }
        catch (Exception ex) { _logger.LogError(ex, "Undo: couldn't remove the fee account of {StudentId}", studentId); }

        if (routeAssigned)
        {
            try { await _transport.RemoveForStudentAsync(studentId, none); }
            catch (Exception ex) { _logger.LogError(ex, "Undo: couldn't remove the route of {StudentId}", studentId); }
        }

        try { await _students.UndoAdmissionAsync(studentId, none); }
        catch (Exception ex) { _logger.LogError(ex, "Undo: couldn't delete the student record {StudentId}", studentId); }
    }
}
