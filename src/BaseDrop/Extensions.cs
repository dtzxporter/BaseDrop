using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BaseDrop
{
    internal static class Extensions
    {
        /// <summary>
        /// Read a structure from a binary file
        /// </summary>
        /// <typeparam name="T">Structure type</typeparam>
        /// <param name="reader">The reader of which to read from</param>
        /// <returns>The parsed structure</returns>
        public static T ReadStruct<T>(this BinaryReader reader)
        {
            // Read bytes
            byte[] bytes = reader.ReadBytes(Marshal.SizeOf(typeof(T)));
            // Allocate it
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            // Cast the type to the pointer of the data
            T theStructure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            // Free the memory segment
            handle.Free();
            // Return it
            return theStructure;
        }
    }
}
