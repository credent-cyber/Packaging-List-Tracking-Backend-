﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using TechnoPackaginListTracking.DataContext;
using TechnoPackaginListTracking.Dto;
using NPOI.SS.UserModel;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using TechnoPackaginListTracking.Infrastructure.ActionFilters;

namespace TechnoPackaginListTracking.Controllers.OData
{
    [Route("api/[controller]")]
    [ApiController]
    [ODataAuthorize]
    public class RequestFromController : ODataController
    {
        private readonly ILogger<RequestFromController> _logger;
        private readonly AppDbContext _dbContext;

        public RequestFromController(ILogger<RequestFromController> logger, AppDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        // Include child data when querying all RequestForms
        [EnableQuery]
        [ODataAuthorize]
        public IQueryable<RequestForm> Get()
        {
            return _dbContext.RequestForms
                .Include(o => o.Cartons) // Include Cartons
                .Include(o => o.FileUploads) // Include FileUploads if needed
                .AsQueryable();
        }

        // Optional: Custom method for paginated results with filtering and child data
        [HttpGet("paged")]
        [ODataAuthorize]
        public async Task<IActionResult> GetPaged(
            int page = 1,
            int pageSize = 10,
            string search = null)
        {
            var query = _dbContext.RequestForms
                .Include(o => o.Cartons) // Include Cartons
                .Include(o => o.FileUploads) // Include FileUploads if needed
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(r => r.PackingListId.Contains(search) ||
                                         r.PurchaseOrder.Contains(search) ||
                                         r.FromPort.Contains(search) ||
                                         r.VendorAccount.Contains(search) ||
                                         r.Vessel_Flight.Contains(search) ||
                                         r.ShippingContainer.Contains(search) ||
                                         r.VehicleNumberPlate.Contains(search) ||
                                         r.ShippingCompany.Contains(search) ||
                                         r.Mode.Contains(search) ||
                                         r.ModeOfDelivery.Contains(search));
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                TotalItems = totalItems,
                TotalPages = totalPages,
                PageSize = pageSize,
                CurrentPage = page,
                Items = items
            });
        }


        // Export filtered data to Excel including child tables
        [HttpGet("exportExcel")]
        [ODataAuthorize]
        public async Task<IActionResult> ExportDataToExcel(
            string search = null)
        {
            var query = _dbContext.RequestForms
                .Include(o => o.Cartons)      // Include Cartons
                .Include(o => o.FileUploads)  // Include FileUploads
                .AsQueryable();

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(r => r.PackingListId.Contains(search) ||
                                         r.PurchaseOrder.Contains(search) ||
                                         r.FromPort.Contains(search) ||
                                         r.VendorAccount.Contains(search) ||
                                         r.Vessel_Flight.Contains(search) ||
                                         r.ShippingContainer.Contains(search) ||
                                         r.VehicleNumberPlate.Contains(search) ||
                                         r.ShippingCompany.Contains(search) ||
                                         r.Mode.Contains(search) ||
                                         r.ModeOfDelivery.Contains(search));
            }

            var requestForms = await query.ToListAsync();

            // Generate Excel file
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("RequestForms");

                // Define header columns
                worksheet.Cell(1, 1).Value = "Packing List ID";
                worksheet.Cell(1, 2).Value = "Purchase Order";
                worksheet.Cell(1, 3).Value = "From Port";
                worksheet.Cell(1, 4).Value = "Vendor Account";
                worksheet.Cell(1, 5).Value = "Vessel/Flight";
                worksheet.Cell(1, 6).Value = "Shipping Container";
                worksheet.Cell(1, 7).Value = "Vehicle Number Plate";
                worksheet.Cell(1, 8).Value = "Shipping Company";
                worksheet.Cell(1, 9).Value = "Mode";
                worksheet.Cell(1, 10).Value = "Mode of Delivery";

                // Cartons-related columns
                worksheet.Cell(1, 11).Value = "Carton";
                worksheet.Cell(1, 12).Value = "Item Number";
                worksheet.Cell(1, 13).Value = "Color";
                worksheet.Cell(1, 14).Value = "Size";
                worksheet.Cell(1, 15).Value = "Quantity";

                // FileUploads-related columns
                worksheet.Cell(1, 16).Value = "File Name";
                worksheet.Cell(1, 17).Value = "File Type";

                // Add data rows for RequestForm, Cartons, and FileUploads
                for (int i = 0; i < requestForms.Count; i++)
                {
                    var form = requestForms[i];
                    worksheet.Cell(i + 2, 1).Value = form.PackingListId;
                    worksheet.Cell(i + 2, 2).Value = form.PurchaseOrder;
                    worksheet.Cell(i + 2, 3).Value = form.FromPort;
                    worksheet.Cell(i + 2, 4).Value = form.VendorAccount;
                    worksheet.Cell(i + 2, 5).Value = form.Vessel_Flight;
                    worksheet.Cell(i + 2, 6).Value = form.ShippingContainer;
                    worksheet.Cell(i + 2, 7).Value = form.VehicleNumberPlate;
                    worksheet.Cell(i + 2, 8).Value = form.ShippingCompany;
                    worksheet.Cell(i + 2, 9).Value = form.Mode;
                    worksheet.Cell(i + 2, 10).Value = form.ModeOfDelivery;

                    // Populate Cartons columns
                    if (form.Cartons != null && form.Cartons.Any())
                    {
                        int cartonIndex = 0;
                        foreach (var carton in form.Cartons)
                        {
                            // Write data for each carton, adding new rows for additional cartons
                            worksheet.Cell(i + 2 + cartonIndex, 11).Value = carton.Carton;
                            worksheet.Cell(i + 2 + cartonIndex, 12).Value = carton.ItemNumber;
                            worksheet.Cell(i + 2 + cartonIndex, 13).Value = carton.Color;
                            worksheet.Cell(i + 2 + cartonIndex, 14).Value = carton.Size;
                            worksheet.Cell(i + 2 + cartonIndex, 15).Value = carton.Quantity;
                            cartonIndex++;
                        }
                    }

                    // Populate FileUploads columns
                    if (form.FileUploads != null && form.FileUploads.Any())
                    {
                        int fileIndex = 0;
                        foreach (var file in form.FileUploads)
                        {
                            // Write data for each file, adding new rows for additional file uploads
                            worksheet.Cell(i + 2 + fileIndex, 16).Value = file.FileName;
                            worksheet.Cell(i + 2 + fileIndex, 17).Value = file.FileType;
                            fileIndex++;
                        }
                    }
                }


                // Save the Excel to a memory stream
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Seek(0, SeekOrigin.Begin);
                    var content = stream.ToArray();

                    // Return the file as an Excel download
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"RequestForms_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
                }
            }

        }
    }
}
