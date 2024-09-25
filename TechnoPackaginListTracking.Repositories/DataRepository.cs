using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechnoPackaginListTracking.DataContext;
using TechnoPackaginListTracking.Dto;
using TechnoPackaginListTracking.Dto.Common;
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


        #region Request Form
        public async Task<ApiResponse<RequestForm>> GetRequestFormById(int id)
        {
            var data = new ApiResponse<RequestForm>();
            try
            {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                var queryData = AppDbCxt.RequestForms.FirstOrDefault(o => o.Id == id);
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

        public async Task<ApiResponse<IEnumerable<RequestForm>>> GetAllRequests()
        {
            var data = new ApiResponse<IEnumerable<RequestForm>>();
            try
            {
                data.Result =  AppDbCxt.RequestForms.Include(o=>o.Cartons).ToList();
                data.IsSuccess = true;
                data.Message = "Success";
                return data;
            }
            catch (Exception ex)
            {
                data.IsSuccess = false;
                data.Message = ex.Message;
                return data;
            }
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
                    var existingRequestForm = AppDbCxt.RequestForms
                        .Include(r => r.Cartons)  
                        .Include(r => r.FileUploads) 
                        .FirstOrDefault(r => r.Id == data.Id);

                    if (existingRequestForm == null)
                        throw new ArgumentException("Request form not found");

                
                    AppDbCxt.Entry(existingRequestForm).CurrentValues.SetValues(data);

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

                    AppDbCxt.RequestForms.Update(existingRequestForm);
                }
                else
                {
                    AppDbCxt.RequestForms.Add(data);
                }

                AppDbCxt.SaveChanges();

                result.IsSuccess = true;
                result.Message = "Success";
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
                var requestForm = AppDbCxt.RequestForms.FirstOrDefault(o => o.Id == id);

                if (requestForm == null)
                {
                    result.IsSuccess = false;
                    result.Message = "Request Form not found.";
                    return await Task.FromResult(result);
                }

                AppDbCxt.RequestForms.Remove(requestForm);
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

        #region Settings
        public async Task<Dictionary<string, string>> GetSettings(string Key)
        {
            //var settings = await AppDbCxt.AppSettings.Where(o => o.Key == Constants.Keys.DocsUploadPath).ToListAsync();
            var settings = await AppDbCxt.AppSettings.Where(o => o.Key == Key).ToListAsync();

            return settings.ToDictionary(o => o.Key, v => v.Value);
        }
        public async Task<Dictionary<string, string>> UpsertSettings(Dictionary<string, string> settings, string user)
        {
            var inserted = new Dictionary<string, string>();

            try
            {
                foreach (var key in settings.Keys)
                {
                    var setting = await AppDbCxt.AppSettings.FirstOrDefaultAsync(o => o.Key == key);

                    if (setting != null)
                    {
                        setting.Value = settings[key];
                        setting.ModifiedOn = DateTime.Now;
                        setting.ModifiedBy = user;
                        AppDbCxt.Update(setting);
                    }
                    else
                    {
                        setting = new AppSettings()
                        {
                            Key = key,
                            Value = settings[key],
                            CreatedBy = user,
                            CreatedOn = DateTime.Now,
                        };
                        AppDbCxt.Add(setting);
                    }

                    inserted.Add(key, setting.Value);

                    AppDbCxt.SaveChanges();
                }

            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                throw;
            }
            return inserted;
        }
        #endregion
    }
}
