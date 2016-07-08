using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.File;
using Swashbuckle.Swagger.Annotations;
using System.Threading.Tasks;

namespace BankStatementsWebHook.Controllers
{
    [RoutePrefix("Upload")]
    public class UploadController : ApiController
    {
        // POST api/values
        [SwaggerOperation("Create")]
        [SwaggerResponse(HttpStatusCode.Created)]
        [HttpPost, Route("File")]
        public async Task<HttpResponseMessage> UploadFile()
        {
            var connection = CloudConfigurationManager.GetSetting("StorageConnectionString");

            var storageAccount = CloudStorageAccount.Parse(connection);

            //    var file = HttpContext.Current.Request.Files.Count > 0 ? HttpContext.Current.Request.Files[0] : null;

            //    if (file != null && file.ContentLength > 0)
            //    {
            //        var fileName = Path.GetFileName(file.FileName);

            //        WriteFile(storageAccount, fileName, file.InputStream);
            //    }

            // Check if the request contains multipart/form-data.
            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            var root = HttpContext.Current.Server.MapPath("~/App_Data");
            var provider = new MultipartFormDataStreamProvider(root);

            // Read the form data and return an async task.
            return await Request.Content.ReadAsMultipartAsync(provider).
                ContinueWith(t =>
                {
                    if (t.IsFaulted || t.IsCanceled)
                    {
                        Request.CreateErrorResponse(HttpStatusCode.InternalServerError, t.Exception);
                    }

                    // This illustrates how to get the file names.
                    foreach (var file in provider.FileData)
                    {
                        var filename = file.Headers.ContentDisposition.FileName;

                        Trace.WriteLine(filename);
                        Trace.WriteLine("Server file path: " + file.LocalFileName);

                        //WriteFile(storageAccount, filename, file.InputStream);
                    }
                    return Request.CreateResponse(HttpStatusCode.OK);
                });
        }

        private void WriteFile(CloudStorageAccount storageAccount, string filename, Stream inputStream)
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

                var fileRef = rootDir.GetFileReference(filename);

                fileRef.UploadFromStream(inputStream);
            }
        }
    }
}
