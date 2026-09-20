using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using YAvuzeli.Domain.Entities;
using YAvuzeli.Infrastructure.Data;

namespace YAvuzeli.Client.Services;

public sealed record TeacherRow(Guid Id, string FirstName, string LastName, string? Phone, string? Email, bool IsActive);
public sealed record CourseRow(Guid Id, string Title, string? Description, Guid? TeacherId, string TeacherName, bool IsActive);
public sealed record PaymentRow(Guid Id, Guid StudentId, string StudentName, decimal Amount, DateTime PaymentDate, string PaymentMethod, string? Note);
public sealed record AttendanceRow(Guid Id, Guid StudentId, string StudentName, Guid CourseId, string CourseTitle, DateTime AttendanceDate, bool IsPresent);
public sealed record EnrollmentRow(Guid Id, Guid StudentId, string StudentName, Guid CourseId, string CourseTitle, DateTime EnrollmentDate, decimal Fee, bool IsPaid);
public sealed record ScheduleRow(Guid Id, Guid CourseId, string CourseTitle, DayOfWeek DayOfWeek, string DayName, TimeSpan StartTime, TimeSpan EndTime, string? Room);
public sealed record LookupRow(Guid Id, string Name);
public sealed record DashboardStats(int Students, int Teachers, int Courses, int Payments, decimal Revenue, int AttendanceCount, int PresentCount);

public sealed class SchoolStore
{
    public string DatabasePath { get; }

    public SchoolStore(string? dataDirectory = null)
    {
        var directory = dataDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YAvuzeli");
        DatabasePath = Path.Combine(directory, "yavuzeli.db");
    }

    public Task<List<TeacherRow>> GetTeachersAsync() => WithDbAsync(async db =>
        await db.Teachers.AsNoTracking().OrderBy(x => x.FirstName).ThenBy(x => x.LastName)
            .Select(x => new TeacherRow(x.Id, x.FirstName, x.LastName, x.Phone, x.Email, x.IsActive)).ToListAsync());

    public Task<TeacherRow> SaveTeacherAsync(Guid? id, string firstName, string lastName, string? phone, string? email, bool isActive) =>
        WithDbAsync(async db =>
        {
            firstName = Required(firstName, "Ad");
            lastName = Required(lastName, "Soyad");
            Teacher teacher;
            if (id is { } existing)
            {
                teacher = await db.Teachers.FindAsync(existing) ?? throw new KeyNotFoundException("Öğretmen bulunamadı.");
            }
            else
            {
                teacher = new Teacher();
                db.Teachers.Add(teacher);
            }

            teacher.FirstName = firstName;
            teacher.LastName = lastName;
            teacher.Phone = BlankToNull(phone);
            teacher.Email = BlankToNull(email);
            teacher.IsActive = isActive;
            await db.SaveChangesAsync();
            return new TeacherRow(teacher.Id, teacher.FirstName, teacher.LastName, teacher.Phone, teacher.Email, teacher.IsActive);
        });

    public Task DeleteTeacherAsync(Guid id) => WithDbAsync(async db =>
    {
        var teacher = await db.Teachers.FindAsync(id) ?? throw new KeyNotFoundException("Öğretmen bulunamadı.");
        db.Teachers.Remove(teacher);
        await db.SaveChangesAsync();
        return true;
    });

    public Task<List<CourseRow>> GetCoursesAsync() => WithDbAsync(async db =>
    {
        var teachers = await db.Teachers.AsNoTracking().ToDictionaryAsync(x => x.Id, x => $"{x.FirstName} {x.LastName}");
        var courses = await db.Courses.AsNoTracking().OrderBy(x => x.Title).ToListAsync();
        return courses.Select(x => new CourseRow(x.Id, x.Title, x.Description, x.TeacherId,
            x.TeacherId is { } teacherId && teachers.TryGetValue(teacherId, out var name) ? name : "Atanmamış",
            x.IsActive)).ToList();
    });

    public Task<CourseRow> SaveCourseAsync(Guid? id, string title, string? description, Guid? teacherId, bool isActive) =>
        WithDbAsync(async db =>
        {
            title = Required(title, "Ders adı");
            Course course;
            if (id is { } existing)
            {
                course = await db.Courses.FindAsync(existing) ?? throw new KeyNotFoundException("Ders bulunamadı.");
            }
            else
            {
                course = new Course();
                db.Courses.Add(course);
            }

            course.Title = title;
            course.Description = BlankToNull(description);
            course.TeacherId = teacherId == Guid.Empty ? null : teacherId;
            course.IsActive = isActive;
            await db.SaveChangesAsync();
            var teacherName = "Atanmamış";
            if (course.TeacherId is { } tid)
            {
                var teacher = await db.Teachers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == tid);
                if (teacher is not null) teacherName = $"{teacher.FirstName} {teacher.LastName}";
            }
            return new CourseRow(course.Id, course.Title, course.Description, course.TeacherId, teacherName, course.IsActive);
        });

    public Task DeleteCourseAsync(Guid id) => WithDbAsync(async db =>
    {
        var course = await db.Courses.FindAsync(id) ?? throw new KeyNotFoundException("Ders bulunamadı.");
        db.Courses.Remove(course);
        await db.SaveChangesAsync();
        return true;
    });

    public Task<List<PaymentRow>> GetPaymentsAsync() => WithDbAsync(async db =>
    {
        var students = await StudentLookupMapAsync(db);
        var payments = await db.Payments.AsNoTracking().OrderByDescending(x => x.PaymentDate).ToListAsync();
        return payments.Select(x => new PaymentRow(x.Id, x.StudentId, students.GetValueOrDefault(x.StudentId, "Öğrenci yok"),
            x.Amount, x.PaymentDate, x.PaymentMethod, x.Note)).ToList();
    });

    public Task<PaymentRow> SavePaymentAsync(Guid? id, Guid studentId, decimal amount, DateTime paymentDate, string method, string? note) =>
        WithDbAsync(async db =>
        {
            if (studentId == Guid.Empty) throw new InvalidOperationException("Öğrenci seçin.");
            if (amount <= 0) throw new InvalidOperationException("Tutar sıfırdan büyük olmalı.");
            method = Required(method, "Ödeme yöntemi");
            Payment payment;
            if (id is { } existing)
            {
                payment = await db.Payments.FindAsync(existing) ?? throw new KeyNotFoundException("Ödeme bulunamadı.");
            }
            else
            {
                payment = new Payment();
                db.Payments.Add(payment);
            }

            payment.StudentId = studentId;
            payment.Amount = amount;
            payment.PaymentDate = paymentDate.Date;
            payment.PaymentMethod = method;
            payment.Note = BlankToNull(note);
            await db.SaveChangesAsync();
            var students = await StudentLookupMapAsync(db);
            return new PaymentRow(payment.Id, payment.StudentId, students.GetValueOrDefault(payment.StudentId, "Öğrenci yok"),
                payment.Amount, payment.PaymentDate, payment.PaymentMethod, payment.Note);
        });

    public Task DeletePaymentAsync(Guid id) => WithDbAsync(async db =>
    {
        var payment = await db.Payments.FindAsync(id) ?? throw new KeyNotFoundException("Ödeme bulunamadı.");
        db.Payments.Remove(payment);
        await db.SaveChangesAsync();
        return true;
    });

    public Task<List<AttendanceRow>> GetAttendancesAsync() => WithDbAsync(async db =>
    {
        var students = await StudentLookupMapAsync(db);
        var courses = await CourseLookupMapAsync(db);
        var items = await db.Attendances.AsNoTracking().OrderByDescending(x => x.AttendanceDate).ToListAsync();
        return items.Select(x => new AttendanceRow(x.Id, x.StudentId, students.GetValueOrDefault(x.StudentId, "Öğrenci yok"),
            x.CourseId, courses.GetValueOrDefault(x.CourseId, "Ders yok"), x.AttendanceDate, x.IsPresent)).ToList();
    });

    public Task<AttendanceRow> SaveAttendanceAsync(Guid? id, Guid studentId, Guid courseId, DateTime date, bool isPresent) =>
        WithDbAsync(async db =>
        {
            if (studentId == Guid.Empty) throw new InvalidOperationException("Öğrenci seçin.");
            if (courseId == Guid.Empty) throw new InvalidOperationException("Ders seçin.");
            Attendance attendance;
            if (id is { } existing)
            {
                attendance = await db.Attendances.FindAsync(existing) ?? throw new KeyNotFoundException("Yoklama bulunamadı.");
            }
            else
            {
                attendance = new Attendance();
                db.Attendances.Add(attendance);
            }

            attendance.StudentId = studentId;
            attendance.CourseId = courseId;
            attendance.AttendanceDate = date.Date;
            attendance.IsPresent = isPresent;
            await db.SaveChangesAsync();
            var students = await StudentLookupMapAsync(db);
            var courses = await CourseLookupMapAsync(db);
            return new AttendanceRow(attendance.Id, attendance.StudentId, students.GetValueOrDefault(attendance.StudentId, "Öğrenci yok"),
                attendance.CourseId, courses.GetValueOrDefault(attendance.CourseId, "Ders yok"), attendance.AttendanceDate, attendance.IsPresent);
        });

    public Task DeleteAttendanceAsync(Guid id) => WithDbAsync(async db =>
    {
        var attendance = await db.Attendances.FindAsync(id) ?? throw new KeyNotFoundException("Yoklama bulunamadı.");
        db.Attendances.Remove(attendance);
        await db.SaveChangesAsync();
        return true;
    });

    public Task<List<EnrollmentRow>> GetEnrollmentsAsync() => WithDbAsync(async db =>
    {
        var students = await StudentLookupMapAsync(db);
        var courses = await CourseLookupMapAsync(db);
        var items = await db.Enrollments.AsNoTracking().OrderByDescending(x => x.EnrollmentDate).ToListAsync();
        return items.Select(x => new EnrollmentRow(x.Id, x.StudentId, students.GetValueOrDefault(x.StudentId, "Öğrenci yok"),
            x.CourseId, courses.GetValueOrDefault(x.CourseId, "Ders yok"), x.EnrollmentDate, x.Fee, x.IsPaid)).ToList();
    });

    public Task<EnrollmentRow> SaveEnrollmentAsync(Guid? id, Guid studentId, Guid courseId, DateTime date, decimal fee, bool isPaid) =>
        WithDbAsync(async db =>
        {
            if (studentId == Guid.Empty) throw new InvalidOperationException("Öğrenci seçin.");
            if (courseId == Guid.Empty) throw new InvalidOperationException("Ders seçin.");
            if (fee < 0) throw new InvalidOperationException("Ücret negatif olamaz.");
            Enrollment enrollment;
            if (id is { } existing)
            {
                enrollment = await db.Enrollments.FindAsync(existing) ?? throw new KeyNotFoundException("Kayıt bulunamadı.");
            }
            else
            {
                enrollment = new Enrollment();
                db.Enrollments.Add(enrollment);
            }

            enrollment.StudentId = studentId;
            enrollment.CourseId = courseId;
            enrollment.EnrollmentDate = date.Date;
            enrollment.Fee = fee;
            enrollment.IsPaid = isPaid;
            await db.SaveChangesAsync();
            var students = await StudentLookupMapAsync(db);
            var courses = await CourseLookupMapAsync(db);
            return new EnrollmentRow(enrollment.Id, enrollment.StudentId, students.GetValueOrDefault(enrollment.StudentId, "Öğrenci yok"),
                enrollment.CourseId, courses.GetValueOrDefault(enrollment.CourseId, "Ders yok"), enrollment.EnrollmentDate, enrollment.Fee, enrollment.IsPaid);
        });

    public Task DeleteEnrollmentAsync(Guid id) => WithDbAsync(async db =>
    {
        var enrollment = await db.Enrollments.FindAsync(id) ?? throw new KeyNotFoundException("Kayıt bulunamadı.");
        db.Enrollments.Remove(enrollment);
        await db.SaveChangesAsync();
        return true;
    });

    public Task<List<ScheduleRow>> GetScheduleAsync() => WithDbAsync(async db =>
    {
        var courses = await CourseLookupMapAsync(db);
        var items = await db.ScheduleItems.AsNoTracking().ToListAsync();
        return items.OrderBy(x => x.DayOfWeek).ThenBy(x => x.StartTime)
            .Select(x => new ScheduleRow(x.Id, x.CourseId, courses.GetValueOrDefault(x.CourseId, "Ders yok"),
            x.DayOfWeek, DayName(x.DayOfWeek), x.StartTime, x.EndTime, x.Room)).ToList();
    });

    public Task<ScheduleRow> SaveScheduleAsync(Guid? id, Guid courseId, DayOfWeek dayOfWeek, TimeSpan startTime, TimeSpan endTime, string? room) =>
        WithDbAsync(async db =>
        {
            if (courseId == Guid.Empty) throw new InvalidOperationException("Ders seçin.");
            if (endTime <= startTime) throw new InvalidOperationException("Bitiş saati başlangıçtan sonra olmalı.");
            ScheduleItem item;
            if (id is { } existing)
            {
                item = await db.ScheduleItems.FindAsync(existing) ?? throw new KeyNotFoundException("Program kaydı bulunamadı.");
            }
            else
            {
                item = new ScheduleItem();
                db.ScheduleItems.Add(item);
            }

            item.CourseId = courseId;
            item.DayOfWeek = dayOfWeek;
            item.StartTime = startTime;
            item.EndTime = endTime;
            item.Room = BlankToNull(room);
            await db.SaveChangesAsync();
            var courses = await CourseLookupMapAsync(db);
            return new ScheduleRow(item.Id, item.CourseId, courses.GetValueOrDefault(item.CourseId, "Ders yok"),
                item.DayOfWeek, DayName(item.DayOfWeek), item.StartTime, item.EndTime, item.Room);
        });

    public Task DeleteScheduleAsync(Guid id) => WithDbAsync(async db =>
    {
        var item = await db.ScheduleItems.FindAsync(id) ?? throw new KeyNotFoundException("Program kaydı bulunamadı.");
        db.ScheduleItems.Remove(item);
        await db.SaveChangesAsync();
        return true;
    });

    public Task<List<LookupRow>> GetStudentLookupsAsync() => WithDbAsync(async db =>
        await db.Students.AsNoTracking().OrderBy(x => x.FirstName).ThenBy(x => x.LastName)
            .Select(x => new LookupRow(x.Id, x.FirstName + " " + x.LastName)).ToListAsync());

    public Task<List<LookupRow>> GetTeacherLookupsAsync() => WithDbAsync(async db =>
        await db.Teachers.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.FirstName).ThenBy(x => x.LastName)
            .Select(x => new LookupRow(x.Id, x.FirstName + " " + x.LastName)).ToListAsync());

    public Task<List<LookupRow>> GetCourseLookupsAsync() => WithDbAsync(async db =>
        await db.Courses.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Title)
            .Select(x => new LookupRow(x.Id, x.Title)).ToListAsync());

    public Task<DashboardStats> GetDashboardStatsAsync() => WithDbAsync(async db =>
    {
        var attendance = await db.Attendances.AsNoTracking().ToListAsync();
        var payments = await db.Payments.AsNoTracking().Select(x => x.Amount).ToListAsync();
        return new DashboardStats(
            await db.Students.CountAsync(),
            await db.Teachers.CountAsync(x => x.IsActive),
            await db.Courses.CountAsync(x => x.IsActive),
            await db.Payments.CountAsync(),
            payments.Sum(),
            attendance.Count,
            attendance.Count(x => x.IsPresent));
    });

    private Task<T> WithDbAsync<T>(Func<AppDbContext, Task<T>> action) => Task.Run(async () =>
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
        var connection = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath, ForeignKeys = true, DefaultTimeout = 15
        }.ToString();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        await EnsureExtraSchemaAsync(db);
        return await action(db);
    });

    private static async Task EnsureExtraSchemaAsync(AppDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "ScheduleItems" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_ScheduleItems" PRIMARY KEY,
                "CourseId" TEXT NOT NULL,
                "DayOfWeek" INTEGER NOT NULL,
                "StartTime" TEXT NOT NULL,
                "EndTime" TEXT NOT NULL,
                "Room" TEXT NULL,
                "CreatedAt" TEXT NOT NULL,
                CONSTRAINT "FK_ScheduleItems_Courses_CourseId" FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE RESTRICT
            );
            """);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_ScheduleItems_CourseId_DayOfWeek_StartTime"
            ON "ScheduleItems" ("CourseId", "DayOfWeek", "StartTime");
            """);
    }

    private static async Task<Dictionary<Guid, string>> StudentLookupMapAsync(AppDbContext db) =>
        await db.Students.AsNoTracking().ToDictionaryAsync(x => x.Id, x => $"{x.FirstName} {x.LastName}");

    private static async Task<Dictionary<Guid, string>> CourseLookupMapAsync(AppDbContext db) =>
        await db.Courses.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Title);

    private static string Required(string? value, string field)
    {
        value = value?.Trim();
        if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException($"{field} zorunludur.");
        return value;
    }

    private static string? BlankToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string DayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "Pazartesi",
        DayOfWeek.Tuesday => "Salı",
        DayOfWeek.Wednesday => "Çarşamba",
        DayOfWeek.Thursday => "Perşembe",
        DayOfWeek.Friday => "Cuma",
        DayOfWeek.Saturday => "Cumartesi",
        DayOfWeek.Sunday => "Pazar",
        _ => day.ToString()
    };
}
