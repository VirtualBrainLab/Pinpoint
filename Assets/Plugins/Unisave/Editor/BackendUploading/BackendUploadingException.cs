using System;

namespace Unisave.Editor.BackendUploading
{
    /// <summary>
    /// Thrown by the backend uploader
    /// </summary>
    public class BackendUploadingException : Exception
    {
        public BackendUploadingException()
        {
        }

        public BackendUploadingException(string message) : base(message)
        {
        }

        public BackendUploadingException(string message, Exception inner) : base(
            message, inner)
        {
        }
    }
}