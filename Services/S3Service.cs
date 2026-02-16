using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;

namespace OrderManagementSystem.Services
{
    public class S3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;

        public S3Service(IConfiguration configuration)
        {
            var accessKey = configuration["AWS:AccessKey"];
            var secretKey = configuration["AWS:SecretKey"];
            var region = Amazon.RegionEndpoint.GetBySystemName(configuration["AWS:Region"]);

            _bucketName = configuration["AWS:BucketName"];

            _s3Client = new AmazonS3Client(accessKey, secretKey, region);
        }

        public async Task<string> UploadFileAsync(IFormFile file)
        {
            using var stream = file.OpenReadStream();

            var fileKey = $"{Guid.NewGuid()}_{file.FileName}";

            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = fileKey,
                InputStream = stream,
                ContentType = file.ContentType
            };

            await _s3Client.PutObjectAsync(request);

            // Return only the key (NOT URL)
            return fileKey;
        }


        public string GetPreSignedUrl(string key, string fileName)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = key,
                Expires = DateTime.UtcNow.AddMinutes(5),
                ResponseHeaderOverrides = new ResponseHeaderOverrides
                {
                    ContentDisposition = $"attachment; filename=\"{fileName}\""
                }
            };

            return _s3Client.GetPreSignedURL(request);
        }

    }
}
