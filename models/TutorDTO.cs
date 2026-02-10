using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace TutorsWorldBackend.models
{
    public class TutorDTO
    {
        // --- MANDATORY FIELDS ---
        public string FullName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Gender { get; set; }
        public int Age { get; set; }
        public DateTime DOB { get; set; }
        public string CNIC { get; set; }
        public IFormFile ProfileImg { get; set; } // Required
        public IFormFile ResumePdf { get; set; }
        public string? ProfileImgPath { get; set; }
        public string? ResumePath { get; set; }

        public byte[]? ProfileImgBytes { get; set; }
        public byte[]? ResumeBytes { get; set; }

        public string ContactNo { get; set; }
        public string ContactEmail { get; set; }
        public string Religion { get; set; }
        public string Nationality { get; set; }
        public string MaritalStatus { get; set; }
        public string TeachingSource { get; set; }
        public string FeeType { get; set; }

        // Required

        // --- OPTIONAL FIELDS (Marked with ? to prevent 400 errors) ---

        public string? City { get; set; }
        public string? Province { get; set; }
        public string? Country { get; set; }
        public string? PAddress { get; set; }
        public string? TAddress { get; set; }
        public string? Subjects { get; set; }
        
        public int? TotalExperienceYears { get; set; }

        // --- NESTED DATA ---
        public List<string> Classes { get; set; } = new List<string>();
        public List<QualificationDetail> Qualifications { get; set; } = new List<QualificationDetail>();
        public List<ExperienceDetail> Experiences { get; set; } = new List<ExperienceDetail>();
    }

    public class QualificationDetail
    {
        public string? Institute { get; set; }
        public string? Degree { get; set; }
        public int? PassingYear { get; set; }
        public string? Percentage { get; set; }
        public long TutorID { get; set; } = 0;
    }

    public class ExperienceDetail
    {
        public string? ExpInstitute { get; set; }
        public DateTime? ExpStart { get; set; }
        public DateTime? ExpEnd { get; set; }
        public string? ExpDuration { get; set; }
        public long TutorID { get; set; } = 0;
    }

    public class TutorClasses
    {
        public string? ClassName { get; set; }
        public long TutorID { get; set; } = 0;
    }

    public class PaginatedTutorResponse
    {
        public List<TutorProfile> Tutors { get; set; } = new List<TutorProfile>();
        public int TotalCount { get; set; }
    }

    public class TutorProfile : TutorDTO
    {
        // These lists will hold the data from the other result sets
        public List<TutorClasses> Classes { get; set; } = new List<TutorClasses>();
        public List<QualificationDetail> Qualifications { get; set; } = new List<QualificationDetail>();
        public List<ExperienceDetail> Experiences { get; set; } = new List<ExperienceDetail>();
        public object TutorID { get; internal set; }
    }

    public class TutorFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int RowsPerPage { get; set; } = 9;
        public string SearchTerm { get; set; }
        public string Gender { get; set; }
        public string MaritalStatus { get; set; }
        public string TeachingSources { get; set; }
        public string FeeStructures { get; set; }
        public string Classes { get; set; }
    }
}