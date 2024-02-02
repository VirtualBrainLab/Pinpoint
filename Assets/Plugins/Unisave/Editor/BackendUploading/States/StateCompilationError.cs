namespace Unisave.Editor.BackendUploading.States
{
    public class StateCompilationError : BaseState
    {
        public string CompilerOutput { get; }
        
        public StateCompilationError(string compilerOutput)
        {
            CompilerOutput = compilerOutput;
        }
    }
}