using System.Reflection;
using SchoolERP.Business.Identity.Services.Interfaces;
using SchoolERP.Business.Notification.Services.Interfaces;
using SchoolERP.Business.Student.Services;
using SchoolERP.Common.Audit;
using SchoolERP.DataAccess.Storage;
using SchoolERP.DataAccess.Student.Entities;
using SchoolERP.DataAccess.Student.Repositories.Interfaces;

namespace SchoolERP.Tests;

public class StudentErasureTests
{
    /// <summary>
    /// A stand-in for any interface: records each call and answers from <see cref="Answers"/>
    /// (by method name), or with a completed task holding the default value.
    /// </summary>
    public class Stub<T> : DispatchProxy where T : class
    {
        public List<(string Method, object?[] Args)> Calls { get; } = [];
        public Dictionary<string, Func<object?[], object?>> Answers { get; } = [];

        public static (T Service, Stub<T> Stub) Create()
        {
            var service = DispatchProxy.Create<T, Stub<T>>();
            return (service, (Stub<T>)(object)service);
        }

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            args ??= [];
            Calls.Add((method!.Name, args));
            var answer = Answers.TryGetValue(method.Name, out var f) ? f(args) : null;
            var type = method.ReturnType;
            if (type == typeof(void)) return null;
            if (type == typeof(Task)) return Task.CompletedTask;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var inner = type.GetGenericArguments()[0];
                var value = answer ?? (inner.IsValueType ? Activator.CreateInstance(inner) : null);
                return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(inner).Invoke(null, [value]);
            }
            return answer;
        }

        public int Count(string method) => Calls.Count(c => c.Method == method);
    }

    private sealed class Fixture
    {
        public readonly StudentProfile Student = new()
        {
            Id = Guid.NewGuid(), LinkedUserId = Guid.NewGuid(), ParentUserId = Guid.NewGuid(),
            AdmissionNumber = "ADM-2024-017", FullName = "Asha Verma", Gender = "Female",
            ClassId = "5", SectionId = "A", ParentEmail = "parent@example.com", ParentPhone = "9800000000",
            Status = StudentStatuses.TransferredOut,
        };
        public int OtherChildren;
        public readonly Stub<IStudentRepository> Students;
        public readonly Stub<IUserService> Users;
        public readonly Stub<INotificationService> Notifications;
        public readonly Stub<IBlobStorageService> Files;
        public readonly Stub<IAuditTrail> Audit;
        public readonly StudentErasureService Service;

        public Fixture()
        {
            (var students, Students) = Stub<IStudentRepository>.Create();
            (var users, Users) = Stub<IUserService>.Create();
            (var notifications, Notifications) = Stub<INotificationService>.Create();
            (var files, Files) = Stub<IBlobStorageService>.Create();
            (var audit, Audit) = Stub<IAuditTrail>.Create();

            Students.Answers["FindByIdAsync"] = _ => Student;
            Students.Answers["CountOtherChildrenOfParentAsync"] = _ => OtherChildren;
            Users.Answers["ErasePersonalDataAsync"] = a => a[0]!.Equals(Student.LinkedUserId) ? "asha@school.local" : "parent-login@example.com";
            Files.Answers["DeleteByPrefixAsync"] = _ => 1;
            Service = new StudentErasureService(students, users, notifications, files, audit);
        }

        public Task<StudentErasureResult> Erase(string confirm) =>
            Service.EraseAsync(Student.Id, confirm, Guid.NewGuid().ToString(), "Principal");
    }

    [Fact]
    public async Task A_current_pupil_cannot_be_erased()
    {
        var f = new Fixture();
        f.Student.Status = StudentStatuses.Active;

        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Erase("ADM-2024-017"));
        Assert.Equal(0, f.Students.Count("AnonymizeAsync"));
        Assert.Equal(0, f.Users.Count("ErasePersonalDataAsync"));
    }

    [Fact]
    public async Task The_admission_number_must_be_typed_to_confirm()
    {
        var f = new Fixture();

        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Erase("ADM-2024-018"));
        Assert.Equal(0, f.Students.Count("AnonymizeAsync"));
        Assert.Equal(0, f.Files.Count("DeleteByPrefixAsync"));
    }

    [Fact]
    public async Task Erasing_twice_is_refused()
    {
        var f = new Fixture();
        f.Student.Status = StudentStatuses.Erased;

        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Erase("ADM-2024-017"));
    }

    [Fact]
    public async Task A_former_pupil_is_erased_with_their_logins_documents_and_messages()
    {
        var f = new Fixture();

        var result = await f.Erase(" adm-2024-017 ");

        Assert.Equal(1, f.Students.Count("AnonymizeAsync"));
        Assert.True(result.StudentLoginErased);
        Assert.True(result.ParentLoginErased);
        Assert.Equal(2, f.Users.Count("ErasePersonalDataAsync"));

        var prefixes = f.Files.Calls.Where(c => c.Method == "DeleteByPrefixAsync").Select(c => $"{c.Args[0]}/{c.Args[1]}").ToList();
        Assert.Contains($"transfer-certificates/{f.Student.Id}/", prefixes);
        Assert.Contains($"report-cards/{f.Student.Id}/", prefixes);
        Assert.DoesNotContain(prefixes, p => p.StartsWith("fee-receipts"));   // kept for the accounts

        var forgotten = (IEnumerable<string>)f.Notifications.Calls.Single(c => c.Method == "ForgetRecipientsAsync").Args[0]!;
        Assert.Equal(["parent@example.com", "9800000000", "asha@school.local", "parent-login@example.com"], forgotten);

        var audit = (AuditRecord)f.Audit.Calls.Single().Args[0]!;
        Assert.Equal("student.personal-data-erased", audit.Action);
        Assert.DoesNotContain("Asha", audit.Detail);   // the trail must not keep what was erased
    }

    [Fact]
    public async Task A_parent_login_that_still_serves_a_sibling_is_kept()
    {
        var f = new Fixture { OtherChildren = 1 };

        var result = await f.Erase("ADM-2024-017");

        Assert.False(result.ParentLoginErased);
        Assert.Equal(1, f.Users.Count("ErasePersonalDataAsync"));
        Assert.Equal(f.Student.LinkedUserId, f.Users.Calls.Single().Args[0]);
    }
}
