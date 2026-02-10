using Microsoft.Data.SqlClient;
using System.Data;
using Dapper;
using TutorsWorldBackend.models;

namespace TutorsWorldBackend.DAL
{
    public class UsersDAL
    {
        private readonly string _connectionString;

        public UsersDAL(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            using var db = new SqlConnection(_connectionString);

            int count = await db.ExecuteScalarAsync<int>(
                "sp_CheckUsernameExists",
                new { Username = username },
                commandType: CommandType.StoredProcedure);

            return count > 0;
        }

        public async Task SaveUserAsync(string username, string password, int type, long? studentId, long? parentId)
        {
            using var db = new SqlConnection(_connectionString);

            await db.ExecuteAsync("sp_InsertUser", new
            {
                Username = username,
                Password = password,
                Type = type,
                StudentId = studentId,
                ParentId = parentId
            }, commandType: CommandType.StoredProcedure);
        }

        public async Task<LoginUser?> GetUserDatabyusername(string username)
        {
            using var db = new SqlConnection(_connectionString);

            // Use QueryFirstOrDefaultAsync to get the actual row data
            var user = await db.QueryFirstOrDefaultAsync<LoginUser>(
                "sp_GetDatabyUsername",
                new { Username = username },
                commandType: CommandType.StoredProcedure);

            return user;
        }

        public async Task<StudentWithGardianVM?> GetStudentDataByIdAsync(long id)
        {
            using var db = new SqlConnection(_connectionString);

            using var multi = await db.QueryMultipleAsync(
                "sp_GetStudentDetailsById",
                new { StudentId = id },
                commandType: CommandType.StoredProcedure
            );

            var student = await multi.ReadFirstOrDefaultAsync<Student>();
            var gardian = await multi.ReadFirstOrDefaultAsync<Gardian>();

            if (student != null && !string.IsNullOrEmpty(student.ProfileImgPath))
            {
                try
                {
                    if (System.IO.File.Exists(student.ProfileImgPath))
                    {
                        student.ProfileImgBytes = await System.IO.File.ReadAllBytesAsync(student.ProfileImgPath);
                    }
                }
                catch (Exception ex)
                {
                    // Log error if file reading fails
                    Console.WriteLine($"Error reading image: {ex.Message}");
                }
            }

            return new StudentWithGardianVM
            {
                Student = student,
                Gardian = gardian
            };
        }

        public async Task<dynamic> GetGuardianDataByIdAsync(long id)
        {
            using var db = new SqlConnection(_connectionString);
            return await db.QueryFirstOrDefaultAsync<dynamic>("sp_GetGuardianDetailsById",
                new { GuardianId = id }, commandType: CommandType.StoredProcedure);
        }

        public async Task<long> HireTutorAsync(long studentId, long tutorId, long? guardianId, long? hiredByStudentId)
        {
            try
            {
                using var db = new SqlConnection(_connectionString);
                return await db.ExecuteScalarAsync<long>(
                    "sp_InsertHiredTutor",
                    new
                    {
                        StudentId = studentId,
                        TutorId = tutorId,
                        HirebyGardianId = guardianId,
                        HirebyStudentId = hiredByStudentId
                    },
                    commandType: CommandType.StoredProcedure
                );
            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred while hiring the tutor.", ex);
            }
        }

        public async Task<TutorProfile> GetTutorFullDetailsAsync(long id)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                using (var multi = await db.QueryMultipleAsync("GetTutorDetailsById",
                                       new { TutorID = id },
                                       commandType: CommandType.StoredProcedure))
                {
                    // 1. Read the main profile
                    var tutor = await multi.ReadFirstOrDefaultAsync<TutorProfile>();
                    if (tutor == null) return null;

                    // 2. Read related collections
                    tutor.Classes = (await multi.ReadAsync<TutorClasses>()).ToList();
                    tutor.Experiences = (await multi.ReadAsync<ExperienceDetail>()).ToList();
                    tutor.Qualifications = (await multi.ReadAsync<QualificationDetail>()).ToList();

                    // 3. Handle File conversions (Profile Image)
                    if (!string.IsNullOrEmpty(tutor.ProfileImgPath) && System.IO.File.Exists(tutor.ProfileImgPath))
                    {
                        tutor.ProfileImgBytes = await System.IO.File.ReadAllBytesAsync(tutor.ProfileImgPath);
                    }

                    // 4. Handle File conversions (Resume)
                    if (!string.IsNullOrEmpty(tutor.ResumePath) && System.IO.File.Exists(tutor.ResumePath))
                    {
                        tutor.ResumeBytes = await System.IO.File.ReadAllBytesAsync(tutor.ResumePath);
                    }

                    return tutor;
                }
            }
        }

        public async Task<IEnumerable<dynamic>> GetConnectionsAsync(long currentId, string role)
        {
            using var db = new SqlConnection(_connectionString);
            return await db.QueryAsync<dynamic>(
                "sp_GetHiredConnections",
                new { CurrentId = currentId, Role = role },
                commandType: CommandType.StoredProcedure
            );
        }
    }

}
