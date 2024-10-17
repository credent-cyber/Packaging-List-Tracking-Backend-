using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechnoPackaginListTracking.DataContext;
using TechnoPackaginListTracking.Dto;
using Microsoft.EntityFrameworkCore;
using System.Data.SqlClient;

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
        public async Task<ApiResponse<RequestForm>> GetRequestFormById(int id, string userEmail, string userRole)
        {
            var data = new ApiResponse<RequestForm>();
            try
            {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                if (string.Equals(userRole, "Admin", StringComparison.OrdinalIgnoreCase) || string.Equals(userRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
                {
                    data.Result = AppDbCxt.RequestForms.Include(o=>o.Cartons).Include(o=>o.FileUploads).FirstOrDefault(o => o.Id == id);
                }
                else
                {
                    data.Result = AppDbCxt.RequestForms.Include(o=>o.Cartons).Include(o=>o.FileUploads).FirstOrDefault(o => o.Id == id && o.CreatedBy == userEmail);
                }
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
          
                data.IsSuccess = true;
                return await Task.FromResult(data);
            }
            catch (Exception ex)
            {
                data.IsSuccess = false;
                data.Message = ex.Message;
                return data;
            }

        }

        public async Task<ApiResponse<IEnumerable<RequestForm>>> GetAllRequests(string userEmail, string userRole)
        {
            var data = new ApiResponse<IEnumerable<RequestForm>>();

            try
            {
                if (string.Equals(userRole, "Admin", StringComparison.OrdinalIgnoreCase) || string.Equals(userRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
                {                 
                    data.Result = await AppDbCxt.RequestForms
                        .Include(o => o.Cartons) // Correctly include Cartons
                        .Include(o => o.FileUploads) // Correctly include Cartons
                        .AsNoTracking() // Optional: improve performance
                        .ToListAsync(); // Asynchronous call

                }
                else
                {
                    data.Result = await AppDbCxt.RequestForms.Where(o=>o.CreatedBy == userEmail)
                       .Include(o => o.Cartons) 
                       .Include(o => o.FileUploads)
                       .AsNoTracking() // Optional: improve performance
                       .ToListAsync();
                }
                data.IsSuccess = true;
                data.Message = "Success";
            }
            catch (Exception ex)
            {
                data.IsSuccess = false;
                data.Message = ex.Message;
            }
            return data;
        }


        public async Task<ApiResponse<RequestForm>> UpsertRequestForm(RequestForm data)
        {
            var result = new ApiResponse<RequestForm>();
            const int maxRetries = 3; // Maximum number of retries for unique ID generation
            int retryCount = 0;

            try
            {
                if (data == null)
                    throw new ArgumentNullException("Invalid Details data");

                // Check if it's an update or a new insert
                if (data.Id > 0)
                {
                    // Retrieve the existing RequestForm from the database
                    var existingRequestForm = await AppDbCxt.RequestForms
                        .Include(r => r.FileUploads)
                        .Include(r => r.Cartons)
                        .FirstOrDefaultAsync(r => r.Id == data.Id);

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
                    while (retryCount < maxRetries)
                    {
                        try
                        {
                            var latestPackingListId = await AppDbCxt.RequestForms
                                .OrderByDescending(o => o.Id)
                                .Select(o => o.PackingListId)
                                .FirstOrDefaultAsync();

                            if (!string.IsNullOrEmpty(latestPackingListId))
                            {
                                // Extract numeric part from the PackingListId
                                string numericPart = new string(latestPackingListId.Where(char.IsDigit).ToArray());
                                string prefixPart = new string(latestPackingListId.Where(char.IsLetter).ToArray());

                                // If we have a numeric part, increment it
                                if (int.TryParse(numericPart, out int packingListIdNumber))
                                {
                                    data.PackingListId = $"{prefixPart}{packingListIdNumber + 1}";
                                }
                            }

                            // Add the new RequestForm
                            await AppDbCxt.RequestForms.AddAsync(data);
                            await AppDbCxt.SaveChangesAsync();
                            break; // Exit loop if successful
                        }
                        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
                        {
                            // Increment retry count and prepare for the next iteration
                            retryCount++;
                        }
                    }

                    if (retryCount == maxRetries)
                    {
                        throw new Exception("Failed to generate a unique PackingListId after multiple attempts.");
                    }
                }

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

        private bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            // Check for unique constraint violation, adjust this based on your database provider
            return ex.InnerException is SqlException sqlEx && sqlEx.Number == 2627; // SQL Server unique constraint violation number
        }


        public async Task<string> GetPackagingListId()
        {
            try
            {

                var latestPackingListId = await AppDbCxt.RequestForms
                    .OrderByDescending(o => o.Id)
                    .Select(o => o.PackingListId)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrEmpty(latestPackingListId))
                {
                    // Extract numeric part from the PackingListId
                    string numericPart = new string(latestPackingListId.Where(char.IsDigit).ToArray());
                    string prefixPart = new string(latestPackingListId.Where(char.IsLetter).ToArray());

                    // If we have a numeric part, increment it
                    if (int.TryParse(numericPart, out int packingListIdNumber))
                    {
                        return $"{prefixPart}{packingListIdNumber + 1}";
                    }
                }

                // If no valid PackingListId exists, return default value with prefix
                return "200001";
            }
            catch (Exception ex)
            {
                return null;
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


        #region DropDowns
        public async Task<List<Ports>> GetAllPorts()
        {
            try
            {
                return await AppDbCxt.Ports.ToListAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task<List<DeliveryMode>> GetAllDeliveryMode()
        {
            try
            {
                return await AppDbCxt.DeliveryMode.ToListAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task<List<Mode>> GetAllModes()
        {
            try
            {
                return await AppDbCxt.Modes.ToListAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task<List<CartonsSize>> GetAllCartonsSize()
        {
            try
            {
                return await AppDbCxt.CartonsSize.ToListAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                throw;
            }
        }

        #endregion
    }
}
