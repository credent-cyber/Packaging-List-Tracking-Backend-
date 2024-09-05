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
        Task<ApiResponse<Details>> GetDetailsById(int id);
        Task<IEnumerable<Details>> GetAllDetails();
        Task<ApiResponse<Details>> UpsertDetails(Details data);
    }
}
