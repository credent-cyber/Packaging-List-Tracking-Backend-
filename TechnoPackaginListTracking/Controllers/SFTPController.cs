using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Renci.SshNet;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using TechnoPackaginListTracking.Dto;
using IO = System.IO;

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
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public SFTPController(IConfiguration configuration, ILogger<SFTPController> logger, IWebHostEnvironment env)
        {
            _host = configuration["SftpSettings:Host"];
            _port = int.Parse(configuration["SftpSettings:Port"]);
            _username = configuration["SftpSettings:Username"];
            _password = configuration["SftpSettings:Password"];
            _remoteUploadPath = configuration["SftpSettings:RemoteUploadPath"];
            _logger = logger;
            _env = env;
            _configuration = configuration;
        }

        [HttpPost("AppendFile/{fragment}/{totalChunks}")]
        public async Task<UploadResult> UploadFileChunk([FromForm] int fragment, [FromForm] int totalChunks, [FromForm] IFormFile file, [FromForm] string user, [FromForm] string requestId)
        {
            try
            {
                // Generate the remote directory path based on requestId
                var remoteDirectory = $"{requestId}";

                // Ensure the file has a proper extension
                var fileExtension = Path.GetExtension(file.FileName);
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.FileName);

                // Generate a unique file name by appending the user and file extension
                var uniqueFileName = $"{fileNameWithoutExtension}_{user}{fileExtension}";

                // Full remote path where the file will be uploaded (including file extension)
                var remoteFilePath = $"{remoteDirectory}/{uniqueFileName}";

                // Connect to the SFTP server
                using (var sftpClient = new SftpClient(_host, _port, _username, _password))
                {
                    sftpClient.Connect();
                    if (!sftpClient.IsConnected)
                    {
                        return new UploadResult { IsUploaded = false, Message = "Failed to connect to the SFTP server." };
                    }

                    // Check if the remote directory exists; create it if not
                    if (!sftpClient.Exists(remoteDirectory))
                    {
                        _logger.LogInformation($"Creating directory: {remoteDirectory}");
                        sftpClient.CreateDirectory(remoteDirectory);
                    }

                    // Open a stream to append the current chunk to the remote file
                    using (var fileStream = file.OpenReadStream())
                    {
                        // If it's the first chunk and the file exists, delete the old file
                        if (fragment == 0 && sftpClient.Exists(remoteFilePath))
                        {
                            sftpClient.DeleteFile(remoteFilePath);
                        }

                        // Upload the current chunk to the remote file
                        _logger.LogInformation($"Uploading chunk {fragment + 1}/{totalChunks} to {remoteFilePath}");
                        sftpClient.UploadFile(fileStream, remoteFilePath, true);
                    }

                    sftpClient.Disconnect();
                }

                // Check if this is the last chunk
                if (fragment == totalChunks - 1)
                {
                    return new UploadResult { IsUploaded = true, FileLocation = uniqueFileName, Message = "File uploaded successfully to SFTP." };
                }

                return new UploadResult { IsUploaded = true, FileLocation = uniqueFileName, Message = $"Chunk {fragment + 1}/{totalChunks} uploaded successfully." };
            }
            catch (Renci.SshNet.Common.SftpPathNotFoundException ex)
            {
                _logger.LogError($"SFTP path not found: {ex.Message}");
                return new UploadResult { IsUploaded = false, Message = $"SFTP path not found: {ex.Message}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while uploading file chunk.");
                return new UploadResult { IsUploaded = false, Message = $"Error: {ex.Message}" };
            }
        }


        [HttpGet("DownloadFolder/{requestId}")]
        public async Task<IActionResult> DownloadFolderAsZip(string requestId)
        {
            try
            {
                // Path of the remote folder to download
                var remoteFolderPath = $"{requestId}";
                var zipFilePath = Path.Combine(_env.ContentRootPath, $"{requestId}.zip");

                // Connect to the SFTP server
                using (var sftpClient = new SftpClient(_host, _port, _username, _password))
                {
                    sftpClient.Connect();
                    if (!sftpClient.IsConnected)
                    {
                        return BadRequest("Failed to connect to the SFTP server.");
                    }

                    // Check if the remote directory exists
                    if (!sftpClient.Exists(remoteFolderPath))
                    {
                        return NotFound("The specified folder does not exist on the SFTP server.");
                    }

                    // Create a zip file
                    using (var zipFileStream = new FileStream(zipFilePath, FileMode.Create))
                    using (var zipArchive = new ZipArchive(zipFileStream, ZipArchiveMode.Create, true))
                    {
                        // List all files in the remote directory
                        var remoteFiles = sftpClient.ListDirectory(remoteFolderPath);

                        foreach (var remoteFile in remoteFiles)
                        {
                            // Ignore directories and add files to the zip
                            if (!remoteFile.IsDirectory)
                            {
                                using (var fileStream = new MemoryStream())
                                {
                                    sftpClient.DownloadFile(remoteFile.FullName, fileStream);
                                    fileStream.Position = 0; // Reset the position of the stream

                                    // Create an entry in the zip file
                                    var zipEntry = zipArchive.CreateEntry(remoteFile.Name);
                                    using (var entryStream = zipEntry.Open())
                                    {
                                        await fileStream.CopyToAsync(entryStream);
                                    }
                                }
                            }
                        }
                    }

                    sftpClient.Disconnect();
                }

                // Return the zip file as a downloadable response
                var zipFileBytes = System.IO.File.ReadAllBytes(zipFilePath);
                System.IO.File.Delete(zipFilePath); // Clean up the zip file after reading

                return File(zipFileBytes, "application/zip", $"{requestId}.zip");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while downloading folder.");
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        //[HttpPost("UploadExcelToFtp/{requestId}")]
        public async Task<UploadResult> UploadFileToFtp(string excelFilePath, string remoteDirectory, string packingListId)
        {
            try
            {
                // Ensure the file has a valid extension
                var fileExtension = Path.GetExtension(excelFilePath);
                var fileName = $"{packingListId}_PackingList{fileExtension}";
                var remoteFilePath = $"{remoteDirectory}/{fileName}";

                using (var sftpClient = new SftpClient(_host, _port, _username, _password))
                {
                    sftpClient.Connect();
                    if (!sftpClient.IsConnected)
                    {
                        return new UploadResult { IsUploaded = false, Message = "Failed to connect to the SFTP server." };
                    }

                    // Create remote directory if it doesn't exist
                    if (!sftpClient.Exists(remoteDirectory))
                    {
                        _logger.LogInformation($"Creating directory: {remoteDirectory}");
                        sftpClient.CreateDirectory(remoteDirectory);
                    }

                    // Upload the file
                    using (var fileStream = System.IO.File.OpenRead(excelFilePath))
                    {
                        sftpClient.UploadFile(fileStream, remoteFilePath);
                    }

                    sftpClient.Disconnect();
                }

                return new UploadResult { IsUploaded = true, FileLocation = remoteFilePath, Message = "File uploaded successfully." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file to FTP.");
                return new UploadResult { IsUploaded = false, Message = $"Error: {ex.Message}" };
            }
        }

        [HttpDelete("DeleteFile/{requestId}/{fileName}")]
        public async Task<IActionResult> DeleteFile(string requestId, string fileName)
        {
            try
            {
                // Generate the remote file path
                var remoteFilePath = $"{requestId}/{fileName}";

                // Connect to the SFTP server
                using (var sftpClient = new SftpClient(_host, _port, _username, _password))
                {
                    sftpClient.Connect();
                    if (!sftpClient.IsConnected)
                    {
                        return BadRequest("Failed to connect to the SFTP server.");
                    }

                    // Check if the file exists
                    if (!sftpClient.Exists(remoteFilePath))
                    {
                        return NotFound($"File '{fileName}' does not exist in the folder '{requestId}'.");
                    }

                    // Delete the file
                    sftpClient.DeleteFile(remoteFilePath);
                    sftpClient.Disconnect();
                }

                return Ok($"File '{fileName}' deleted successfully from folder '{requestId}'.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while deleting file.");
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }


    }
}
