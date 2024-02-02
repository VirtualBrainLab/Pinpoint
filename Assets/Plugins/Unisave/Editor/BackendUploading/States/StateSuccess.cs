namespace Unisave.Editor.BackendUploading.States
{
    public class StateSuccess : BaseState
    {
        public string CompilerOutput { get; }
        
        public StateSuccess(string compilerOutput)
        {
            CompilerOutput = compilerOutput;
        }
    }
}