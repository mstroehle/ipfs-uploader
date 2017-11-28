using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

using Uploader.Attributes;
using Uploader.Helper;
using Uploader.Managers.Common;
using Uploader.Managers.Ipfs;
using Uploader.Managers.Overlay;
using Uploader.Managers.Video;
using Uploader.Models;

namespace Uploader.Controllers
{
    [Route("uploader")]
    public class UploaderController : Controller
    {
        [HttpPost]
        [DisableFormValueModelBinding]
        [DisableRequestSizeLimit]
        [Route("/uploadVideo")]
        public async Task<IActionResult> UploadVideo(string videoEncodingFormats = null, bool? sprite = null)
        {
            try
            {
                return Ok(new
                {
                    success = true, token = VideoManager.ComputeVideo(await GetFileToTemp(), videoEncodingFormats, sprite)
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception Upload Video : {0}", ex);
                return BadRequest(new
                {
                    errorMessage = ex.Message
                });
            }
        }

        [HttpPost]
        [DisableFormValueModelBinding]
        [DisableRequestSizeLimit]
        [Route("/uploadImage")]
        public async Task<IActionResult> UploadImage(bool? overlay = null)
        {
            try
            {
                string sourceFilePath = await GetFileToTemp();
                FileContainer fileContainer = FileContainer.NewImageContainer(sourceFilePath);

                // si pas d'option overlay, c'est qu'on veut juste ipfs add l'image
                if (!(overlay??false))
                {
                    IpfsDaemon.Queue(fileContainer.SourceFileItem);
                }
                else
                {
                    OverlayManager.ComputeOverlay(fileContainer, overlay);
                }

                return Ok(new
                {
                    success = true, token = fileContainer.ProgressToken
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception Upload Image : {0}", ex);
                return BadRequest(new
                {
                    errorMessage = ex.Message
                });
            }
        }

        private async Task<string> GetFileToTemp()
        {
            // Copy file to temp location
            string sourceFilePath = TempFileManager.GetNewTempFilePath();

            try
            {
                // Récupération du fichier
                FormValueProvider formModel;
                using(FileStream stream = System.IO.File.Create(sourceFilePath))
                {
                    formModel = await Request.StreamFile(stream);
                }
                //var fileName = formModel.GetValue("qqFileName");
                return sourceFilePath;
            }
            catch(Exception ex)
            {
                Debug.WriteLine("Exception Download File : {0}", ex);
                TempFileManager.SafeDeleteTempFile(sourceFilePath);
                throw;
            }
        }
    }
}