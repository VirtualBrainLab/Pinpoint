using System;

namespace Unisave.Editor.BackendUploading.States
{
    public class StateException : BaseState
    {
        public string ExceptionMessage { get; private set; }
        
        public string ExceptionBody { get; private set; }
        
        public StateException(Exception e)
        {
            ExceptionMessage = e.Message;
            ExceptionBody = e.ToString();
        }
    }
}