using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EDID.cs
{
    using DWORD = System.UInt32;
    using HDEVINFO = System.IntPtr;
    using HWND = System.IntPtr;
    using HKEY = System.IntPtr;
    using LRESULT = System.Int64;

    class Program
    {
        static void Main(string[] args)
        {

            //==============================================================
            // EXTRACTING MONITOR EDID
            //==============================================================
            const string msgCaption = "EDID.cs";

            /*
                IF THE FUNCTION "SetupDiClassGuidsFromNameW()" is passed buffer of GUID smaller
                than the very last argument "ref dwSize", the function returns FALSE while assigning
                dwSize a required buffer size to store GUIDs of the specified classes.

                However, C# can only pass variable as ref, and cannot convert null to Guid.
                Therefore, in C# takes different approach by allowing ptrGUID to have elligible size,
                but setting `GuidClassListSize` parameter as 0. The function will think buffer doesn't
                have any size at all, resulting FAILED operation.

                REFERENCE: https://docs.microsoft.com/en-us/windows/win32/api/setupapi/nf-setupapi-setupdiclassguidsfromnamew
            */
            DWORD dwSize = 1;
            Guid[] ptrGUID = new Guid[1];
            bool bResult = SetupAPI.SetupDiClassGuidsFromNameW("Monitor", ref ptrGUID[0], 0, ref dwSize);
            if (bResult == false)
            {
                if (Marshal.GetLastWin32Error() == (int)ERROR.INSUFFICIENT_BUFFER)
                {
                    /*
                        IN SECOND ATTEMPT, now that the required buffer size is known,
                        array of GUID data type is created with the length of dwSize.
                        The GUID array "ptrGUID" will be assigned with GUID of the
                        "Monitor" class which is {4d36e96e-e325-11ce-bfc1-08002be10318}.

                        REFERENCE: https://docs.microsoft.com/en-us/windows-hardware/drivers/install/system-defined-device-setup-classes-available-to-vendors
                    */
                    ptrGUID = new Guid[dwSize];
                    bResult = SetupAPI.SetupDiClassGuidsFromNameW("Monitor", ref ptrGUID[0], dwSize, ref dwSize);
                    if (!bResult)
                    {
                        MessageBox.Show("Unable to retrieve GUID of the specified class.", msgCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                else
                {
                    /*
                        HOWEVER, in case the "SetupDiClassGuidsFromNameW()" failed to return
                        required buffer size due to a certain matter, alert a message box.
                    */
                    MessageBox.Show("Unable to retrieve GUID of the specified class.", msgCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            /*
                GET ALL THE device information of the "Monitor" GUID that are currently presented
                within the system in HDEVINFO data type. This data type is merely a void pointer
                of the collection of device information set, thus is categorized as "Handle".

                FLAG: DIGCF_PRESENT - Return only devices that are currently present in a system.
            */
            HDEVINFO devINFO = INVALID_HANDLE_VALUE;
            devINFO = SetupAPI.SetupDiGetClassDevsW(ref ptrGUID[0], null, IntPtr.Zero, (DWORD)DIGCF.PRESENT);
            if (devINFO == INVALID_HANDLE_VALUE)
            {
                MessageBox.Show("Failed to retrieve device information of the GUID class.", msgCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            /*
                SP_DEVINFO_DATA is a structure for a single device instance of the HDEVINFO device information set.
                Here, variable "devDATA" is used to temporary store a single device information from "devINFO" data.

                SP_DEVINFO_DATA.cbSize field must be specified as function that accpets SP_DEVINFO_DATA
                always check whether the byte size of the SP_DEVINFO_DATA structure is the same as the cbSize field.

                REFERENCE: https://docs.microsoft.com/en-us/windows/win32/api/setupapi/ns-setupapi-sp_devinfo_data

                SP_DEVINFO_DATA in C# is custom-created structure, thus does not have predefined size.
                Unsafe context is needed to acquire the size of the SP_DEVINFO_DATA, which require "Allow unsafe code" enabled in C# Build configureation.

                REFERENCE: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/unsafe
            */
            SP_DEVINFO_DATA devDATA;
            unsafe { 
                devDATA = new SP_DEVINFO_DATA { cbSize = (DWORD)sizeof(SP_DEVINFO_DATA), ClassGuid = Guid.Empty, DevInst = 0, Reserved = IntPtr.Zero };
            }


            bool devFOUND = true;
            for (DWORD index = 0; devFOUND; index++)
            {
                /*
                    THE FUNCTION "SetupDiEnumDeviceInfo()" passes single "index"th device instance to devDATA
                    from device information set devINFO. When no device instance is found on "index"th element,
                    the function return FALSE.

                    REFERENCE: https://docs.microsoft.com/en-us/windows/win32/api/setupapi/nf-setupapi-setupdienumdeviceinfo
                */
                devFOUND = SetupAPI.SetupDiEnumDeviceInfo(devINFO, index, ref devDATA);
                if (devFOUND)
                {
                    if (index != 0) Console.WriteLine();
                    /*
                        THE FUNCTION "SetupDiOpenDevRegKey()" returns handle for the registry key (HKEY) of the
                        devDATA included in the device information set devINFO. Device has two registry key:
                        hardware (DIREG_DEV) and software (DIREG_DRV) key. EDID can be found in hardware key.

                        * DEV: \REGISTRY\MACHINE\SYSTEM\ControlSet001\Enum\DISPLAY\??????\*&********&*&UID****\Device Parameters
                               \REGISTRY\MACHINE\SYSTEM\ControlSet001\Enum\DISPLAY\??????\*&********&*&UID****\Device Parameters

                        * DRV: \REGISTRY\MACHINE\SYSTEM\ControlSet001\Control\Class\{????????-****-????-****-????????????}\0001
                               \REGISTRY\MACHINE\SYSTEM\ControlSet001\Control\Class\{????????-****-????-****-????????????}\0000
                    */
                    HKEY devKEY = SetupAPI.SetupDiOpenDevRegKey(devINFO, ref devDATA, (DWORD)DICS_FLAG.GLOBAL, 0, (DWORD)DIREG.DEV, (DWORD)KEY.READ);

                    /*
                        OPENING and querying registry key has already been dealt in other repository.
                        Refer to GKO95/MFC.CommonRegistry repository for more information.

                        REFERENCE: https://github.com/GKO95/MFC.CommonRegistry
                    */
                    byte[] byteBuffer = new byte[128];
                    DWORD regSize = 128;
                    DWORD regType = (DWORD)REG.BINARY;
                    LRESULT lResult = SetupAPI.RegQueryValueExW(devKEY, "EDID", 0, ref regType, ref byteBuffer[0], ref regSize);
                    if (lResult != (int)ERROR.SUCCESS)
                    {
                        Console.WriteLine("ERROR!");
                    }
                    else
                    {
                        string hexBuffer = BitConverter.ToString(byteBuffer).Replace("-", " ").ToLower();
                        Console.Write(hexBuffer + "\n");
                    }
                }
            }
        }

        private static readonly IntPtr INVALID_HANDLE_VALUE = (IntPtr)(-1);
    }

    static class SetupAPI
    {
        /*
            When importing .DLL to C# project, use ref keyword for pointer parameter.
           
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
        */
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "SetupDiClassGuidsFromNameW")]
        public static extern bool SetupDiClassGuidsFromNameW(string ClassName, ref Guid ClassGuidList, DWORD ClassGuidListSize, ref DWORD RequiredSize);

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
        public static extern HDEVINFO SetupDiGetClassDevsW(ref Guid ClassGuid, string Enumerator, HWND hwndParent, DWORD Flags);

        /*
            * HDEVINFO          -> IntPtr
            * DWORD             -> UInt32
            * PSP_DEVINFO_DATA  -> ref SP_DEVINFO_DATA
        */
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, EntryPoint = "SetupDiEnumDeviceInfo")]
        public static extern bool SetupDiEnumDeviceInfo(HDEVINFO DeviceInfoSet, DWORD MemberIndex, ref SP_DEVINFO_DATA DeviceInfoData);

        /*
            * HDEVINFO          -> IntPtr
            * PSP_DEVINFO_DATA  -> ref SP_DEVINFO_DATA
            * DWORD             -> UInt32
            * DWORD             -> UInt32
            * DWORD             -> UInt32
            * REGSAM            -> UInt32
        */
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, EntryPoint = "SetupDiOpenDevRegKey")]
        public static extern HKEY SetupDiOpenDevRegKey(HDEVINFO DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData, DWORD Scope, DWORD HwProfile, DWORD KeyType, DWORD samDesired);

        /*
            * HKEY              -> IntPtr
            * LPCWSTR           -> string
            * LPDWORD           -> UInt32
            * LPDWORD           -> UInt32
            * LPBYTE            -> UInt32
            * LPDWORD           -> UInt32
            However, lpReserved must be set to NULL, thus "ref" keyword is not used.
        */
        [DllImport("Advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegQueryValueExW")]
        public static extern LRESULT RegQueryValueExW(HKEY hKey, string lpValueName, DWORD lpReserved, ref DWORD lpType, ref byte lpData, ref DWORD lpcbData);

    }

    /*
        Lets you control the physical layout of the data fields of a class or structure in memory.
        LayoutKind
        * Auto: The runtime automatically chooses an appropriate layout for the members of an object in unmanaged memory.
        * Explicit: The precise position of each member of an object in unmanaged memory is explicitly controlled, subject to the setting of the Pack field.
                    Each member must use the FieldOffsetAttribute "[FieldOffset]" to indicate the position of that field within the type.
        * Sequential: The members of the object are laid out sequentially, in the order in which they appear when exported to unmanaged memory.
    */
    [StructLayout(LayoutKind.Sequential)]
    public struct SP_DEVINFO_DATA
    {
        public DWORD cbSize;
        public Guid ClassGuid;
        public DWORD DevInst;
        public IntPtr Reserved;
    }
    //[StructLayout(LayoutKind.Explicit, Size = 32, CharSet = CharSet.Unicode)]
    //public struct SP_DEVINFO_DATA
    //{
    //    [FieldOffset(0)]  public DWORD cbSize;
    //    [FieldOffset(4)]  public Guid  ClassGuid;
    //    [FieldOffset(20)] public DWORD DevInst;
    //    [FieldOffset(24)] public ulong Reserved;
    //}

    [Flags]
    public enum ERROR : int
    {
        SUCCESS             = 0x0,
        INVALID_HANDLE      = 0x6,
        INSUFFICIENT_BUFFER = 0x7A,
    }

    [Flags]
    public enum DIGCF : DWORD
    {
        DEFAULT = 0x00000001,
        PRESENT = 0x00000002,
        ALLCLASSES = 0x00000004,
        PROFILE = 0x00000008,
        DEVICEINTERFACE = 0x00000010
    }

    [Flags]
    public enum DICS_FLAG : DWORD
    { 
        GLOBAL = 0x00000001,
        CONFIGSPECIFIC = 0x00000002,
        CONFIGGENERAL = 0x00000004
    }

    [Flags]
    public enum DIREG : DWORD
    { 
        DEV = 0x00000001,
        DRV = 0x00000002,
        BOTH = 0x00000004
    }

    [Flags]
    public enum KEY : long
    { 
        QUERY_VALUE         = 0x0001,
        SET_VALUE           = 0x0002,
        CREATE_SUB_KEY      = 0x0004,
        ENUMERATE_SUB_KEYS  = 0x0008,
        NOTIFY              = 0x0010,
        CREATE_LINK         = 0x0020,
        WOW64_32KEY         = 0x0200,
        WOW64_64KEY         = 0x0100,
        WOW64_RES           = 0x0300,
        READ                = ((0x00020000L | QUERY_VALUE | ENUMERATE_SUB_KEYS | NOTIFY) & (~0x00100000L)),
        WRITE               = ((0x00020000L | SET_VALUE | CREATE_SUB_KEY) & (~0x00100000L)),
        EXECUTE             = ((READ) & (~0x00100000L)),
        ALL_ACCESS          = ((0x001F0000L | QUERY_VALUE | SET_VALUE | CREATE_SUB_KEY | ENUMERATE_SUB_KEYS | NOTIFY | CREATE_LINK) & (~0x00100000L))
    }

    [Flags]
    public enum REG : ulong
    { 
        NONE                        = 0ul,
        SZ                          = 1ul,
        EXPAND_SZ                   = 2ul,
        BINARY                      = 3ul,
        DWORD                       = 4ul,
        DWORD_LITTLE_ENDIAN         = 4ul,
        DWORD_BIG_ENDIAN            = 5ul,
        LINK                        = 6ul,
        MULTI_SZ                    = 7ul,
        RESOURCE_LIST               = 8ul,
        FULL_RESOURCE_DESCRIPTOR    = 9ul,
        RESOURCE_REQUIREMENTS_LIST  = 10ul,
        QWORD                       = 11ul,
        QWORD_LITTLE_ENDIAN         = 11ul
    }
}
