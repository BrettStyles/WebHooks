using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.File;
using Newtonsoft.Json.Linq;
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

            var formKeys = HttpContext.Current.Request.Form.AllKeys;
            if (formKeys.Contains("data"))
            {
                var data = HttpContext.Current.Request.Form["data"];

                WriteFile(storageAccount, applicationId, "accounts.json", data);
            }

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

        private void WriteFile(CloudStorageAccount storageAccount, string applicationId, string filename, string content)
        {
            GetFileReference(storageAccount, applicationId, filename, fileRef => fileRef.UploadText(content));
        }

        private void WriteFile(CloudStorageAccount storageAccount, string applicationId, string filename, Stream inputStream)
        {
            GetFileReference(storageAccount,applicationId,filename, fileRef => fileRef.UploadFromStream(inputStream));
        }

        private void GetFileReference(CloudStorageAccount storageAccount, string applicationId, string filename, Action<CloudFile> callback) { 

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

                callback(applicationDir.GetFileReference(filename));
            }
        }
    }
}
