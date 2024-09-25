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
        private readonly IDataRepository _appRepository;
        private readonly ILogger<DataController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DataController(ILogger<DataController> logger, IConfiguration appConfig, IDataRepository appRepository, IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor) : base()
        {
            _appRepository = appRepository;
            _logger = logger;
            _configuration = appConfig;
            _env = env;
            _httpContextAccessor = httpContextAccessor;
        }

        #region Request Form

        [HttpGet]
        [Route("request-form/{id}")]
        public async Task<ApiResponse<RequestForm>> GetRequestFormById(int id)
        {
            return await _appRepository.GetRequestFormById(id);
        }

        [HttpGet]
        [Route("all-request-form")]
        public async Task<ApiResponse<IEnumerable<RequestForm>>> GetAllRequests()
        {
            return await _appRepository.GetAllRequests();
        }

        [HttpPost]
        [Route("upsert-request-form")]
        public async Task<ApiResponse<RequestForm>> UpsertRequestForm(RequestForm data)
        {
            return await _appRepository.UpsertRequestForm(data);
        }

        [HttpDelete]
        [Route("delete-request-form/{id}")]
        public async Task<ApiResponse<bool>> DeleteRequestFormById(int id)
        {
            return await _appRepository.DeleteRequestFormById(id);
        }

        #endregion

        #region Settings

        [HttpPost]
        [Route("get-settings")]
        public async Task<Dictionary<string, string>> GetSettings(string Key)
        {
            var result = await _appRepository.GetSettings(Key);
            return result;
        }

        [HttpPost]
        [Route("upsert-settings")]
        public async Task<Dictionary<string, string>> UpsertSettings(Dictionary<string, string> settings)
        {
            // get the username or other info from the current HttpContext
            var currentUser = _httpContextAccessor?.HttpContext?.User?.Identity?.Name;
            var result = await _appRepository.UpsertSettings(settings, currentUser);
            return result;
        }

        #endregion
    }
}
