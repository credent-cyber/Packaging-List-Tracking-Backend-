using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TechnoPackaginListTracking.Dto;
using TechnoPackaginListTracking.Repositories;

namespace TechnoPackaginListTracking.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataController : ControllerBase
    {
        readonly IDataRepository _appRepository;
        readonly ILogger _logger;
        readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public DataController(ILogger<DataController> logger, IConfiguration appConfig, IDataRepository appRepository, IWebHostEnvironment env) : base()
        {
            _appRepository = (IDataRepository?)appRepository;
            _logger = logger;
            _configuration = appConfig;
            _env = env;
            // Retrieve username and roles in the constructor and store them in _userName and _userRoles

        }

        #region Details
        [HttpGet]
        [Route("details/{id}")]
        public async Task<ApiResponse<Details>> GetDetailsById(int id)
        {
            return await _appRepository.GetDetailsById(id);
        }

        [HttpGet]
        [Route("all-details")]
        [AllowAnonymous]
        public async Task<IEnumerable<Details>> GetAllDetails()
        {
            return await _appRepository.GetAllDetails();
        }

        [HttpPost]
        [Route("UpsertDetails")]
        public async Task<ApiResponse<Details>> UpsertDetails(Details details)
        {

            return await _appRepository.UpsertDetails(details);
        }
        #endregion
    }
}
