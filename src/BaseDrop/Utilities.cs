using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BaseDrop
{
    internal class Utilities
    {
        // A lightning fast search of bytes in a chunk of memory
        public static unsafe long LightningSearch(ref byte[] haystack, ref byte[] needle, long startOffset = 0)
        {
            fixed (byte* h = haystack) fixed (byte* n = needle)
            {
                for (byte* hNext = h + startOffset, hEnd = h + haystack.LongLength + 1 - needle.LongLength, nEnd = n + needle.LongLength; hNext < hEnd; hNext++)
                    for (byte* hInc = hNext, nInc = n; *nInc == *hInc; hInc++)
                        if (++nInc == nEnd)
                            return hNext - h;
                return -1;
            }
        }

        public static T ByteArrayToStruct<T>(ref byte[] Data)
        {
            // Cast it
            GCHandle handle = GCHandle.Alloc(Data, GCHandleType.Pinned);
            // Make it
            T theStructure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            // Free handle
            handle.Free();
            // Return it
            return theStructure;
        }
    }
}
