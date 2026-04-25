using CloudinaryDotNet.Actions;

namespace PBL3_DUTLibrary.Interfaces
{
    public interface IPhotoService
    {
        Task<ImageUploadResult> AddPhotoAsync(IFormFile file);
        Task<DeletionResult> DeletePhotoAsync(string publicid);
    }
}
