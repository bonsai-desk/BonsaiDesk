using System;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace Dissonance.Extensions
{
    public static class ArraySegmentExtensions
    {
        /// <summary>
        /// Copy from the given array segment into the given array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="destinationOffset"></param>
        /// <returns>The segment of the destination array which was written into</returns>
        public static ArraySegment<T> CopyTo<T>(this ArraySegment<T> source, [NotNull] T[] destination, int destinationOffset = 0)
            where T : struct
        {
            if (destination == null)
                throw new ArgumentNullException("destination");
            if (source.Count > destination.Length - destinationOffset)
                throw new ArgumentException("Insufficient space in destination array", "destination");

            // ReSharper disable once AssignNullToNotNullAttribute
            Array.Copy(source.Array, source.Offset, destination, destinationOffset, source.Count);

            return new ArraySegment<T>(destination, destinationOffset, source.Count);
        }

        /// <summary>
        /// Copy as many samples as possible from the source array into the segment
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="destination"></param>
        /// <param name="source"></param>
        /// <returns>The number of samples copied</returns>
        internal static int CopyFrom<T>(this ArraySegment<T> destination, [NotNull] T[] source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            var count = Math.Min(destination.Count, source.Length);
            // ReSharper disable once AssignNullToNotNullAttribute (Justification: Array segment cannot be null)
            Array.Copy(source, 0, destination.Array, destination.Offset, count);
            return count;
        }

        [NotNull] internal static T[] ToArray<T>(this ArraySegment<T> segment)
            where T : struct
        {
            var arr = new T[segment.Count];
            segment.CopyTo(arr);
            return arr;
        }

        internal static void Clear<T>(this ArraySegment<T> segment)
        {
            // ReSharper disable once AssignNullToNotNullAttribute (Justification: Array segment cannot be null)
            Array.Clear(segment.Array, segment.Offset, segment.Count);
        }

        /// <summary>
        /// Pin the array and return a pointer to the start of the segment
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="segment"></param>
        /// <returns></returns>
        internal static DisposableHandle Pin<T>(this ArraySegment<T> segment) where T : struct
        {
            var handle = GCHandle.Alloc(segment.Array, GCHandleType.Pinned);

            var size = Marshal.SizeOf(typeof(T));
            var ptr = new IntPtr(handle.AddrOfPinnedObject().ToInt64() + segment.Offset * size);

            return new DisposableHandle(ptr, handle);
        }

        internal struct DisposableHandle
            : IDisposable
        {
            private readonly IntPtr _ptr;
            private GCHandle _handle;

            public IntPtr Ptr
            {
                get
                {
                    if (!_handle.IsAllocated)
                        throw new ObjectDisposedException("GC Handle has already been freed");
                    return _ptr;
                }
            }

            internal DisposableHandle(IntPtr ptr, GCHandle handle)
            {
                _ptr = ptr;
                _handle = handle;
            }

            public void Dispose()
            {
                _handle.Free();
            }
        }
    }
}
