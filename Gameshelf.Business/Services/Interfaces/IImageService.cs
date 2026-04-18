using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameShelf.Business.Services.Interfaces
{
    /// <summary>
    /// Defines a service for handling file system operations related to images.
    /// This service abstracts the logic for saving and deleting files from the server's web root directory.
    /// </summary>
    public interface IImageService
    {
        /// <summary>
        /// Asynchronously saves an image to the wwwroot folder
        /// </summary>
        /// <param name="file">The file from the form</param>
        /// <param name="folderName">Subfolder name (e.g. "restaurants")</param>
        /// <returns>The relative path to the image (for DB storage)</returns>
        Task<string> UploadImageAsync(IFormFile file, string folderName);

        /// <summary>
        /// Deletes an image from the physical storage
        /// </summary>
        /// <param name="imageUrl">Relative path stored in DB</param>
        void DeleteImage(string imageUrl);
    }
}
