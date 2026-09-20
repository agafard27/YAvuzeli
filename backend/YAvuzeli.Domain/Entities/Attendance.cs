namespace YAvuzeli.Domain.Entities;

public class Attendance
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }
    public Student Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
    public DateTime AttendanceDate { get; set; }
    public bool IsPresent { get; set; }
}
