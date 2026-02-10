using TutorsWorldBackend.DAL;
using TutorsWorldBackend.models;

namespace TutorsWorldBackend.Services
{
    public class StudentRegistrationService
    {
        private readonly StudentDAL _studentDAL;
        private readonly GardianDAL _gardianDAL;
        private readonly UsersDAL _usersDAL;

        public StudentRegistrationService(IConfiguration config)
        {
            string cs = config.GetConnectionString("DefaultConnection");
            _studentDAL = new StudentDAL(cs);
            _gardianDAL = new GardianDAL(cs);
            _usersDAL = new UsersDAL(cs);
        }

        public async Task<ServiceResult> Register(StudentRegistrationVM model)
        {
            if (await _usersDAL.UsernameExistsAsync(model.Student.Username))
                return new ServiceResult { Success = false, Message = "Student username already exists" };

            long? gardianId = null;

            if (model.Gardian != null)
            {
                if (await _usersDAL.UsernameExistsAsync(model.Gardian.Username))
                    return new ServiceResult { Success = false, Message = "Gardian username already exists" };

                gardianId = await _gardianDAL.SaveAsync(model.Gardian);
            }

            model.Student.GardianId = gardianId?.ToString();
            long studentId = await _studentDAL.SaveAsync(model.Student);

            await _usersDAL.SaveUserAsync(model.Student.Username, model.Student.Password, 1, studentId, null);

            if (model.Gardian != null)
                await _usersDAL.SaveUserAsync(model.Gardian.Username, model.Gardian.Password, 2, null, gardianId);

            return new ServiceResult { Success = true, Message = "Registration successful" };
        }
    }


}
