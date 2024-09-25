using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechnoPackaginListTracking.Dto;

namespace TechnoPackaginListTracking.Repositories
{
    public interface IDataRepository
    {
        Task<ApiResponse<RequestForm>> GetRequestFormById(int id);
        Task<ApiResponse<IEnumerable<RequestForm>>> GetAllRequests();
        Task<ApiResponse<RequestForm>> UpsertRequestForm(RequestForm data);
        Task<ApiResponse<bool>> DeleteRequestFormById(int id);
        Task<Dictionary<string, string>> GetSettings(string Key);
        Task<Dictionary<string, string>> UpsertSettings(Dictionary<string, string> settings, string value);


    }
}
