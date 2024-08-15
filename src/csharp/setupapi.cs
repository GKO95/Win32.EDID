using System;
using System.Runtime.InteropServices;

using DWORD = System.UInt32;
using HDEVINFO = System.IntPtr;
using HWND = System.IntPtr;
using HKEY = System.IntPtr;
using LRESULT = System.Int64;

namespace EDID.Csharp
{
    static class SetupApi
    {
        /*
            DllImpotAttribute allows using function implemented in DLL on C# source code.
            When importing DLL to C# project, use ref keyword for pointer parameter.
               
            * PCWSTR (= Pointer of Constant Wide-character, thus STRing)    -> string
            * LPGUID (= Long Pointer of GUID)                               -> ref Guid
            * DWORD                                                         -> UInt32
            * PDWORD (= Pointer of DWORD)                                   -> ref UInt32
            
            Keyword "extern" is used to declare a method that is implemented externally.
            
            "CharSet = CharSet.Unicode"
            : Indicates how to marshal string parameters to the method and controls name mangling.
                Here, "marshaling" is the process of transforming types when they need to cross between managed and native code.
                Marshaling is needed because the types in the managed and unmanaged code are different. In managed code, for instance,
                you have a String, while in the unmanaged world strings can be Unicode ("wide"), non-Unicode, null-terminated, ASCII, etc.
                (ref: https://docs.microsoft.com/en-us/dotnet/standard/native-interop/type-marshaling)
            
                Charset has four enumerator; Ansi(default), Auto, None, and Unicode. Th external function being called
                is UTF-16 (i.e. 2-Byte Unicode) indicated from prefix "W", where "A" indicate Ansi (not ASCII).
              
            "SetLastError = true"
            : allows catching error number signaled from SetLastError function of "Kernel32.dll".
                To catch the errnum, use "Marhsal.GetLastWin32Error()" instead of importing GetLastError from DLL.
            
            "EntryPoint = 'SetupDiClassGuidsFromNameW'"
            : indicates the name or ordinal of the DLL entry point to be called.
                By default, EntryPoint is set to the function name specified on declaration.
        */
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "SetupDiClassGuidsFromNameW")]
        internal static extern bool SetupDiClassGuidsFromNameW(string ClassName, ref Guid ClassGuidList, DWORD ClassGuidListSize, ref DWORD RequiredSize);

        /*
            If the function returns the pointer, C# has IntPtr that can handle a pointer:
            There are IntPtr and UIntPtr, where each represetns signed and unsigned pointer, but recommends using IntPtr because
            it is CLS-compliant.
            
            * GUID*                                                         -> ref Guid
            * PCWSTR (= Pointer of Constant Wide-character, thus STRing)    -> string
            * HWND   (= Handle to a WiNDow; thus pointer)                   -> IntPtr
            * DWORD                                                         -> UInt32
        */
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, EntryPoint = "SetupDiGetClassDevsW")]
        internal static extern HDEVINFO SetupDiGetClassDevsW(ref Guid ClassGuid, string Enumerator, HWND hwndParent, DWORD Flags);

        /*
            * HDEVINFO          -> IntPtr
            * DWORD             -> UInt32
            * PSP_DEVINFO_DATA  -> ref SP_DEVINFO_DATA
        */
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, EntryPoint = "SetupDiEnumDeviceInfo")]
        internal static extern bool SetupDiEnumDeviceInfo(HDEVINFO DeviceInfoSet, DWORD MemberIndex, ref SP_DEVINFO_DATA DeviceInfoData);

        /*
            * HDEVINFO          -> IntPtr
            * PSP_DEVINFO_DATA  -> ref SP_DEVINFO_DATA
            * DWORD             -> UInt32
            * DWORD             -> UInt32
            * DWORD             -> UInt32
            * REGSAM            -> UInt32
        */
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, EntryPoint = "SetupDiOpenDevRegKey")]
        internal static extern HKEY SetupDiOpenDevRegKey(HDEVINFO DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData, DWORD Scope, DWORD HwProfile, DWORD KeyType, DWORD samDesired);

        /*
            Some functions have a parameter but must be left alone with 0 or null data.
            Parameter "lpReserved" is one of them, thus "ref" keyword is not used as constant argument cannot be passed.
            * HKEY              -> IntPtr
            * LPCWSTR           -> string
            * LPDWORD           -> UInt32
            * LPDWORD           -> ref UInt32
            * LPBYTE            -> ref UInt32
            * LPDWORD           -> ref UInt32
        */
        [DllImport("Advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegQueryValueExW")]
        internal static extern LRESULT RegQueryValueExW(HKEY hKey, string lpValueName, DWORD lpReserved, ref DWORD lpType, ref byte lpData, ref DWORD lpcbData);

        /*
            * HANDLE    -> IntPtr
            * int       -> Int32
            * PVOID     -> string
            * ULONG     -> UInt32
            * PULONG    -> ref UInt32
        */
        [DllImport("ntdll.dll", CharSet = CharSet.Unicode, EntryPoint = "NtQueryKey")]
        internal static extern DWORD NtQueryKey(HKEY KeyHandle, int KeyInformationClass, ref char KeyInformation, uint Length, ref uint ResultLength);
    }

    /*
        StructLayoutAttribute is used to control physical (i.e. memory) layout of classes and structs.
        Hence, this attribute can be considered as advanced data structure configurator.

        "LayoutKind"
        : controls the layout of an object when exported to unmanaged code. There are three LayoutKinds available.
            * Auto: The runtime automatically chooses an appropriate layout for the members of an object in unmanaged memory.
            * Explicit: The precise position of each member of an object in unmanaged memory is explicitly controlled.
                        Each member must use the FieldOffsetAttribute "[FieldOffset]" to indicate the position of that field within the type.
            * Sequential: The members of the object are laid out sequentially, in the order in which they appear when exported to unmanaged memory.
    */
    [StructLayout(LayoutKind.Sequential)]
    internal struct SP_DEVINFO_DATA
    {
        public DWORD cbSize;
        public Guid ClassGuid;
        public DWORD DevInst;
        public IntPtr Reserved;
    }
    /* ALTERNATIVE
    [StructLayout(LayoutKind.Explicit, Size = 32, CharSet = CharSet.Unicode)]
    public struct SP_DEVINFO_DATA
    {
        [FieldOffset(0)] public DWORD cbSize;
        [FieldOffset(4)] public Guid ClassGuid;
        [FieldOffset(20)] public DWORD DevInst;
        [FieldOffset(24)] public ulong Reserved;
    }
    */

    [Flags]
    internal enum ERROR : int
    {
	    SUCCESS = 0x0,
	    INVALID_HANDLE = 0x6,
	    INSUFFICIENT_BUFFER = 0x7A,
    }

    [Flags]
    internal enum DIGCF : DWORD
    {
        DEFAULT = 0x00000001,
        PRESENT = 0x00000002,
        ALLCLASSES = 0x00000004,
        PROFILE = 0x00000008,
        DEVICEINTERFACE = 0x00000010
    }

    [Flags]
    internal enum DICS_FLAG : DWORD
    {
        GLOBAL = 0x00000001,
        CONFIGSPECIFIC = 0x00000002,
        CONFIGGENERAL = 0x00000004
    }

    [Flags]
    internal enum DIREG : DWORD
    {
        DEV = 0x00000001,
        DRV = 0x00000002,
        BOTH = 0x00000004
    }

    [Flags]
    internal enum KEY : long
    {
        QUERY_VALUE = 0x0001,
        SET_VALUE = 0x0002,
        CREATE_SUB_KEY = 0x0004,
        ENUMERATE_SUB_KEYS = 0x0008,
        NOTIFY = 0x0010,
        CREATE_LINK = 0x0020,
        WOW64_32KEY = 0x0200,
        WOW64_64KEY = 0x0100,
        WOW64_RES = 0x0300,
        READ = ((0x00020000L | QUERY_VALUE | ENUMERATE_SUB_KEYS | NOTIFY) & (~0x00100000L)),
        WRITE = ((0x00020000L | SET_VALUE | CREATE_SUB_KEY) & (~0x00100000L)),
        EXECUTE = ((READ) & (~0x00100000L)),
        ALL_ACCESS = ((0x001F0000L | QUERY_VALUE | SET_VALUE | CREATE_SUB_KEY | ENUMERATE_SUB_KEYS | NOTIFY | CREATE_LINK) & (~0x00100000L))
    }

    [Flags]
    internal enum REG : ulong
    {
        NONE = 0ul,
        SZ = 1ul,
        EXPAND_SZ = 2ul,
        BINARY = 3ul,
        DWORD = 4ul,
        DWORD_LITTLE_ENDIAN = 4ul,
        DWORD_BIG_ENDIAN = 5ul,
        LINK = 6ul,
        MULTI_SZ = 7ul,
        RESOURCE_LIST = 8ul,
        FULL_RESOURCE_DESCRIPTOR = 9ul,
        RESOURCE_REQUIREMENTS_LIST = 10ul,
        QWORD = 11ul,
        QWORD_LITTLE_ENDIAN = 11ul
    }
}
