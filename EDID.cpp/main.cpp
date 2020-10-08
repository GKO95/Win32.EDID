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

				REFERENCE: https://github.com/GKO95/MFC.CommonRegistry
			*/
			BYTE byteBUFFER[128] = { 0 };
			DWORD regSize = 128;
			DWORD regType = REG_BINARY;
			LRESULT lResult = RegQueryValueExW(devKEY, L"EDID", NULL, &regType, byteBUFFER, &regSize);
			if (lResult != ERROR_SUCCESS) {
				std::cout << "ERROR!" << std::endl;
			}
			else
			{
				for (BYTE i = 0; i < 128; i++)
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
