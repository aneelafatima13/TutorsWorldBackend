using Dapper;
using System.Data;
using Microsoft.Data.SqlClient;
using TutorsWorldBackend.Interface;
using TutorsWorldBackend.models;

namespace TutorsWorldBackend.DAL
{
    public class StudentDAL : IRepository<Student>
    {
        private readonly string _connectionString;

        public StudentDAL(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<long> SaveAsync(Student student)
        {
            using var db = new SqlConnection(_connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("@FullName", student.FullName);
            parameters.Add("@CNIC", student.CNIC);
            parameters.Add("@BFormNo", student.BFormNo);
            parameters.Add("@Gender", student.StudentGender);
            parameters.Add("@MaritalStatus", student.StudentMaritalStatus);
            parameters.Add("@Age", student.Age);
            parameters.Add("@DOB", student.DOB);
            parameters.Add("@GardianId", student.GardianId);
            parameters.Add("@StudentId", dbType: DbType.Int64, direction: ParameterDirection.Output);

            await db.ExecuteAsync("sp_InsertStudent", parameters, commandType: CommandType.StoredProcedure);

            return parameters.Get<long>("@StudentId");
        }

       
    }

}
