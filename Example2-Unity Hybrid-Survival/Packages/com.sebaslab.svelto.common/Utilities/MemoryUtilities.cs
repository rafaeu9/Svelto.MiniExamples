#if DEBUG && !PROFILE_SVELTO
#define DEBUG_MEMORY
#endif

#if UNITY_5_3_OR_NEWER
#if !UNITY_2019_3_OR_NEWER
#error Svelto.ECS 3.0 supports Unity 2019_3 and above only
#else
// Unity.Collections.LowLevel.Unsafe.UnsafeUtility is not part of Unity Collection but it comes with Unity
#define UNITY_COLLECTIONS
#endif
#endif

#if !UNITY_COLLECTIONS
using System.Runtime.InteropServices;
#endif

using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Svelto.Common
{
#if !UNITY_COLLECTIONS
    public enum Allocator
    {
        Invalid
     ,

        //no allocation needed
        None
     ,

        //temporary allocation, it doesn't have any meaning outside unity atm
        Temp
     ,

        //temporary allocation, it doesn't have any meaning outside unity atm
        TempJob
     ,

        //persistent native allocation, must be disposed of
        Persistent
     ,

        //managed allocation
        Managed
    }
#else
    public enum Allocator
    {
        /// <summary>
        ///   <para>Invalid allocation.</para>
        /// </summary>
        Invalid = Unity.Collections.Allocator.Invalid

       ,

        /// <summary>
        ///   <para>No allocation.</para>
        /// </summary>
        None = Unity.Collections.Allocator.None

       ,

        /// <summary>
        ///   <para>Temporary allocation.</para>
        /// </summary>
        Temp = Unity.Collections.Allocator.Temp

       ,

        /// <summary>
        ///   <para>Temporary job allocation.</para>
        /// </summary>
        TempJob = Unity.Collections.Allocator.TempJob

       ,

        /// <summary>
        ///   <para>Persistent allocation.</para>
        /// </summary>
        Persistent = Unity.Collections.Allocator.Persistent

      , Managed
    }
#endif

    public static class MemoryUtilities
    {
        public static IntPtr Alloc(uint newCapacityInBytes, Allocator allocator, bool clear = true)
        {
            var signedCapacity = (int) SignedCapacity(newCapacityInBytes);
            IntPtr newPointer = IntPtr.Zero;
#if UNITY_COLLECTIONS
            var allocator1 = (Unity.Collections.Allocator) allocator;
            unsafe
            {
                var tempPointer =
                    Unity.Collections.LowLevel.Unsafe.UnsafeUtility.Malloc(
                        signedCapacity, (int) OptimalAlignment.alignment, allocator1);

                newPointer = (IntPtr) tempPointer;
            }
#else
            newPointer = Marshal.AllocHGlobal(signedCapacity);
#endif
            //Note MemClear is actually necessary
            if (clear && newCapacityInBytes > 0)
                MemClear(newPointer, (uint) signedCapacity);

            var signedPointer = SignedPointer(newCapacityInBytes, newPointer);

            CheckBoundaries(newPointer);

            return signedPointer;
        }

        public static IntPtr Realloc
        (IntPtr realBuffer, uint newCapacityInBytes, Allocator allocator, uint numberOfElementsToCopyInBytes
       , bool copy = true, bool memClear = true)
        {
            unsafe
            {
                //Alloc returns the correct Signed Pointer already
                //if copy == true, memclear is optimised, otherwise memclear if set to true
                var signedPointer = Alloc(newCapacityInBytes, allocator, copy == false && memClear == true);

                //Copy only the real data
                if (copy && numberOfElementsToCopyInBytes > 0)
                {
                    if (newCapacityInBytes > numberOfElementsToCopyInBytes)
                    {
                        Unsafe.CopyBlock((void*) signedPointer, (void*) realBuffer, numberOfElementsToCopyInBytes);
                        if (memClear)
                        {
                            var bytesToClear               = newCapacityInBytes - numberOfElementsToCopyInBytes;
                            var startingBytePointerToClear = signedPointer + (int) numberOfElementsToCopyInBytes;
                            MemClear(startingBytePointerToClear, bytesToClear);
                        }
                    }
                    else
                        Unsafe.CopyBlock((void*) signedPointer, (void*) realBuffer, newCapacityInBytes);
                }

                //Free unsigns the pointer itself
                Free(realBuffer, allocator);
                return signedPointer;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr Alloc<T>(uint newCapacity, Allocator allocator, bool clear = true) where T : struct
        {
            var newCapacityInBytes = (uint) (SizeOf<T>() * newCapacity);

            return Alloc(newCapacityInBytes, allocator, clear);
        }

        public static IntPtr Realloc<T>
        (IntPtr realBuffer, uint newCapacity, Allocator allocator, uint numberOfElementsToCopy, bool copy = true
       , bool memClear = true) where T : struct
        {
            var sizeOf                        = SizeOf<T>();
            var newCapacityInBytes            = (uint) (sizeOf * newCapacity);
            var numberOfElementsToCopyInBytes = (uint) (sizeOf * numberOfElementsToCopy);

            return Realloc(realBuffer, newCapacityInBytes, allocator, numberOfElementsToCopyInBytes, copy, memClear);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Free(IntPtr ptr, Allocator allocator)
        {
            ptr = CheckAndReturnPointerToFree(ptr);

#if UNITY_COLLECTIONS
            unsafe
            {
                Unity.Collections.LowLevel.Unsafe.UnsafeUtility.Free(
                    (void*) ptr, (Unity.Collections.Allocator) allocator);
            }
#else
            Marshal.FreeHGlobal(ptr);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MemClear<T>(IntPtr destination, uint sizeOf) where T : struct
        {
            unsafe
            {
                var sizeOfInBytes = (uint) (SizeOf<T>() * sizeOf);
#if UNITY_COLLECTIONS
                Unity.Collections.LowLevel.Unsafe.UnsafeUtility.MemClear((void*) destination, sizeOfInBytes);
#else
                Unsafe.InitBlock((void*) destination, 0, sizeOfInBytes);
#endif
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MemClear(IntPtr destination, uint sizeOfInBytes)
        {
            unsafe
            {
#if UNITY_COLLECTIONS
                Unity.Collections.LowLevel.Unsafe.UnsafeUtility.MemClear((void*) destination, sizeOfInBytes);
#else
                Unsafe.InitBlock((void*) destination, 0, sizeOfInBytes);
#endif
            }
        }

        /// <summary>
        /// Like Memcpy but safe when memory overlaps
        /// </summary>
        public static void Memmove<T>(IntPtr source, uint sourceStartIndex, uint destinationStartIndex, uint size)
            where T : struct
        {
            unsafe
            {
                var sizeOf        = SizeOf<T>();
                var sizeOfInBytes = (uint) (sizeOf * size);
                //issues cpblk that assumes that both the source and destination addressed are aligned to the natural size of the machine.
                Unsafe.CopyBlock((void*) (source + (int) destinationStartIndex * sizeOf)
                               , (void*) (source + (int) sourceStartIndex * sizeOf), sizeOfInBytes);
            }
        }

        public static void Memcpy<T>
            (IntPtr source, uint sourceStartIndex, IntPtr destination, uint destinationStartIndex, uint size)
            where T : struct
        {
            unsafe
            {
                var sizeOf        = SizeOf<T>();
                var sizeOfInBytes = (uint) (sizeOf * size);
                Buffer.MemoryCopy((void*) (source + (int) sourceStartIndex * sizeOf)
                                , (void*) (destination + (int) destinationStartIndex * sizeOf), sizeOfInBytes
                                , sizeOfInBytes);
            }
        }

#if UNITY_COLLECTIONS
        static class OptimalAlignment
        {
            internal static readonly uint alignment;

            static OptimalAlignment() { alignment = (uint) (Environment.Is64BitProcess ? 16 : 8); }
        }
#endif
        static class CachedSize<T> where T : struct
        {
            public static readonly uint cachedSize        = (uint) Unsafe.SizeOf<T>();
            public static readonly uint cachedSizeAligned = Align4(cachedSize);
        }

        //THIS MUST STAY INT. THE REASON WHY EVERYTHING IS INT AND NOT UINT IS BECAUSE YOU CAN END UP
        //DOING SUBTRACT OPERATION EXPECTING TO BE < 0 AND THEY WON'T BE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOf<T>() where T : struct { return (int) CachedSize<T>.cachedSize; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOfAligned<T>() where T : struct { return (int) CachedSize<T>.cachedSizeAligned; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyStructureToPtr<T>(ref T buffer, IntPtr bufferPtr) where T : struct
        {
            unsafe
            {
                Unsafe.Write((void*) bufferPtr, buffer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T ArrayElementAsRef<T>(IntPtr data, int threadIndex) where T : struct
        {
            unsafe
            {
                return ref Unsafe.AsRef<T>(Unsafe.Add<T>((void*) data, threadIndex));
            }
        }

        public static int GetFieldOffset(FieldInfo field)
        {
#if UNITY_COLLECTIONS
            return Unity.Collections.LowLevel.Unsafe.UnsafeUtility.GetFieldOffset(field);
#else
            int GetFieldOffset(RuntimeFieldHandle h)
            {
                return Marshal.ReadInt32(h.Value + (4 + IntPtr.Size)) & 0xFFFFFF;
            }

            return GetFieldOffset(field.FieldHandle);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Align4(uint input) { return (uint) (Math.Ceiling(input / 4.0) * 4); }

        static long SignedCapacity(uint newCapacity)
        {
#if DEBUG_MEMORY
            return newCapacity + 128;
#else
            return newCapacity;
#endif
        }

        static IntPtr SignedPointer(uint capacityWithoutSignature, IntPtr pointerToSign)
        {
            unsafe
            {
#if DEBUG_MEMORY
                var value = 0xDEADBEEF;
                for (var i = 0; i < 60; i += 4)
                {
                    Unsafe.Write((void*) pointerToSign, value); //4 bytes signature
                    pointerToSign += 4;
                }

                Unsafe.Write((void*) pointerToSign, capacityWithoutSignature); //4 bytes size allocated
                pointerToSign += 4;

                for (var i = 0; i < 64; i += 4)
                    Unsafe.Write((void*) (pointerToSign + (int) capacityWithoutSignature + i)
                               , value); //4 bytes size allocated

                return (IntPtr) (byte*) pointerToSign;
#else
                return (IntPtr) pointerToSign;
#endif
            }
        }

        static IntPtr UnsignPointer(IntPtr ptr)
        {
#if DEBUG_MEMORY
            return ptr - 64;
#else
            return ptr;
#endif
        }

        static IntPtr CheckAndReturnPointerToFree(IntPtr ptr)
        {
            ptr = UnsignPointer(ptr);

            CheckBoundaries(ptr);
            return ptr;
        }

        static unsafe void CheckBoundaries(IntPtr ptr)
        {
#if DEBUG_MEMORY
            var debugPtr = ptr;

            for (var i = 0; i < 60; i += 4)
            {
                var u = Unsafe.Read<uint>((void*) debugPtr);
                if (u != 0xDEADBEEF)
                    throw new Exception("Memory Boundaries check failed!!!");

                debugPtr += 4;
            }

            var size = Unsafe.Read<uint>((void*) debugPtr);
            debugPtr = debugPtr + (int) (4 + size);

            for (var i = 0; i < 64; i += 4)
            {
                var u = Unsafe.Read<uint>((void*) (debugPtr + i));
                if (u != 0xDEADBEEF)
                    throw new Exception("Memory Boundaries check failed!!!");
            }
#endif
        }
    }
}