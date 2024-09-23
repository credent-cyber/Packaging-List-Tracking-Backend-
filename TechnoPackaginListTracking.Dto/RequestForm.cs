using RTechnoPackaginListTrackingMS.Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechnoPackaginListTracking.Dto
{
    public class RequestForm : Auditable
    {
        [Required]
        public string PurchaseOrder { get; set; } = string.Empty;
        public string PackingListId { get; set; } = string.Empty;
        public DateTime ShipDate { get; set; }
        public string Vessel_Flight { get; set; } = string.Empty;
        public string VehicleNumberPlate { get; set; } = string.Empty;
        public string ModeOfDelivery { get; set; } = string.Empty;
        public string FromPort { get; set; } = string.Empty;
        public DateTime SubmissionDate { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public string PackingListDate { get; set; } = string.Empty;
        public string VendorAccount { get; set; } = string.Empty;
        public string ShippingContainer { get; set; } = string.Empty;
        public string ShippingCompany { get; set; } = string.Empty;
        public string Mode { get; set; } = string.Empty;
        public List<FileUploadDto> FileUploads { get; set; } = new List<FileUploadDto>();
        public List<Cartons> Cartons { get; set; }
    }

    public class Cartons : BaseEntity
    {
        public string Carton { get; set; }
        public string ItemNumber { get; set; }
        public string Color { get; set; }
        public string Size { get; set; }
        public string Quantity { get; set; }
    }

    public class FileUploadDto : BaseEntity
    {
        public string FileName { get; set; }
        public string FileType { get; set; }
        public byte[] FileContent { get; set; } // Or string for the file path, depending on your needs
    }
}
