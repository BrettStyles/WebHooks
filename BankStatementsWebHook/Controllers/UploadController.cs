using System.IO;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.File;
using Swashbuckle.Swagger.Annotations;

namespace BankStatementsWebHook.Controllers
{
    [RoutePrefix("BankStatementsWebHook")]
    public class UploadController : ApiController
    {
        // POST api/values
        [SwaggerOperation("Create")]
        [SwaggerResponse(HttpStatusCode.Created)]
        [HttpPost, Route("Statements/{applicationId}")]
        public HttpResponseMessage UploadFile([FromUri]string applicationId)
        {            
            // Check if the request contains multipart/form-data.
            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            var connection = CloudConfigurationManager.GetSetting("StorageConnectionString");

            var storageAccount = CloudStorageAccount.Parse(connection);

            HttpFileCollection files = HttpContext.Current.Request.Files;

            foreach (var key in files.AllKeys)
            {
                var file = files[key];

                if (file != null && file.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(file.FileName);

                    WriteFile(storageAccount, applicationId, fileName, file.InputStream);
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        private void WriteFile(CloudStorageAccount storageAccount, string applicationId , string filename, Stream inputStream)
        {
            // Create a CloudFileClient object for credentialed access to File storage.
            CloudFileClient fileClient = storageAccount.CreateCloudFileClient();

            // Get a reference to the file share we created previously.
            CloudFileShare share = fileClient.GetShareReference("statements");

            // Ensure that the share exists.
            if (share.Exists())
            {
                // Get a reference to the root directory for the share.
                CloudFileDirectory rootDir = share.GetRootDirectoryReference();

                var applicationDir = rootDir.GetDirectoryReference(applicationId);

                applicationDir.CreateIfNotExists();

                var fileRef = applicationDir.GetFileReference(filename);

                fileRef.UploadFromStream(inputStream);
            }
        }
    }
}
