using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using SchoolERP.DataAccess.Academic;
using SchoolERP.DataAccess.Academic.Repositories;
using SchoolERP.DataAccess.Academic.Repositories.Interfaces;
using SchoolERP.DataAccess.Attendance;
using SchoolERP.DataAccess.Attendance.Repositories;
using SchoolERP.DataAccess.Attendance.Repositories.Interfaces;
using SchoolERP.DataAccess.Communication;
using SchoolERP.DataAccess.Communication.Repositories;
using SchoolERP.DataAccess.Communication.Repositories.Interfaces;
using SchoolERP.DataAccess.Examination;
using SchoolERP.DataAccess.Examination.Repositories;
using SchoolERP.DataAccess.Examination.Repositories.Interfaces;
using SchoolERP.DataAccess.Fee;
using SchoolERP.DataAccess.Fee.Repositories;
using SchoolERP.DataAccess.Fee.Repositories.Interfaces;
using SchoolERP.DataAccess.Identity;
using SchoolERP.DataAccess.Identity.Repositories;
using SchoolERP.DataAccess.Identity.Repositories.Interfaces;
using SchoolERP.DataAccess.Library;
using SchoolERP.DataAccess.Library.Repositories;
using SchoolERP.DataAccess.Library.Repositories.Interfaces;
using SchoolERP.DataAccess.Notification;
using SchoolERP.DataAccess.Notification.Repositories;
using SchoolERP.DataAccess.Notification.Repositories.Interfaces;
using SchoolERP.DataAccess.Reporting;
using SchoolERP.DataAccess.Reporting.Repositories;
using SchoolERP.DataAccess.Reporting.Repositories.Interfaces;
using SchoolERP.DataAccess.Staff;
using SchoolERP.DataAccess.Staff.Repositories;
using SchoolERP.DataAccess.Staff.Repositories.Interfaces;
using SchoolERP.DataAccess.Storage;
using SchoolERP.DataAccess.Student;
using SchoolERP.DataAccess.Student.Repositories;
using SchoolERP.DataAccess.Student.Repositories.Interfaces;
using SchoolERP.DataAccess.Transport;
using SchoolERP.DataAccess.Transport.Repositories;
using SchoolERP.DataAccess.Transport.Repositories.Interfaces;

namespace SchoolERP.DataAccess;

public static class DataAccessServiceCollectionExtensions
{
    /// <summary>
    /// Every module keeps its own database (identity, student, fee, ...) on the same TiDB/MySQL
    /// server -- the same databases the microservices used, so no data migration is needed.
    /// </summary>
    public static readonly IReadOnlyList<(Type ContextType, string Database)> Modules = new (Type, string)[]
    {
        (typeof(IdentityDbContext), "identity"),
        (typeof(StudentDbContext), "student"),
        (typeof(StaffDbContext), "staff"),
        (typeof(AttendanceDbContext), "attendance"),
        (typeof(AcademicDbContext), "academic"),
        (typeof(ExaminationDbContext), "examination"),
        (typeof(FeeDbContext), "fee"),
        (typeof(CommunicationDbContext), "communication"),
        (typeof(LibraryDbContext), "library"),
        (typeof(TransportDbContext), "transport"),
        (typeof(NotificationDbContext), "notification"),
        (typeof(ReportingDbContext), "reporting"),
    };

    // TiDB speaks the MySQL 8 wire protocol; pinning the version avoids a connection at startup.
    private static readonly MySqlServerVersion ServerVersion = new(new Version(8, 0, 11));

    public static IServiceCollection AddDataAccess(this IServiceCollection services, IConfiguration configuration)
    {
        AddContext<IdentityDbContext>(services, configuration, "identity");
        AddContext<StudentDbContext>(services, configuration, "student");
        AddContext<StaffDbContext>(services, configuration, "staff");
        AddContext<AttendanceDbContext>(services, configuration, "attendance");
        AddContext<AcademicDbContext>(services, configuration, "academic");
        AddContext<ExaminationDbContext>(services, configuration, "examination");
        AddContext<FeeDbContext>(services, configuration, "fee");
        AddContext<CommunicationDbContext>(services, configuration, "communication");
        AddContext<LibraryDbContext>(services, configuration, "library");
        AddContext<TransportDbContext>(services, configuration, "transport");
        AddContext<NotificationDbContext>(services, configuration, "notification");
        AddContext<ReportingDbContext>(services, configuration, "reporting");

        // Identity
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        // Student
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<IStudentDocumentRepository, StudentDocumentRepository>();
        services.AddScoped<ITransferCertificateRepository, TransferCertificateRepository>();
        // Staff
        services.AddScoped<IStaffRepository, StaffRepository>();
        services.AddScoped<ILeaveRequestRepository, LeaveRequestRepository>();
        services.AddScoped<IPayoutRepository, PayoutRepository>();
        services.AddScoped<IStaffAttendanceRepository, StaffAttendanceRepository>();
        // Attendance
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<IMonthlyAttendanceSummaryRepository, MonthlyAttendanceSummaryRepository>();
        // Academic
        services.AddScoped<ISubjectRepository, SubjectRepository>();
        services.AddScoped<ITimetableRepository, TimetableRepository>();
        services.AddScoped<IHomeworkRepository, HomeworkRepository>();
        services.AddScoped<IScheduleConfigRepository, ScheduleConfigRepository>();
        // Examination
        services.AddScoped<IExamRepository, ExamRepository>();
        services.AddScoped<IMarksRepository, MarksRepository>();
        // Fee
        services.AddScoped<IFeeStructureRepository, FeeStructureRepository>();
        services.AddScoped<IFeePaymentRepository, FeePaymentRepository>();
        services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
        // Communication
        services.AddScoped<IAnnouncementRepository, AnnouncementRepository>();
        services.AddScoped<IParentMessageRepository, ParentMessageRepository>();
        // Library
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IBookIssueRepository, BookIssueRepository>();
        // Transport
        services.AddScoped<IRouteRepository, RouteRepository>();
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IStudentRouteMappingRepository, StudentRouteMappingRepository>();
        // Notification
        services.AddScoped<INotificationLogRepository, NotificationLogRepository>();
        // Reporting
        services.AddScoped<IReportSnapshotRepository, ReportSnapshotRepository>();

        // Receipts, report cards and transfer certificates (Azure Blob Storage / Azurite).
        services.AddSingleton(_ => new BlobServiceClient(configuration.GetConnectionString("BlobStorage")));
        services.AddScoped<IBlobStorageService, AzureBlobStorageService>();

        return services;
    }

    /// <summary>
    /// Connection string for one module's database: ConnectionStrings:{Module}Db when set
    /// explicitly, otherwise ConnectionStrings:SchoolDb (the shared server) with the module's
    /// database name filled in.
    /// </summary>
    public static string ConnectionStringFor(IConfiguration configuration, string database)
    {
        var moduleKey = char.ToUpperInvariant(database[0]) + database[1..] + "Db";
        var explicitValue = configuration.GetConnectionString(moduleKey);
        if (!string.IsNullOrWhiteSpace(explicitValue))
            return explicitValue;

        var server = configuration.GetConnectionString("SchoolDb")
            ?? throw new InvalidOperationException("ConnectionStrings:SchoolDb is not configured.");
        return new MySqlConnectionStringBuilder(server) { Database = database }.ConnectionString;
    }

    private static void AddContext<TContext>(IServiceCollection services, IConfiguration configuration, string database)
        where TContext : DbContext =>
        services.AddDbContext<TContext>(options => options.UseMySql(ConnectionStringFor(configuration, database), ServerVersion));
}
