using System;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;

namespace UserService.Services.S3;

public class S3Service : IS3Service
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly ILogger<S3Service> logger;

    public S3Service(IConfiguration configuration)
    {
        var awsConfig = configuration.GetSection("AWS");

        var accessKey = awsConfig["AccessKey"];
        var secretKey = awsConfig["SecretKey"];
        var serviceUrl = awsConfig["ServiceUrl"];
        var region = awsConfig["Region"] ?? "us-east-1";

        _bucketName = awsConfig["BucketName"]
            ?? throw new ArgumentNullException("AWS:BucketName Configuration is missing.");

        var s3Config = new AmazonS3Config();

        if (!string.IsNullOrEmpty(serviceUrl))
        {
            s3Config.ServiceURL = serviceUrl;
            s3Config.ForcePathStyle = true;
        }
        else
        {
            s3Config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region);
        }

        var credentials = new Amazon.Runtime.BasicAWSCredentials(accessKey, secretKey);
        _s3Client = new AmazonS3Client(credentials, s3Config);
    }

    public async Task EnsureBucketExistsAsync(CancellationToken cancellationToken = default)
    {
        var bucketExists = await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, _bucketName);

        if (!bucketExists)
        {
            var putBucketRequest = new PutBucketRequest
            {
                BucketName = _bucketName,
                UseClientRegion = true
            };

            await _s3Client.PutBucketAsync(putBucketRequest, cancellationToken);
        }
    }

    public string GeneratePresignedUrlForUpload(string userId, string contentType, int expirationInMinutes = 5)
    {
        var fileKey = $"profile-pictures/{userId}.webp";

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = fileKey,
            Verb = HttpVerb.PUT,
            Expires = DateTime.Now.AddMinutes(expirationInMinutes),
            ContentType = contentType
        };

        return _s3Client.GetPreSignedURL(request);
    }
}
