using System;
using System.Runtime.Serialization;

namespace Unisave.Heapstore.Backend
{
    [Serializable]
    public class HeapstoreException : Exception
    {
        public int ErrorNumber { get; }
        
        public string ErrorMessage { get; }
        
        public HeapstoreException(int errorNumber, string errorMessage)
            : this($"[ERROR {errorNumber}] {errorMessage}")
        {
            ErrorNumber = errorNumber;
            ErrorMessage = errorMessage;
        }
        
        public HeapstoreException() { }

        public HeapstoreException(string message) : base(message) { }

        public HeapstoreException(string message, Exception inner)
            : base(message, inner) { }
        
        protected HeapstoreException(
            SerializationInfo info,
            StreamingContext context
        ) : base(info, context)
        {
            ErrorNumber = info.GetInt32(nameof(ErrorNumber));
            ErrorMessage = info.GetString(nameof(ErrorMessage));
        }

        public override void GetObjectData(
            SerializationInfo info,
            StreamingContext context
        )
        {
            if (info == null)
                throw new ArgumentNullException(nameof (info));
            
            info.AddValue("ErrorNumber", ErrorNumber);
            info.AddValue("ErrorMessage", ErrorMessage);
            
            base.GetObjectData(info, context);
        }
    }
}