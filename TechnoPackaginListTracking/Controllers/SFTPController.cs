using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Renci.SshNet;
using System.IO;
using System.Threading.Tasks;

namespace TechnoPackaginListTracking.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SFTPController : ControllerBase
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;
        private readonly string _remoteUploadPath;
        private readonly ILogger<SFTPController> _logger;

        public SFTPController(IConfiguration configuration, ILogger<SFTPController> logger)
        {
            _host = configuration["SftpSettings:Host"];
            _port = int.Parse(configuration["SftpSettings:Port"]);
            _username = configuration["SftpSettings:Username"];
            _password = configuration["SftpSettings:Password"];
            _remoteUploadPath = configuration["SftpSettings:RemoteUploadPath"];
            _logger = logger;
        }

        // Upload a file chunk to the SFTP server
        [HttpPost("upload-chunk")]
        public async Task<IActionResult> UploadChunk([FromForm] IFormFile file, [FromForm] int chunkNumber, [FromForm] int totalChunks, [FromForm] string fileName)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            try
            {
                string tempFolderPath = Path.Combine(Path.GetTempPath(), "sftp_temp");
                if (!Directory.Exists(tempFolderPath))
                {
                    Directory.CreateDirectory(tempFolderPath);
                }

                string tempFilePath = Path.Combine(tempFolderPath, fileName);

                // Save the chunk to a temporary file
                using (var fileStream = new FileStream(tempFilePath, FileMode.Append, FileAccess.Write))
                {
                    await file.CopyToAsync(fileStream);
                }

                _logger.LogInformation($"Received chunk {chunkNumber} of {totalChunks}");

                // If this is the last chunk, upload the complete file to the SFTP server
                if (chunkNumber == totalChunks)
                {
                    using (var client = new SftpClient(_host, _port, _username, _password))
                    {
                        client.Connect();
                        if (!client.IsConnected)
                        {
                            return StatusCode(500, "Failed to connect to the SFTP server.");
                        }

                        // Upload the completed file
                        using (var localFileStream = new FileStream(tempFilePath, FileMode.Open))
                        {
                            client.UploadFile(localFileStream, $"{_remoteUploadPath}/{fileName}");
                        }

                        client.Disconnect();
                    }

                    // Delete the temporary file after successful upload
                    System.IO.File.Delete(tempFilePath);

                    _logger.LogInformation("File uploaded successfully to the SFTP server.");
                    return Ok(new { Message = "File uploaded successfully", FileName = fileName });
                }

                return Ok(new { Message = $"Chunk {chunkNumber} of {totalChunks} uploaded successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading chunk: {ex.Message}");
                return StatusCode(500, $"Error uploading chunk: {ex.Message}");
            }
        }

        // Download a file from the SFTP server
        [HttpGet("download-file/{fileName}")]
        public IActionResult DownloadFile(string fileName)
        {
            try
            {
                using (var client = new SftpClient(_host, _port, _username, _password))
                {
                    client.Connect();
                    if (!client.IsConnected)
                    {
                        return StatusCode(500, "Failed to connect to the SFTP server.");
                    }

                    // Remote file path
                    string remoteFilePath = $"{_remoteUploadPath}/{fileName}";

                    if (!client.Exists(remoteFilePath))
                    {
                        return NotFound($"File {fileName} does not exist on the SFTP server.");
                    }

                    using (var fileStream = new MemoryStream())
                    {
                        client.DownloadFile(remoteFilePath, fileStream);
                        client.Disconnect();

                        // Return the file as a downloadable response
                        fileStream.Seek(0, SeekOrigin.Begin);
                        return File(fileStream, "application/octet-stream", fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error downloading file: {ex.Message}");
                return StatusCode(500, $"Error downloading file: {ex.Message}");
            }
        }

        // Delete a file from the SFTP server
        [HttpDelete("delete-file/{fileName}")]
        public IActionResult DeleteFile(string fileName)
        {
            try
            {
                using (var client = new SftpClient(_host, _port, _username, _password))
                {
                    client.Connect();
                    if (!client.IsConnected)
                    {
                        return StatusCode(500, "Failed to connect to the SFTP server.");
                    }

                    string remoteFilePath = $"{_remoteUploadPath}/{fileName}";

                    if (client.Exists(remoteFilePath))
                    {
                        client.DeleteFile(remoteFilePath);
                        client.Disconnect();

                        _logger.LogInformation($"File {fileName} deleted successfully.");
                        return Ok(new { Message = $"File {fileName} deleted successfully." });
                    }
                    else
                    {
                        _logger.LogWarning($"File {fileName} does not exist on the SFTP server.");
                        return NotFound($"File {fileName} does not exist.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting file: {ex.Message}");
                return StatusCode(500, $"Error deleting file: {ex.Message}");
            }
        }
    }
}
