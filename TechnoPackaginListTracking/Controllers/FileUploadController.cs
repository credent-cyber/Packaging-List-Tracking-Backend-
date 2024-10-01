using Microsoft.AspNetCore.Http;
using IO = System.IO;
using Microsoft.AspNetCore.Mvc;
using TechnoPackaginListTracking.Dto;

namespace TechnoPackaginListTracking.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileUploadController : ControllerBase
    {
        private readonly IWebHostEnvironment env;
        private readonly ILogger<FileUploadController> logger;
        private readonly IConfiguration configuration;

        public FileUploadController(IWebHostEnvironment env, ILogger<FileUploadController> logger, IConfiguration configuration)
        {
            this.env = env;
            this.logger = logger;
            this.configuration = configuration;
        }

        [HttpPost("AppendFile/{fragment}")]
        public async Task<UploadResult> UploadFileChunk(int fragment, IFormFile file, string User)
        {
            try
            {
                // Get the document path from configuration
                var DocumentPath = configuration.GetValue<string>("DocumentUploadPath");

                // Generate a unique file name by appending the user and current datetime
                var fileExtension = Path.GetExtension(file.FileName);
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss"); 
                var uniqueFileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{User}_{timestamp}{fileExtension}";

                var fileLocation = Path.Combine(env.ContentRootPath, DocumentPath, uniqueFileName);

                // If it's the first chunk and the file already exists, delete the existing file
                if (fragment == 0 && IO.File.Exists(fileLocation))
                {
                    IO.File.Delete(fileLocation);
                }

                // Append the incoming file chunk to the file
                using (var fileStream = new FileStream(fileLocation, FileMode.Append, FileAccess.Write, FileShare.None))
                using (var bw = new BinaryWriter(fileStream))
                {
                    await file.CopyToAsync(fileStream);
                }

                // Return the unique file name with the result
                return new UploadResult { IsUploaded = true, FileLocation = uniqueFileName };
            }
            catch (Exception exception)
            {
                Console.WriteLine("Exception: {0}", exception.Message);
            }
            return new UploadResult { IsUploaded = false, FileLocation = "" };
        }


        [HttpGet("DownloadFile/{fileName}")]
        public IActionResult DownloadFile(string fileName)
        {
            try
            {
                var documentPath = configuration.GetValue<string>("DocumentUploadPath");
                var fileLocation = Path.Combine(env.ContentRootPath, documentPath, fileName);

                if (!IO.File.Exists(fileLocation))
                {
                    return NotFound($"File {fileName} not found.");
                }

                var fileStream = new FileStream(fileLocation, FileMode.Open, FileAccess.Read);
                var contentType = "application/octet-stream"; 

                return File(fileStream, contentType, fileName);
            }
            catch (Exception exception)
            {
                logger.LogError($"Error downloading file: {exception.Message}");
                return StatusCode(500, $"Error downloading file: {exception.Message}");
            }
        }


        [HttpDelete("DeleteFile/{fileName}")]
        public IActionResult DeleteFile(string fileName)
        {
            try
            {
                var documentPath = configuration.GetValue<string>("DocumentUploadPath");
                var fileLocation = Path.Combine(env.ContentRootPath, documentPath, fileName);

                if (!IO.File.Exists(fileLocation))
                {
                    return NotFound($"File {fileName} not found.");
                }

                IO.File.Delete(fileLocation);
                return Ok(new { Message = $"File {fileName} deleted successfully." });
            }
            catch (Exception exception)
            {
                logger.LogError($"Error deleting file: {exception.Message}");
                return StatusCode(500, $"Error deleting file: {exception.Message}");
            }
        }
    }
}
