using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechnoPackaginListTracking.DataContext;
using TechnoPackaginListTracking.Dto;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TechnoPackaginListTracking.Repositories
{
    public class DataRepository : BaseRepository, IDataRepository
    {
        public AppDbContext AppDbCxt { get; set; }
        public DataRepository(ILogger<BaseRepository> logger, AppDbContext appContext) : base(logger)
        {
            AppDbCxt = appContext;
        }

        #region Details
        public async Task<ApiResponse<Details>> GetDetailsById(int id)
        {
            var data = new ApiResponse<Details>();
            try
            {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                var queryData = AppDbCxt.Details.FirstOrDefault(o => o.Id == id);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                data.IsSuccess = true;
                data.Result = queryData;
                return await Task.FromResult(data);
            }
            catch (Exception ex)
            {
                data.IsSuccess = false;
                data.Message = ex.Message;
                return data;
            }

        }

        public async Task<IEnumerable<Details>> GetAllDetails()
        {
            IEnumerable<Details> result = null;

            result = AppDbCxt.Details.ToList();
            return result;
        }
        public async Task<ApiResponse<Details>> UpsertDetails(Details data)
        {
            var result = new ApiResponse<Details>();
            try
            {
                if (data == null)
                    throw new ArgumentNullException("Invalid Details data");

                if (data.Id > 0)
                {
                    AppDbCxt.Details.Update(data);
                }
                else
                {
                    AppDbCxt.Details.Add(data);
                }

                AppDbCxt.SaveChanges();

                result.IsSuccess = true;
                result.Result = data;
                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = ex.Message;
                return result;
            }
        }
        #endregion
    }
}
