using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NETInterceptor
{
    /*public unsafe class MethodDesc
    {
        private readonly byte* _ptr;

        private readonly ushort m_wFlags3AndTokenRemainder;
        private readonly byte m_chunkIndex;
        private readonly byte m_bFlags2;
        private readonly short m_wSlotNumber;
        private readonly short m_wFlags;

        private static readonly int ALIGNMENT = 1 << (Utils.CurrentArchitecture == Architecture.X64 ? 3 : 2);
        private static readonly int MethodDescChunkSize = IntPtr.Size * 2 + 1 + 1 + 2;

        // TODO: other MethodDesc types
        private static readonly int[] s_ClassificationSizeTable = new int[] {
            8, // MethodDesc
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            IntPtr.Size, // NonVtableSlot
            0,
            0,
        };

        [Flags]
        private enum Flag2 : byte
        {
            // enum_flag2_HasPrecode implies that enum_flag2_HasStableEntryPoint is set.
            enum_flag2_HasStableEntryPoint = 0x01,   // The method entrypoint is stable (either precode or actual code)
            enum_flag2_HasPrecode = 0x02,   // Precode has been allocated for this method

            enum_flag2_IsUnboxingStub = 0x04,
            enum_flag2_HasNativeCodeSlot = 0x08,   // Has slot for native code
        }

        public MethodDesc(IntPtr pMethodDesc)
        {
            _ptr = (byte*)pMethodDesc.ToPointer();

            m_wFlags3AndTokenRemainder = *(ushort*)(_ptr);
            m_chunkIndex = *(_ptr + 2);
            m_bFlags2 = *(_ptr + 3);
            m_wSlotNumber = *(short*)(_ptr + 4);
            m_wFlags = *(short*)(_ptr + 6);
        }

        private enum MethodDescClassification : short
        {
            // Method is IL, FCall etc., see MethodClassification above.
            mdcClassification = 0x0007,
            mdcClassificationCount = mdcClassification + 1,

            // Note that layout of code:MethodDesc::s_ClassificationSizeTable depends on the exact values 
            // of mdcHasNonVtableSlot and mdcMethodImpl

            // Has local slot (vs. has real slot in MethodTable)
            mdcHasNonVtableSlot = 0x0008,

            // Method is a body for a method impl (MI_MethodDesc, MI_NDirectMethodDesc, etc)
            // where the function explicitly implements IInterface.foo() instead of foo().
            mdcMethodImpl = 0x0010,

            // Method is static
            mdcStatic = 0x0020,

            // unused                           = 0x0040,
            // unused                           = 0x0080,
            // unused                           = 0x0100,
            // unused                           = 0x0200,

            // Duplicate method. When a method needs to be placed in multiple slots in the
            // method table, because it could not be packed into one slot. For eg, a method
            // providing implementation for two interfaces, MethodImpl, etc
            mdcDuplicate = 0x0400,

            // Has this method been verified?
            mdcVerifiedState = 0x0800,

            // Is the method verifiable? It needs to be verified first to determine this
            mdcVerifiable = 0x1000,

            // Is this method ineligible for inlining?
            mdcNotInline = 0x2000,

            // Is the method synchronized
            mdcSynchronized = 0x4000,

            // Does the method's slot number require all 16 bits
            mdcRequiresFullSlotNumber = unchecked((short)0x8000)
        };

        private enum SlotMask : short
        {
            enum_packedSlotLayout_SlotMask = 0x03FF,
            enum_packedSlotLayout_NameHashMask = unchecked((short)0xFC00)
        }

        public byte MethodDescIndex
        {
            get { return m_chunkIndex; }
        }

        public bool HasNativeCodeSlot
        {
            get { return (m_bFlags2 & (byte)Flag2.enum_flag2_HasNativeCodeSlot) != 0; }
        }

        public bool HasStableEntryPoint
        {
            get { return (m_bFlags2 & (byte)Flag2.enum_flag2_HasStableEntryPoint) != 0; }
        }

        public bool HasPrecode
        {
            get { return (m_bFlags2 & (byte)Flag2.enum_flag2_HasPrecode) != 0; }
        }

        public bool RequiresFullSlotNumber
        {
            get { return (m_wFlags & (short)MethodDescClassification.mdcRequiresFullSlotNumber) != 0; }
        }

        public bool HasNonVtableSlot
        {
            get { return (m_wFlags & (short)MethodDescClassification.mdcHasNonVtableSlot) != 0; }
        }

        public short Slot
        {
            get
            {
                return RequiresFullSlotNumber
                    ? m_wSlotNumber
                    : (short)(m_wSlotNumber & (short)SlotMask.enum_packedSlotLayout_SlotMask);
            }
        }

        public IntPtr MethodDescChunk
        {
            get { return new IntPtr(_ptr - MethodDescChunkSize - MethodDescIndex * ALIGNMENT); }
        }

        public IntPtr AddrOfNativeCodeSlot
        {
            get { return new IntPtr(_ptr + s_ClassificationSizeTable[m_wFlags & (short)(MethodDescClassification.mdcClassification | MethodDescClassification.mdcHasNonVtableSlot | MethodDescClassification.mdcMethodImpl)]); }
        }

        public IntPtr GetNativeCode()
        {
            if (HasNativeCodeSlot) {
                var ptr = (byte*)AddrOfNativeCodeSlot.ToPointer();
                if (Utils.CurrentArchitecture == Architecture.X86) {
                    var offset = *(int*)ptr;
                    return new IntPtr((int)(ptr + offset) & ~1);
                }
                if (Utils.CurrentArchitecture == Architecture.X64) {
                    var offset = *(long*)ptr;
                    return new IntPtr((long)(ptr + offset) & ~1L);
                }
                throw new NotSupportedException();
            }

            if (!HasStableEntryPoint || HasPrecode)
                return IntPtr.Zero;

            return GetStableEntryPoint();
        }

        private IntPtr GetStableEntryPoint()
        {
            throw new NotImplementedException();
        }
    }*/
}
