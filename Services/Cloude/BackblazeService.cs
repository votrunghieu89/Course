using Amazon.S3.Model;
using Amazon.S3;
using E_learning.Model.cloudeDB;
using Microsoft.Extensions.Options;

namespace E_learning.Services.Cloude
{
    public class BackblazeService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly BackBlazeModel _settings;
        private readonly ILogger<BackblazeService> _logger;

        public BackblazeService(IAmazonS3 s3Client, IOptions<BackBlazeModel> settings, ILogger<BackblazeService> logger)
        {
            _s3Client = s3Client;
            _settings = settings.Value;
            _logger = logger;
        }
        public async Task UploadFileAsync(string filePath, string key)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

            var request = new PutObjectRequest
            {
                BucketName = _settings.BucketName,   // Tên bucket trong appsettings
                Key = key,                            // Đường dẫn file trên B2 (vd: "video/index.m3u8")
                InputStream = stream,                 // Luồng đọc file local
                ContentType = "application/octet-stream" // MIME type chung, có thể sửa cho .ts, .m3u8
            };

            var response = await _s3Client.PutObjectAsync(request);
        }
        public async Task UploadFolderAsync(string folderPath, string lessonId)
        {
            var files = Directory.GetFiles(folderPath);
            foreach (var file in files)
            {
                var key = $"videos/{lessonId}/{Path.GetFileName(file)}";
                await UploadFileAsync(file, key);
            }
        }
        public string GetSignedUrl(string key, int expiresSeconds = 3600)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _settings.BucketName,
                Key = key, // Đường dẫn file (vd: "video1/index.m3u8")
                Expires = DateTime.UtcNow.AddSeconds(expiresSeconds)
            };

            return _s3Client.GetPreSignedURL(request);
        }

    }
}
