using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Unisave.Editor
{
    /// <summary>
    /// Helper class for hashing
    /// </summary>
    public static class Hash
    {
        /// <summary>
        /// Computes composite hash of multiple MD5 hashes.
        /// Sorts the partial hashes first so the order doesn't matter.
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static string CompositeMD5(IEnumerable<string> partialHashes)
        {
            var partials = new List<string>(partialHashes);
            
            // keep the hashes sorted to make the result order invariant
            partials.Sort();

            // compose the hashes into a single string
            string compositeText = string.Join("-", partials);
            
            // get bytes (no special characters present so ascii is enough)
            byte[] bytes = Encoding.ASCII.GetBytes(compositeText);
            
            // hash the composite
            return MD5(bytes);
        }
        
        /// <summary>
        /// Computes MD5 hash of some data
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static string MD5(byte[] subject)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(subject);

                return BytesToHexaString(hashBytes);
            }
        }
        
        /// <summary>
        /// Computes MD5 hash of some string
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static string MD5(string subject)
        {
            return MD5(Encoding.ASCII.GetBytes(subject));
        }

        /// <summary>
        /// Converts bytes to a hexadecimal string
        /// </summary>
        public static string BytesToHexaString(byte[] subject)
        {
            var sb = new StringBuilder();
            
            for (int i = 0; i < subject.Length; i++)
                sb.Append(subject[i].ToString("x2"));
            
            return sb.ToString();
        }
    }
}