#include <iostream>

/* Essential headers for "SetupAPI.lib"; do not forget to include library to the project! */
#include <Windows.h>
#include <SetupApi.h>

/* Used for displaying hexadecimal: includes "std::setfill()" and "std::setw()" functions */
#include <iomanip>

/*
	Function prototype for returing registry key path of the GUID device information.
	REFERENCE: https://stackoverflow.com/questions/937044/determine-path-to-registry-key-from-hkey-handle-in-c
*/
std::wstring GetHKEY(HKEY key);

int main(int argc, char** argv) {

	//==============================================================
	// EXTRACTING MONITOR EDID
	//==============================================================
	const LPCWSTR msgCaption = L"EDID.cpp";

	DWORD dwSize;	// GUID container size.
	GUID* ptrGUID = NULL;	// pointer to the GUID container.

	/*
		IF THE FUNCTION "SetupDiClassGuidsFromNameW()" is passed buffer of GUID smaller
		than the very last argument "&dwSize", the function returns FALSE while assigning
		dwSize a required buffer size to store GUIDs of the specified classes.

		REFERENCE: https://docs.microsoft.com/en-us/windows/win32/api/setupapi/nf-setupapi-setupdiclassguidsfromnamew
	*/
	BOOL bResult = SetupDiClassGuidsFromNameW(TEXT("Monitor"), NULL, NULL, &dwSize);
	if (bResult == FALSE)
	{
		if (GetLastError() == ERROR_INSUFFICIENT_BUFFER)
		{
			/*
				IN SECOND ATTEMPT, now that the required buffer size is known,
				array of GUID data type is created with the length of dwSize.
				The GUID array "ptrGUID" will be assigned with GUID of the
				"Monitor" class which is {4d36e96e-e325-11ce-bfc1-08002be10318}.

				REFERENCE: https://docs.microsoft.com/en-us/windows-hardware/drivers/install/system-defined-device-setup-classes-available-to-vendors
			*/
			ptrGUID = new GUID[dwSize];
			bResult = SetupDiClassGuidsFromNameW(TEXT("Monitor"), ptrGUID, dwSize, &dwSize);
			if (!bResult)
			{
				// IF somehow failed to retrieve the GUID, alert a message box.
				MessageBoxW(NULL, L"Unable to retrieve GUID of the specified class.", msgCaption, MB_ICONERROR);
				return 1;
			}
		}
		else
		{
			/*
				HOWEVER, in case the "SetupDiClassGuidsFromNameW()" failed to return
				required buffer size due to a certain matter, alert a message box.
			*/
			MessageBoxW(NULL, L"Unable to retrieve GUID of the specified class.", msgCaption, MB_ICONERROR);
			return 1;
		}
	}

	/*
		GET ALL THE device information of the "Monitor" GUID that are currently presented
		within the system in HDEVINFO data type. This data type is merely a void pointer
		of the collection of device information set, thus is categorized as "Handle".

		FLAG: DIGCF_PRESENT - Return only devices that are currently present in a system.
	*/
	HDEVINFO devINFO = INVALID_HANDLE_VALUE;
	devINFO = SetupDiGetClassDevsW(ptrGUID, NULL, NULL, DIGCF_PRESENT);
	if (devINFO == INVALID_HANDLE_VALUE)
	{
		MessageBoxW(NULL, L"Failed to retrieve device information of the GUID class.", msgCaption, MB_ICONERROR);
		return 1;
	}

	/*
		SP_DEVINFO_DATA is a structure for a single device instance of the HDEVINFO device information set.
		Here, variable "devDATA" is used to temporary store a single device information from "devINFO" data.

		SP_DEVINFO_DATA.cbSize field must be specified as function that accpets SP_DEVINFO_DATA
		always check whether the byte size of the SP_DEVINFO_DATA structure is the same as the cbSize field.

		REFERENCE: https://docs.microsoft.com/en-us/windows/win32/api/setupapi/ns-setupapi-sp_devinfo_data
	*/
	SP_DEVINFO_DATA devDATA;
	devDATA.cbSize = sizeof(SP_DEVINFO_DATA);

	BOOL devFOUND = TRUE;
	for (DWORD index = 0; devFOUND; index++)
	{
		/*
			THE FUNCTION "SetupDiEnumDeviceInfo()" passes single "index"th device instance to devDATA
			from device information set devINFO. When no device instance is found on "index"th element,
			the function return FALSE.

			REFERENCE: https://docs.microsoft.com/en-us/windows/win32/api/setupapi/nf-setupapi-setupdienumdeviceinfo
		*/
		devFOUND = SetupDiEnumDeviceInfo(devINFO, index, &devDATA);
		if (devFOUND)
		{
			if (index != 0) std::cout << std::endl;

			/*
				THE FUNCTION "SetupDiOpenDevRegKey()" returns handle for the registry key (HKEY) of the
				devDATA included in the device information set devINFO. Device has two registry key:
				hardware (DIREG_DEV) and software (DIREG_DRV) key. EDID can be found in hardware key.

				* DEV: \REGISTRY\MACHINE\SYSTEM\ControlSet001\Enum\DISPLAY\??????\*&********&*&UID****\Device Parameters
					   \REGISTRY\MACHINE\SYSTEM\ControlSet001\Enum\DISPLAY\??????\*&********&*&UID****\Device Parameters

				* DRV: \REGISTRY\MACHINE\SYSTEM\ControlSet001\Control\Class\{????????-****-????-****-????????????}\0001
					   \REGISTRY\MACHINE\SYSTEM\ControlSet001\Control\Class\{????????-****-????-****-????????????}\0000
			*/
			HKEY devKEY = SetupDiOpenDevRegKey(devINFO, &devDATA, DICS_FLAG_GLOBAL, 0, DIREG_DEV, KEY_READ);

			wprintf_s(L"Registry Key: \"%s\"\n", GetHKEY(devKEY).c_str());

			/*
				OPENING and querying registry key has already been dealt in other repository.
				Refer to GKO95/MFC.CommonRegistry repository for more information.
			*/
			BYTE byteBUFFER[256] = { 0 };
			DWORD regSize = sizeof(byteBUFFER);
			DWORD regType = REG_BINARY;
			LRESULT lResult = RegQueryValueExW(devKEY, L"EDID", NULL, &regType, byteBUFFER, &regSize);
			if (lResult != ERROR_SUCCESS) {
				std::cout << "!ERROR: " << lResult << std::endl;
			}
			else
			{
				for (short i = 0; i < sizeof(byteBUFFER); i++)
					std::cout << std::hex << std::setfill('0') << std::setw(2) << int(byteBUFFER[i]) << " ";
				std::cout << std::endl;
			}
		}
	}

	// PREVENT memory leak and dangling pointer.
	delete[] ptrGUID;
	ptrGUID = nullptr;

	return 0;
}

std::wstring GetHKEY(HKEY key)
{
	std::wstring keyPath;
	if (key != NULL)
	{
		/*
			WINDOWS IS A family of OS developed by Microsoft; Windows NT is one of the active OS, which includes
			Windows 10, 8.1, 8, 7, Vista, XP, and 2000. And "ntdll.dll" module contains NT kernel functions.

			LoadLibraryW() function is responsible for loading modules such as DLL or EXE to the current process.
			The function can be deemed as counterpart of "modprobe" command in UNIX/Linux operating system.
		*/
		HMODULE dll = LoadLibraryW(L"ntdll.dll");
		if (dll != NULL) {
			/*
				THE FUNCTION NEEDED for acquiring the registry key path is called "NtQueryKey()", but no header
				is available unless installing Windows Driver Kit. Thus, this is an attempt to declare the function
				without a help from "wdm.h" header file, identifying its function structure as "NtQueryKeyType".

				__stdcall is a keyword to specify it is the function directly called from the Win32 API.
			*/
			typedef NTSTATUS(__stdcall* NtQueryKeyType)(
				HANDLE  KeyHandle,
				int KeyInformationClass,
				PVOID  KeyInformation,
				ULONG  Length,
				PULONG  ResultLength);

			/*
				ACQRUIRING THE FUNCTION "NtQueryKey()" from the ntdll.dll module ("ZwQueryKey()" in kernel-mode)
				using "GerProcAddress()" function. The acquired function is returned as a function pointer.
			*/
			NtQueryKeyType func = reinterpret_cast<NtQueryKeyType>(::GetProcAddress(dll, "NtQueryKey"));

			if (func != NULL) {
				/*
					SUPPLYING THE FUNCTION "KEY_NAME_INFORMATION" (enumerated as integer 3) structure of the
					registry key. However, since the buffer size is smaller, required size of the buffer will
					be assigned to "size" variable, while returning STATUS_BUFFER_TOO_SMALL.
				*/
				DWORD size = 0;
				DWORD result = 0;
				result = func(key, 3, 0, 0, &size);
				if (result == ((NTSTATUS)0xC0000023L))
				{
					// Additional 2-byte for extra space when trimming first two insignificant bytes.
					size = size + 2;
					wchar_t* buffer = new (std::nothrow) wchar_t[size / sizeof(wchar_t)];
					if (buffer != NULL)
					{
						result = func(key, 3, buffer, size, &size);
						if (result == ((NTSTATUS)0x00000000L))
						{
							buffer[size / sizeof(wchar_t)] = L'\0';
							keyPath = std::wstring(buffer + 2);
						}

						delete[] buffer;
					}
				}
			}

			FreeLibrary(dll);
		}
	}

	return keyPath;
}
