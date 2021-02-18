using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using DWORD = System.UInt32;
using HDEVINFO = System.IntPtr;
using HKEY = System.IntPtr;
using LRESULT = System.Int64;

namespace EDID.Framework.cs
{
	class Program
	{
		static void Main(string[] args)
		{
			//==============================================================
			// EXTRACTING MONITOR EDID
			//==============================================================
			const string msgCaption = "EDID.Framework.cs";

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
				SP_DEVINFO_DATA has been initialized for call by reference with "ref" keyword, instead of "out" keyword.

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
					Console.WriteLine("Registry Key: \"{0}\"", GetHKEY(devKEY));

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

		private static string GetHKEY(HKEY key)
		{
			string keyPath =  null;
			char[] buff = new char[1];
			
			if (key != null)
			{
				DWORD size = 0;
				DWORD result;

				/*
					SUPPLYING THE FUNCTION "KEY_NAME_INFORMATION" (enumerated as integer 3) structure of the
					registry key. However, since the buffer size is smaller, required size of the buffer will
					be assigned to "size" variable, while returning STATUS_BUFFER_TOO_SMALL.
				*/
				result = SetupAPI.NtQueryKey(key, 3, ref buff[0], 0 , ref size);
				if (result == ((DWORD)0xC0000023L))
				{
					// Additional 2-byte for extra space when trimming first two insignificant bytes.
					size += 2;
					buff = new char[size];
					result = SetupAPI.NtQueryKey(key, 3, ref buff[0], size, ref size);
					if (result == ((DWORD)0x00000000L))
					{
						keyPath = new string(buff);
						keyPath = keyPath.Substring(2);
						keyPath = keyPath.TrimEnd('\0');
					}
				}
			}
			return keyPath;
		}

		private static readonly IntPtr INVALID_HANDLE_VALUE = (IntPtr)(-1);
	}
}
