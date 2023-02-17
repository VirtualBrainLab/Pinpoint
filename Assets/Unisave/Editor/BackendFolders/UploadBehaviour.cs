namespace Unisave.Editor.BackendFolders
{
    /// <summary>
    /// Upload behaviours of a backend folder
    /// </summary>
    public class UploadBehaviour
    {
        /// <summary>
        /// The folder should never be uploaded
        /// </summary>
        public static readonly UploadBehaviour Never = new UploadBehaviour("never");
        
        /// <summary>
        /// The folder should always be uploaded
        /// </summary>
        public static readonly UploadBehaviour Always = new UploadBehaviour("always");
        
        /// <summary>
        /// The folder is uploaded only when its referenced from a scene
        /// that is active or is to be built.
        /// </summary>
        public static readonly UploadBehaviour CheckScenes = new UploadBehaviour("check-scenes");
        
        /// <summary>
        /// String value of the upload behaviour
        /// </summary>
        public string Value { get; }

        private UploadBehaviour(string value)
        {
            Value = value;
        }

        public static UploadBehaviour FromString(string value)
        {
            switch (value)
            {
                case "always":
                    return Always;
                
                case "never":
                    return Never;
                
                case "check-scenes":
                    return CheckScenes;
            }
            
            return Never;
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(UploadBehaviour a, UploadBehaviour b)
        {
            return a?.Value == b?.Value;
        }
        
        public static bool operator !=(UploadBehaviour a, UploadBehaviour b)
        {
            return a?.Value != b?.Value;
        }
        
        protected bool Equals(UploadBehaviour other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((UploadBehaviour) obj);
        }

        public override int GetHashCode()
        {
            return (Value != null ? Value.GetHashCode() : 0);
        }
    }
}