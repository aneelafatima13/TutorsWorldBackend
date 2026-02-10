using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Data;
using TutorsWorldBackend.models;

namespace TutorsWorldBackend.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class TutorController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly IMemoryCache _cache; // Add this

        public TutorController(IConfiguration config, IMemoryCache cache) // Inject here
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
            _cache = cache;
        }

        [HttpPost("SaveTutor")]
        public async Task<IActionResult> SaveTutor([FromForm] TutorDTO model)
        {
            try
            {
                using (IDbConnection db = new SqlConnection(_connectionString))
                {
                    // --- NEW: Check if Username already exists ---
                    string checkSql = "SELECT COUNT(1) FROM Users WHERE Username = @Username";
                    int exists = await db.ExecuteScalarAsync<int>(checkSql, new { Username = model.Username });

                    if (exists > 0)
                    {
                        // Return a 400 Bad Request with a clear message
                        return BadRequest(new { success = false, message = "Username already taken. Please choose another one." });
                    }
                    string pPath = await SaveFile(model.ProfileImg, model.Username);
                    string rPath = await SaveFile(model.ResumePdf, model.Username);

                    // DataTables setup stays the same...
                    var qualDataTable = new DataTable();
                    qualDataTable.Columns.Add("Institute");
                    qualDataTable.Columns.Add("Degree");
                    qualDataTable.Columns.Add("PassingYear", typeof(int));
                    qualDataTable.Columns.Add("Percentage");
                    model.Qualifications?.ForEach(q => qualDataTable.Rows.Add(q.Institute, q.Degree, q.PassingYear, q.Percentage));

                    var expDataTable = new DataTable();
                    expDataTable.Columns.Add("ExpInstitute");
                    expDataTable.Columns.Add("ExpStart", typeof(DateTime));
                    expDataTable.Columns.Add("ExpEnd", typeof(DateTime)); expDataTable.Columns.Add("ExpDuration");
                    model.Experiences?.ForEach(e => expDataTable.Rows.Add(e.ExpInstitute, e.ExpStart, e.ExpEnd, e.ExpDuration));

                    var classDataTable = new DataTable();
                    classDataTable.Columns.Add("ClassName");
                    model.Classes?.ForEach(c => classDataTable.Rows.Add(c));


                    var parameters = new DynamicParameters();
                    parameters.Add("@FullName", model.FullName);
                    parameters.Add("@CNIC", model.CNIC);
                    parameters.Add("@Gender", model.Gender);
                    parameters.Add("@Age", model.Age);
                    parameters.Add("@DOB", model.DOB);
                    parameters.Add("@Username", model.Username);
                    parameters.Add("@Password", model.Password);
                    parameters.Add("@Religion", model.Religion);
                    parameters.Add("@Nationality", model.Nationality);
                    parameters.Add("@PhoneNo", model.ContactNo);
                    parameters.Add("@Email", model.ContactEmail);
                    parameters.Add("@MaritalStatus", model.MaritalStatus);

                    parameters.Add("@City", model.City);
                    parameters.Add("@Province", model.Province);
                    parameters.Add("@Country", model.Country);

                    parameters.Add("@PAddress", model.PAddress);
                    parameters.Add("@TAddress", model.TAddress);
                    parameters.Add("@TeachingSource", model.TeachingSource);
                    parameters.Add("@FeeType", model.FeeType);
                    parameters.Add("@TotalExperience", model.TotalExperienceYears);

                    parameters.Add("@ProfileImgPath", pPath);
                    parameters.Add("@ResumePath", rPath);

                    parameters.Add("@QualList", qualDataTable.AsTableValuedParameter("[dbo].[QualificationType]"));
                    parameters.Add("@ExpList", expDataTable.AsTableValuedParameter("[dbo].[ExperienceType]"));
                    parameters.Add("@ClassList", classDataTable.AsTableValuedParameter("[dbo].[ClassListType]"));

                    await db.ExecuteAsync("sp_RegisterTutorComplete", parameters, commandType: CommandType.StoredProcedure);

                    // Inside SaveTutor after successful DB execute:
                    var cacheKeysToRemove = new[] { "tutors_page_1_10" }; // Add common keys here
                    foreach (var key in cacheKeysToRemove) _cache.Remove(key);
                    return Ok(new { success = true, message = "Registration Complete!" });
                }

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // Helper method to save files to disk
        private async Task<string> SaveFile(IFormFile file, string username)
        {
            if (file == null) return null;

            // FIX 1: Use a clean Windows path without leading slashes or double forward slashes
            string wwwRoot = @"D:\TutorsWorldUploads";

            if (!Directory.Exists(wwwRoot))
            {
                Directory.CreateDirectory(wwwRoot);
            }

            // Use Path.Combine to ensure correct backslashes (\) are used
            string userFolder = Path.Combine(wwwRoot, username);
            if (!Directory.Exists(userFolder)) Directory.CreateDirectory(userFolder);

            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            string fullPath = Path.Combine(userFolder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // FIX 2: Return the absolute physical path without a leading "/" 
            // This allows System.IO.File.Exists() to find it later.
            return fullPath;
        }

        [HttpGet("GetTutors")]
        public async Task<IActionResult> GetTutors(int pageNumber = 1, int rowsPerPage = 10)
        {
            try
            {
                // Unique key for each page/size combination
                string cacheKey = $"tutors_page_{pageNumber}_{rowsPerPage}";

                if (!_cache.TryGetValue(cacheKey, out object cachedData))
                {
                    using (IDbConnection db = new SqlConnection(_connectionString))
                    {
                        var parameters = new { PageNumber = pageNumber, RowsPerPage = rowsPerPage };
                        using (var multi = await db.QueryMultipleAsync("GetTopTutorsWithPagination",
                                             parameters, commandType: CommandType.StoredProcedure))
                        {
                            var tutors = (await multi.ReadAsync<TutorProfile>()).ToList();
                            var classes = (await multi.ReadAsync<TutorClasses>()).ToList();
                            var experiences = (await multi.ReadAsync<ExperienceDetail>()).ToList();
                            var qualifications = (await multi.ReadAsync<QualificationDetail>()).ToList();
                            var totalCount = await multi.ReadFirstAsync<int>();

                            foreach (var tutor in tutors)
                            {
                                long tId = Convert.ToInt64(tutor.TutorID);
                                tutor.Classes = classes.Where(c => c.TutorID == tId).ToList();
                                tutor.Experiences = experiences.Where(e => e.TutorID == tId).ToList();
                                tutor.Qualifications = qualifications.Where(q => q.TutorID == tId).ToList();

                                if (!string.IsNullOrEmpty(tutor.ProfileImgPath) && System.IO.File.Exists(tutor.ProfileImgPath))
                                    tutor.ProfileImgBytes = await System.IO.File.ReadAllBytesAsync(tutor.ProfileImgPath);

                                if (!string.IsNullOrEmpty(tutor.ResumePath) && System.IO.File.Exists(tutor.ResumePath))
                                {
                                    tutor.ResumeBytes = await System.IO.File.ReadAllBytesAsync(tutor.ResumePath);
                                }
                            }

                            cachedData = new { success = true, data = tutors, totalRecords = totalCount, currentPage = pageNumber, pageSize = rowsPerPage };

                            // Set cache options (e.g., store for 10 minutes)
                            var options = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(10));
                            _cache.Set(cacheKey, cachedData, options);
                        }
                    }
                }
                return Ok(cachedData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("GetTutors")]
        public async Task<IActionResult> GetTutors([FromBody] TutorFilterRequest request)
        {
            try
            {
                string filterKey = $"filter_{request.SearchTerm}_{request.Gender}_{request.PageNumber}";

                if (!_cache.TryGetValue(filterKey, out object cachedResult))
                {
                    using (IDbConnection db = new SqlConnection(_connectionString))
                    {
                        var parameters = new
                        {
                            PageNumber = request.PageNumber,
                            RowsPerPage = request.RowsPerPage,
                            SearchTerm = request.SearchTerm ?? "",
                            Gender = request.Gender ?? "",
                            MaritalStatus = request.MaritalStatus ?? "",
                            TeachingSources = request.TeachingSources ?? "",
                            FeeStructures = request.FeeStructures ?? "",
                            Classes = request.Classes ?? ""
                        };

                        using (var multi = await db.QueryMultipleAsync("GetTopTutorsWithPagination",
                                             parameters, commandType: CommandType.StoredProcedure))
                        {
                            var tutors = (await multi.ReadAsync<TutorProfile>()).ToList();
                            var classes = (await multi.ReadAsync<TutorClasses>()).ToList();
                            var experiences = (await multi.ReadAsync<ExperienceDetail>()).ToList();
                            var qualifications = (await multi.ReadAsync<QualificationDetail>()).ToList();

                            // 1. Store the value in a variable
                            var totalCount = await multi.ReadFirstAsync<int>();

                            foreach (var tutor in tutors)
                            {
                                long tId = Convert.ToInt64(tutor.TutorID);
                                tutor.Classes = classes.Where(c => c.TutorID == tId).ToList();
                                tutor.Experiences = experiences.Where(e => e.TutorID == tId).ToList();
                                tutor.Qualifications = qualifications.Where(q => q.TutorID == tId).ToList();

                                if (!string.IsNullOrEmpty(tutor.ProfileImgPath) && System.IO.File.Exists(tutor.ProfileImgPath))
                                {
                                    tutor.ProfileImgBytes = await System.IO.File.ReadAllBytesAsync(tutor.ProfileImgPath);
                                }
                            }

                            // 2. USE the variable here, NOT the reader (multi)
                            cachedResult = new
                            {
                                success = true,
                                data = tutors,
                                totalRecords = totalCount, // Fixed here
                                currentPage = request.PageNumber,
                                pageSize = request.RowsPerPage
                            };

                            _cache.Set(filterKey, cachedResult, TimeSpan.FromMinutes(5));
                        }
                    }
                }
                return Ok(cachedResult);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
        
        //[HttpGet("GetTutors")]
        //public async Task<IActionResult> GetTutors(int pageNumber = 1, int rowsPerPage = 10)
        //{
        //    try
        //    {
        //        using (IDbConnection db = new SqlConnection(_connectionString))
        //        {
        //            var parameters = new { PageNumber = pageNumber, RowsPerPage = rowsPerPage };

        //            using (var multi = await db.QueryMultipleAsync("GetTopTutorsWithPagination",
        //                                 parameters, commandType: CommandType.StoredProcedure))
        //            {
        //                // Read Result Sets in order defined in SP
        //                var tutors = (await multi.ReadAsync<TutorProfile>()).ToList();
        //                var classes = (await multi.ReadAsync<TutorClasses>()).ToList();
        //                var experiences = (await multi.ReadAsync<ExperienceDetail>()).ToList();
        //                var qualifications = (await multi.ReadAsync<QualificationDetail>()).ToList();
        //                var totalCount = await multi.ReadFirstAsync<int>();

        //                // Map related data to each tutor
        //                foreach (var tutor in tutors)
        //                {
        //                    tutor.Classes = classes.Where(c => c.TutorID == Convert.ToInt64(tutor.TutorID)).ToList();
        //                    tutor.Experiences = experiences.Where(e => e.TutorID == Convert.ToInt64(tutor.TutorID)).ToList();
        //                    tutor.Qualifications = qualifications.Where(q => q.TutorID == Convert.ToInt64(tutor.TutorID)).ToList();
        //                    if (!string.IsNullOrEmpty(tutor.ProfileImgPath) && System.IO.File.Exists(tutor.ProfileImgPath))
        //                    {
        //                        tutor.ProfileImgBytes = await System.IO.File.ReadAllBytesAsync(tutor.ProfileImgPath);
        //                    }

        //                    if (!string.IsNullOrEmpty(tutor.ResumePath) && System.IO.File.Exists(tutor.ResumePath))
        //                    {
        //                        tutor.ResumeBytes = await System.IO.File.ReadAllBytesAsync(tutor.ResumePath);
        //                    }
        //                }

        //                return Ok(new
        //                {
        //                    success = true,
        //                    data = tutors,
        //                    totalRecords = totalCount,
        //                    currentPage = pageNumber,
        //                    pageSize = rowsPerPage
        //                });
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { success = false, message = ex.Message });
        //    }
        //}

        //[HttpPost("GetTutors")]
        //public async Task<IActionResult> GetTutors([FromBody] TutorFilterRequest request)
        //{
        //    try
        //    {
        //        using (IDbConnection db = new SqlConnection(_connectionString))
        //        {
        //            var parameters = new
        //            {
        //                PageNumber = request.PageNumber,
        //                RowsPerPage = request.RowsPerPage,
        //                SearchTerm = request.SearchTerm ?? "",
        //                Gender = request.Gender ?? "",
        //                MaritalStatus = request.MaritalStatus ?? "",
        //                TeachingSources = request.TeachingSources ?? "",
        //                FeeStructures = request.FeeStructures ?? "",
        //                Classes = request.Classes ?? ""
        //            };

        //            using (var multi = await db.QueryMultipleAsync("GetTopTutorsWithPagination",
        //                                 parameters, commandType: CommandType.StoredProcedure))
        //            {
        //                var tutors = (await multi.ReadAsync<TutorProfile>()).ToList();
        //                var classes = (await multi.ReadAsync<TutorClasses>()).ToList();
        //                var experiences = (await multi.ReadAsync<ExperienceDetail>()).ToList();
        //                var qualifications = (await multi.ReadAsync<QualificationDetail>()).ToList();
        //                var totalCount = await multi.ReadFirstAsync<int>();

        //                foreach (var tutor in tutors)
        //                {
        //                    long tId = Convert.ToInt64(tutor.TutorID);
        //                    tutor.Classes = classes.Where(c => c.TutorID == tId).ToList();
        //                    tutor.Experiences = experiences.Where(e => e.TutorID == tId).ToList();
        //                    tutor.Qualifications = qualifications.Where(q => q.TutorID == tId).ToList();

        //                    // Profile Image Logic
        //                    if (!string.IsNullOrEmpty(tutor.ProfileImgPath) && System.IO.File.Exists(tutor.ProfileImgPath))
        //                    {
        //                        tutor.ProfileImgBytes = await System.IO.File.ReadAllBytesAsync(tutor.ProfileImgPath);
        //                    }
        //                }

        //                return Ok(new
        //                {
        //                    success = true,
        //                    data = tutors,
        //                    totalRecords = totalCount,
        //                    currentPage = request.PageNumber, // Fixed the 'pera' variable here
        //                    pageSize = request.RowsPerPage
        //                });
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { success = false, message = ex.Message });
        //    }
        //}

        [HttpGet("GetTutorDetails/{id}")]
        public async Task<IActionResult> GetTutorDetails(long id)
        {
            try
            {
                using (IDbConnection db = new SqlConnection(_connectionString))
                {
                    using (var multi = await db.QueryMultipleAsync("GetTutorDetailsById",
                                         new { TutorID = id },
                                         commandType: CommandType.StoredProcedure))
                    {
                        var tutor = await multi.ReadFirstOrDefaultAsync<TutorProfile>();
                        if (tutor == null) return NotFound(new { success = false, message = "Tutor not found" });

                        tutor.Classes = (await multi.ReadAsync<TutorClasses>()).ToList();
                        tutor.Experiences = (await multi.ReadAsync<ExperienceDetail>()).ToList();
                        tutor.Qualifications = (await multi.ReadAsync<QualificationDetail>()).ToList();

                            if (!string.IsNullOrEmpty(tutor.ProfileImgPath) && System.IO.File.Exists(tutor.ProfileImgPath))
                            {
                                byte[] imageBytes = await System.IO.File.ReadAllBytesAsync(tutor.ProfileImgPath);
                                tutor.ProfileImgBytes = imageBytes; 
                            }
                        if (!string.IsNullOrEmpty(tutor.ResumePath) && System.IO.File.Exists(tutor.ResumePath))
                        {
                            byte[] resumeBytes = await System.IO.File.ReadAllBytesAsync(tutor.ResumePath);
                            tutor.ResumeBytes = resumeBytes; 
                        }
                        
                        return Ok(new { success = true, data = tutor });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
