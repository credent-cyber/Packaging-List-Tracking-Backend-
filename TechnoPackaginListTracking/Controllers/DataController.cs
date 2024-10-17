using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity.UI.Services;
using TechnoPackaginListTracking.Dto;
using TechnoPackaginListTracking.Repositories;
using ClosedXML.Excel;
using Renci.SshNet;
using DocumentFormat.OpenXml.Office2010.Excel;

namespace TechnoPackaginListTracking.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DataController : ControllerBase
    {
        private readonly IDataRepository _appRepository;
        private readonly ILogger<DataController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly SFTPController _sftpController;

        public DataController(ILogger<DataController> logger, IConfiguration appConfig, IDataRepository appRepository, IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor, SFTPController sftpController) : base()
        {
            _appRepository = appRepository;
            _logger = logger;
            _configuration = appConfig;
            _env = env;
            _httpContextAccessor = httpContextAccessor;
            _sftpController = sftpController;
        }

        #region Request Form

        [HttpGet]
        [Route("request-form/{id}")]
        public async Task<ApiResponse<RequestForm>> GetRequestFormById(int id)
        {

            var userEmail = HttpContext.User.Claims.ToList()[2].Value;
            var userRole = HttpContext.User.Claims.ToList()[3].Value;
            return await _appRepository.GetRequestFormById(id, userEmail, userRole);
        }

        [HttpGet]
        [Route("all-request-form")]
      
        public async Task<ApiResponse<IEnumerable<RequestForm>>> GetAllRequests()
        {
            var authorizationHeader = HttpContext.Request.Headers["Authorization"].ToString();
            var userEmail = HttpContext.User.Claims.ToList()[2].Value;
            var userRole = HttpContext.User.Claims.ToList()[3].Value;
            return await _appRepository.GetAllRequests(userEmail, userRole);
        }

        [HttpPost]
        [Route("upsert-request-form")]
        public async Task<ApiResponse<RequestForm>> UpsertRequestForm(RequestForm data)
        {
            var userEmail = HttpContext.User.Claims.ToList()[2].Value;
            var userRole = HttpContext.User.Claims.ToList()[3].Value;

            if (data.Id > 0)
            {
                data.ModifiedBy = userEmail;
                data.ModifiedOn = DateTime.Now;
               

            }
            else
            {
                data.CreatedBy = userEmail;
                data.CreatedOn = DateTime.Now;
                data.ModifiedBy = "NA";
            }

            // Upsert the request form data
            var result = await _appRepository.UpsertRequestForm(data);

            // Check if the result is successful before proceeding
            if (result.IsSuccess)
            {
                // Generate Excel for the request form
                string excelFilePath = await GenerateExcelForRequestForm(data);

                // Ensure the excel file is generated successfully
                if (!string.IsNullOrEmpty(excelFilePath) && System.IO.File.Exists(excelFilePath))
                {
                    // Upload the generated Excel file to FTP
                    string remoteDirectory = data.PackingListId;
                    var uploadResult = await _sftpController.UploadFileToFtp(excelFilePath, remoteDirectory, data.PackingListId);

                    // Optionally log or return upload result information
                    if (uploadResult.IsUploaded)
                    {
                        _logger.LogInformation($"Excel file uploaded successfully: {uploadResult.FileLocation}");
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to upload Excel file{data.PackingListId}: {uploadResult.Message}");
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to generate Excel file for the request form.");
                }
            }

            return result;
        }

    


        [HttpDelete]
        [Route("delete-request-form/{id}")]
        public async Task<ApiResponse<bool>> DeleteRequestFormById(int id)
        {
            return await _appRepository.DeleteRequestFormById(id);
        }


        [HttpGet]
        [Route("get-packagingListId")]

        public async Task<string> GetPackagingListId()
        {
            return await _appRepository.GetPackagingListId();
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

        #region misc
        public async Task<string> GenerateExcelForRequestForm(RequestForm data)
        {
            string fileName = $"RequestForm_{data.PackingListId}.xlsx";
            string filePath = Path.Combine(Path.GetTempPath(), fileName);

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("RequestForm");

                // Define styles for headers
                var blueHeaderStyle = workbook.Style;
                blueHeaderStyle.Fill.BackgroundColor = XLColor.Gray;
                blueHeaderStyle.Font.FontColor = XLColor.White;
                blueHeaderStyle.Font.Bold = true;

                var cyanHeaderStyle = workbook.Style;
                cyanHeaderStyle.Fill.BackgroundColor = XLColor.Blue;
                cyanHeaderStyle.Font.FontColor = XLColor.Black; // Change text color for better contrast
                cyanHeaderStyle.Font.Bold = true;

                // Add headers for RequestForm
                string[] headers = new string[] {
            "Packing list Id", "Packing list date", "Ship date", "Vendor account",
            "From port", "Vessel/Flight", "Shipping container", "Vehicle number plate",
            "Shipping company", "Mode of delivery", "Mode", "Purchase order",
            "Carton", "Item number", "Color", "Size", "Qty carton"
        };

                // Populate headers in the first row
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                }

                // Apply blue header style to the first 11 columns (RequestForm section)
                worksheet.Range(1, 1, 1, 11).Style = blueHeaderStyle;

                // Apply cyan header style to the last 6 columns (Cartons section)
                worksheet.Range(1, 12, 1, 17).Style = cyanHeaderStyle;

                // Start filling data from the second row
                int currentRow = 2;

                // Add data for each Carton and repeat the main table fields for each row
                foreach (var carton in data.Cartons)
                {
                    // Repeat the main RequestForm data for each carton
                    worksheet.Cell(currentRow, 1).Value = data.PackingListId;
                    worksheet.Cell(currentRow, 2).Value = data.PackingListDate;
                    worksheet.Cell(currentRow, 3).Value = data.ShipDate;
                    worksheet.Cell(currentRow, 4).Value = data.VendorAccount;
                    worksheet.Cell(currentRow, 5).Value = data.FromPort;
                    worksheet.Cell(currentRow, 6).Value = data.Vessel_Flight;
                    worksheet.Cell(currentRow, 7).Value = data.ShippingContainer;
                    worksheet.Cell(currentRow, 8).Value = data.VehicleNumberPlate;
                    worksheet.Cell(currentRow, 9).Value = data.ShippingCompany;
                    worksheet.Cell(currentRow, 10).Value = data.ModeOfDelivery;
                    worksheet.Cell(currentRow, 11).Value = data.Mode;
                    worksheet.Cell(currentRow, 12).Value = data.PurchaseOrder;

                    // Fill Carton details
                    worksheet.Cell(currentRow, 13).Value = carton.Carton;
                    worksheet.Cell(currentRow, 14).Value = carton.ItemNumber;
                    worksheet.Cell(currentRow, 15).Value = carton.Color;
                    worksheet.Cell(currentRow, 16).Value = carton.Size;
                    worksheet.Cell(currentRow, 17).Value = carton.Quantity;

                    currentRow++;
                }

                // Insert a blank row
                currentRow++; // Move to the next row for the blank line
                worksheet.Row(currentRow).InsertRowsAbove(1);

            //    // Details to add below the blank row
            //    string[,] details = new string[,]
            //    {
            //{ "Packing list Id", data.PackingListId, "\"unique identifier for the PL\n\"" },
            //{ "", "\"1165 = delivery note number sequence for that specific supplier //\n200233 = supplier number\"", "" },
            //{ "Packing list date", data.PackingListDate, "PL creation date" },
            //{ "Ship date", data.ShipDate.ToShortDateString(), "ETD" },
            //{ "Vendor account", data.VendorAccount, "Supplier / Vendor number\nMapping with GW-Supplier No." },
            //{ "From port", data.FromPort, "LOCode of the POL" },
            //{ "Vessel/Flight", data.Vessel_Flight, "Vessel/Flight" },
            //{ "Shipping container", data.ShippingContainer, "Ship Container No.\nfor SEA & Air" },
            //{ "Vehicle number plate", data.VehicleNumberPlate, "Vehicle Number Plate\nfor TRUCK" },
            //{ "Shipping company", data.ShippingCompany, "Forwarder\nMapping with GW-Forwarder" },
            //{ "Mode of delivery", data.ModeOfDelivery, "Mode of delivery\ni.e. SEA, TRUCK, AIR, SEA/AIR, RAIL" },
            //{ "Mode", data.Mode, "Transport mode\nflatpacked, GOH" },
            //{ "Purchase order", data.PurchaseOrder, "DFO PO Number" },
            //{ "carton", "", "Carton No. of the carton label\nsupplier number + 8 digit number sequence according to the carton label" },
            //{ "Item number", "", "Item-Number" },
            //{ "Color", "", "Color" },
            //{ "Size", "", "Size" },
            //{ "Qty carton", "", "Qty" },
            //    };

            //    // Add details to the worksheet
            //    for (int i = 0; i < details.GetLength(0); i++)
            //    {
            //        for (int j = 0; j < details.GetLength(1); j++)
            //        {
            //            worksheet.Cell(currentRow, j + 1).Value = details[i, j];
            //        }
            //        currentRow++;
            //    }

                // Format the entire table as a striped table without filters
                var cartonRange = worksheet.Range(1, 1, currentRow - 1, 17);
                var cartonTable = cartonRange.CreateTable();
                cartonTable.Theme = XLTableTheme.TableStyleMedium2; // Striped table style
                cartonTable.ShowAutoFilter = false; // Remove filters from the table

                // Adjust column width for readability
                worksheet.Columns().AdjustToContents();

                // Save the Excel file to a temporary location
                workbook.SaveAs(filePath);
            }

            return filePath;
        }


        #endregion

        #region DropDowns
        [HttpGet]
        [Route("all-Ports")]

        public async Task<List<Ports>> GetAllPorts()
        {
            return await _appRepository.GetAllPorts();
        }

        [HttpGet]
        [Route("all-DeliveryMode")]

        public async Task<List<DeliveryMode>> GetAllDeliveryMode()
        {
            return await _appRepository.GetAllDeliveryMode();
        }

        [HttpGet]
        [Route("all-modes")]

        public async Task<List<Mode>> GetAllModes()
        {
            return await _appRepository.GetAllModes();
        }
        [HttpGet]
        [Route("all-cartonsSize")]

        public async Task<List<CartonsSize>> GetAllCartonsSize()
        {
            return await _appRepository.GetAllCartonsSize();
        }
        #endregion
    }
}
