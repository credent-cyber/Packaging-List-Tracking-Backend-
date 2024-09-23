using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Entity;
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

        #region Request Form
        public async Task<ApiResponse<RequestForm>> GetRequestFormById(int id)
        {
            var data = new ApiResponse<RequestForm>();
            try
            {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                var queryData = AppDbCxt.RequestForm.FirstOrDefault(o => o.Id == id);
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

        public async Task<IEnumerable<RequestForm>> GetAllRequests()
        {
            var result = await AppDbCxt.RequestForm
                .Include(r => r.Cartons)
                .Include(r => r.FileUploads)
                .ToListAsync(); 

            return result;
        }

        public async Task<ApiResponse<RequestForm>> UpsertRequestForm(RequestForm data)
        {
            var result = new ApiResponse<RequestForm>();
            try
            {
                if (data == null)
                    throw new ArgumentNullException("Invalid Details data");

                // Check if it's an update or a new insert
                if (data.Id > 0)
                {
                    // Retrieve the existing RequestForm from the database
                    var existingRequestForm = AppDbCxt.RequestForm
                        .Include(r => r.Cartons)  
                        .Include(r => r.FileUploads) 
                        .FirstOrDefault(r => r.Id == data.Id);

                    if (existingRequestForm == null)
                        throw new ArgumentException("Request form not found");

                
                    AppDbCxt.Entry(existingRequestForm).CurrentValues.SetValues(data);

                    // Handle Cartons (dependent table)
                    // Remove cartons that are not in the new data
                    foreach (var existingCarton in existingRequestForm.Cartons.ToList())
                    {
                        if (!data.Cartons.Any(c => c.Id == existingCarton.Id))
                        {
                            AppDbCxt.Cartons.Remove(existingCarton);
                        }
                    }

                    // Add or update cartons
                    foreach (var newCarton in data.Cartons)
                    {
                        var existingCarton = existingRequestForm.Cartons.FirstOrDefault(c => c.Id == newCarton.Id);
                        if (existingCarton != null)
                        {
                            // Update existing carton
                            AppDbCxt.Entry(existingCarton).CurrentValues.SetValues(newCarton);
                        }
                        else
                        {
                            // Add new carton
                            existingRequestForm.Cartons.Add(newCarton);
                        }
                    }

                    // Handle FileUploads (dependent table)
                    foreach (var existingFile in existingRequestForm.FileUploads.ToList())
                    {
                        if (!data.FileUploads.Any(f => f.Id == existingFile.Id))
                        {
                            AppDbCxt.FileUploads.Remove(existingFile);
                        }
                    }

                    // Add or update FileUploads
                    foreach (var newFile in data.FileUploads)
                    {
                        var existingFile = existingRequestForm.FileUploads.FirstOrDefault(f => f.Id == newFile.Id);
                        if (existingFile != null)
                        {
                            AppDbCxt.Entry(existingFile).CurrentValues.SetValues(newFile);
                        }
                        else
                        {
                            existingRequestForm.FileUploads.Add(newFile);
                        }
                    }

                    AppDbCxt.RequestForm.Update(existingRequestForm);
                }
                else
                {
                    AppDbCxt.RequestForm.Add(data);
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


        public async Task<ApiResponse<bool>> DeleteRequestFormById(int id)
        {
            var result = new ApiResponse<bool>();
            try
            {
                var requestForm = AppDbCxt.RequestForm.FirstOrDefault(o => o.Id == id);

                if (requestForm == null)
                {
                    result.IsSuccess = false;
                    result.Message = "Request Form not found.";
                    return await Task.FromResult(result);
                }

                AppDbCxt.RequestForm.Remove(requestForm);
                AppDbCxt.SaveChanges();

                result.IsSuccess = true;
                result.Result = true; // Indicating success
                return await Task.FromResult(result);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = ex.Message;
                result.Result = false; // Indicating failure
                return await Task.FromResult(result);
            }
        }


        #endregion
    }
}
