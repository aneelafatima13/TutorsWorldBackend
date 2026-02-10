namespace TutorsWorldBackend.models
{
    public class LoginUser
    {
        public long Id { get; set; } // bigint -> long
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public string? Password { get; set; }
        public int Type { get; set; }

        public string? Role => Type switch
        {
            0 => "Tutor",
            1 => "Student",
            2 => "Gardian",
            _ => "Unknown"
        };

        public long? StudentId { get; set; }
        public long? TutorId { get; set; } 
        public long? GardianId { get; set; }
    }

    public class HireRequest
    {
        public long StudentId { get; set; }
        public long TutorId { get; set; }
        public long? GuardianId { get; set; }
        public long? HiredByStudentId { get; set; }
    }
}