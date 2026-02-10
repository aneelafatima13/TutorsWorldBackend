namespace TutorsWorldBackend.models
{
    public abstract class User
    {
        // Private Backing Fields
        private string? _fullName;
        private string? _username;
        private string? _password;
        private string? _cnic;
        private int _age;

        // Public Properties with Getters and Setters
        public string? FullName
        {
            get => _fullName;
            set => _fullName = value;
        }

        public string? Username
        {
            get => _username;
            set => _username = value;
        }

        public string? Password
        {
            get => _password;
            set => _password = value; // In a real app, you would hash this here
        }

        public string? CNIC
        {
            get => _cnic;
            set => _cnic = value?.Replace("-", ""); // Logic: Automatically remove dashes
        }

        public int Age
        {
            get => _age;
            set => _age = (value < 0) ? 0 : value; // Validation: Age cannot be negative
        }

        // Auto-properties for simpler fields
        public string? ContactNo { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? Country { get; set; }
        public string? PAddress { get; set; }
        public string? TAddress { get; set; }
        
        public DateTime DOB { get; set; }
        public string? ContactEmail { get; set; }
        public string? Religion { get; set; }
        public string? Nationality { get; set; }
       
    }

    public class Student : User
    {
        private string? _rollNo;
        private string? _bFormNo;

        public string? RollNo
        {
            get => _rollNo;
            set => _rollNo = value;
        }

        public string? BFormNo
        {
            get => _bFormNo;
            set => _bFormNo = value;
        }

        public string? TargetSubjects { get; set; }
        public string? GardianId { get; set; }
        public string? StudentGender { get; set; }
        public string? StudentMaritalStatus { get; set; }

        public IFormFile? ProfileImg { get; set; }
        public string? ProfileImgPath { get; set; }

        
    }

    public class Gardian : User
    {
        private string? _studentId;
        public string? ParentGender { get; set; }
        public string? ParentMaritalStatus { get; set; }
        public string? StudentId
        {
            get => _studentId;
            set => _studentId = value;
        }
        public long? Id { get; set; }
    }
    public class StudentRegistrationVM
    {
        // Student
        public Student? Student { get; set; }

        // Guardian (nullable if age < 18)
        public Gardian? Gardian { get; set; }
    }

    public class ServiceResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }
}