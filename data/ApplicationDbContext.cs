

using Microsoft.EntityFrameworkCore;
using TutorsWorldBackend.models; // Ensure this matches your DTO/Model namespace

namespace TutorsWorldBackend.data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // These become your SQL Tables
        public DbSet<TutorDTO> Tutors { get; set; }
        public DbSet<QualificationDetail> Qualifications { get; set; }
        public DbSet<ExperienceDetail> Experiences { get; set; }
    }
}
