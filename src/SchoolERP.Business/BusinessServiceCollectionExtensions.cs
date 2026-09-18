using SchoolERP.Business.Identity.Sessions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;
using SchoolERP.Business.Academic.Services;
using SchoolERP.Business.Academic.Services.Interfaces;
using SchoolERP.Business.Attendance.Services;
using SchoolERP.Business.Attendance.Services.Interfaces;
using SchoolERP.Business.Common.Events;
using SchoolERP.Business.Communication.Services;
using SchoolERP.Business.Communication.Services.Interfaces;
using SchoolERP.Business.Examination.Services;
using SchoolERP.Business.Examination.Services.Interfaces;
using SchoolERP.Business.Fee.Services;
using SchoolERP.Business.Fee.Services.Interfaces;
using SchoolERP.Business.Identity.Auth;
using SchoolERP.Business.Identity.Services;
using SchoolERP.Business.Identity.Services.Interfaces;
using SchoolERP.Business.Library.Services;
using SchoolERP.Business.Library.Services.Interfaces;
using SchoolERP.Business.Notification.Handlers;
using SchoolERP.Business.Notification.Services;
using SchoolERP.Business.Notification.Services.Interfaces;
using SchoolERP.Business.Reporting.Services;
using SchoolERP.Business.Reporting.Services.Interfaces;
using SchoolERP.Business.Staff.Services;
using SchoolERP.Business.Staff.Services.Interfaces;
using SchoolERP.Business.Student.Services;
using SchoolERP.Business.Student.Services.Interfaces;
using SchoolERP.Business.Transport.Services;
using SchoolERP.Business.Transport.Services.Interfaces;
using SchoolERP.Common.Events;
using SchoolERP.DataAccess;

namespace SchoolERP.Business;

public static class BusinessServiceCollectionExtensions
{
    public static IServiceCollection AddBusiness(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDataAccess(configuration);

        QuestPDF.Settings.License = LicenseType.Community;

        // A single process needs no shared cache server: an in-memory IDistributedCache keeps
        // the existing caching code (timetables, active-student counts, refresh tokens) as-is.
        services.AddDistributedMemoryCache();

        // Identity
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<ITokenGenerator, TokenGenerator>();
        services.AddScoped<StudentProfileResolver>();
        services.AddScoped<StaffProfileResolver>();
        services.AddMemoryCache();
        services.AddSingleton<SchoolERP.Business.Common.Audit.ChannelAuditTrail>();
        services.AddSingleton<SchoolERP.Common.Audit.IAuditTrail>(sp => sp.GetRequiredService<SchoolERP.Business.Common.Audit.ChannelAuditTrail>());
        services.AddHostedService<SchoolERP.Business.Common.Audit.AuditWriter>();
        services.AddSingleton(TimeProvider.System);
        services.Configure<SessionOptions>(configuration.GetSection(SessionOptions.SectionName));
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        // Student
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<ITransferCertificateService, TransferCertificateService>();
        // Staff
        services.AddScoped<IStaffService, StaffService>();
        services.AddScoped<ILeaveRequestService, LeaveRequestService>();
        services.AddScoped<IPayrollService, PayrollService>();
        // Attendance
        services.AddScoped<IAttendanceService, AttendanceService>();
        // Academic
        services.AddScoped<ISubjectService, SubjectService>();
        services.AddScoped<ITimetableService, TimetableService>();
        services.AddScoped<IHomeworkService, HomeworkService>();
        services.AddScoped<IScheduleConfigService, ScheduleConfigService>();
        services.AddScoped<ITimetableGeneratorService, TimetableGeneratorService>();
        // Examination
        services.AddScoped<IExamService, ExamService>();
        services.AddScoped<IMarksService, MarksService>();
        // Fee
        services.AddScoped<IFeeStructureService, FeeStructureService>();
        services.AddScoped<IFeePaymentService, FeePaymentService>();
        // Communication
        services.AddScoped<IAnnouncementService, AnnouncementService>();
        services.AddScoped<IParentMessageService, ParentMessageService>();
        // Library
        services.AddScoped<IBookService, BookService>();
        services.AddScoped<IBookIssueService, BookIssueService>();
        // Transport
        services.AddScoped<IRouteService, RouteService>();
        services.AddScoped<IVehicleService, VehicleService>();
        services.AddScoped<IStudentRouteMappingService, StudentRouteMappingService>();
        // Reporting
        services.AddScoped<IReportingService, ReportingService>();
        // Notification: real email when an SMTP relay is configured, otherwise log-only.
        services.AddScoped<INotificationService, NotificationService>();
        if (!string.IsNullOrWhiteSpace(configuration["Smtp:Host"]))
            services.AddScoped<IDispatchService, SmtpDispatchService>();
        else
            services.AddScoped<IDispatchService, LoggingDispatchService>();

        // In-process events (replaces RabbitMQ/MassTransit): publishers raise events after
        // saving; the dispatcher runs the handlers in the background.
        services.AddSingleton<InProcessEventBus>();
        services.AddSingleton<IEventPublisher>(sp => sp.GetRequiredService<InProcessEventBus>());
        services.AddHostedService<EventDispatcher>();
        services.AddScoped<IEventHandler<UserRegisteredEvent>, UserRegisteredHandler>();
        services.AddScoped<IEventHandler<StudentEnrolledEvent>, StudentEnrolledHandler>();
        services.AddScoped<IEventHandler<FeePaidEvent>, FeePaidHandler>();
        services.AddScoped<IEventHandler<CertificateGeneratedEvent>, CertificateGeneratedHandler>();

        return services;
    }
}
