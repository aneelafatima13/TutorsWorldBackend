
using Microsoft.Data.SqlClient;
using System.Data;
using Dapper;
using TutorsWorldBackend.Interface;
using TutorsWorldBackend.models;

namespace TutorsWorldBackend.DAL
{
    public class GardianDAL : IRepository<Gardian>
    {
        private readonly string _connectionString;

        public GardianDAL(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<long> SaveAsync(Gardian gardian)
        {
            using var db = new SqlConnection(_connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("@FullName", gardian.FullName);
            parameters.Add("@CNIC", gardian.CNIC);
            parameters.Add("@Gender", gardian.ParentGender);
            parameters.Add("@MaritalStatus", gardian.ParentMaritalStatus);
            parameters.Add("@Age", gardian.Age);
            parameters.Add("@DOB", gardian.DOB);
            parameters.Add("@ContactNo", gardian.ContactNo);
            parameters.Add("@Religion", gardian.Religion);
            parameters.Add("@GardianId", dbType: DbType.Int64, direction: ParameterDirection.Output);

            await db.ExecuteAsync("sp_InsertGardian", parameters, commandType: CommandType.StoredProcedure);

            return parameters.Get<long>("@GardianId");
        }


    }

}
