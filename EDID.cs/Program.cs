using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using DWORD = System.UInt32;
using HDEVINFO = System.IntPtr;
using HWND = System.IntPtr;

namespace EDID.cs
{
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
                if (Marshal.GetLastWin32Error() == 123)
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





        }
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
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern Boolean SetupDiClassGuidsFromNameW(string ClassName, ref Guid ClassGuidList, DWORD ClassGuidListSize, ref DWORD RequiredSize);

        /*
            If the function returns the pointer, C# has IntPtr that can handle a pointer:
            There are IntPtr and UIntPtr, where each represetns signed and unsigned pointer, but recommends using IntPtr because
            it is CLS-compliant.

            * GUID*                                                         -> ref Guid
            * PCWSTR (= Pointer of Constant Wide-character, thus STRing)    -> string
            * HWND   (= Handle to a WiNDow; thus pointer)                   -> IntPtr
            * DWORD                                                         -> UInt32
        */
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode)]
        public static extern HDEVINFO SetupDiGetClassDevsW(ref Guid ClassGuid, string Enumerator, HWND hwndParent, DWORD Flags);


        //[DllImport("setupapi.dll")]
        //public static extern Boolean SetupDiEnumDeviceInfo(HDEVINFO DeviceInfoSet, DWORD MemberIndex, );
    }
}
