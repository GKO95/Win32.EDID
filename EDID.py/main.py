from ctypes import *
from SetupAPI import *

def main():

	#==============================================================
	# EXTRACTING MONITOR EDID
	#==============================================================
	msgCaption = "EDID.py"

	dwSize = DWORD()	# GUID container size.
	ptrGUID = GUID()	# GUID container.

	'''
		IF THE FUNCTION "SetupDiClassGuidsFromNameW()" is passed buffer of GUID smaller
		than the very last argument "&dwSize", the function returns FALSE while assigning
		dwSize a required buffer size to store GUIDs of the specified classes.

		REFERENCE: https://docs.microsoft.com/en-us/windows/win32/api/setupapi/nf-setupapi-setupdiclassguidsfromnamew
	'''
	bResult = SetupAPI.SetupDiClassGuidsFromNameW("Monitor", None, 0, pointer(dwSize))
	if (bResult is False):
		if GetLastError() == ERROR_INSUFFICIENT_BUFFER:
			'''
				IN SECOND ATTEMPT, now that the required buffer size is known,
				array of GUID data type is created with the length of dwSize.
				The GUID array "ptrGUID" will be assigned with GUID of the
				"Monitor" class which is {4d36e96e-e325-11ce-bfc1-08002be10318}.

				REFERENCE: https://docs.microsoft.com/en-us/windows-hardware/drivers/install/system-defined-device-setup-classes-available-to-vendors
			'''
			GUIDs = GUID * dwSize.value
			ptrGUID = GUIDs()
			bResult = SetupAPI.SetupDiClassGuidsFromNameW("Monitor", pointer(ptrGUID[0]), dwSize, pointer(dwSize))
			if (bResult is False):
				#IF somehow failed to retrieve the GUID, alert a message box.'''
				MessageBoxW(0, "Unable to retrieve GUID of the specified class.", msgCaption, MB_ICONERROR)
				return 1
		else:
			# HOWEVER, in case the "SetupDiClassGuidsFromNameW()" failed to return
			# required buffer size due to a certain matter, alert a message box.
			MessageBoxW(0, "Unable to retrieve GUID of the specified class.", msgCaption, MB_ICONERROR)
			return 1

	'''
		GET ALL THE device information of the "Monitor" GUID that are currently presented
		within the system in HDEVINFO data type. This data type is merely a void pointer
		of the collection of device information set, thus is categorized as "Handle".

		FLAG: DIGCF_PRESENT - Return only devices that are currently present in a system.
	'''
	devINFO = HANDLE(-1)
	devINFO = SetupAPI.SetupDiGetClassDevsW(pointer(ptrGUID[0]), None, None, 2)
	if (devINFO == HANDLE(-1).value):
		MessageBoxW(0, "Failed to retrieve device information of the GUID class.", msgCaption, MB_ICONERROR)
		return 1

	'''
		SP_DEVINFO_DATA is a structure for a single device instance of the HDEVINFO device information set.
		Here, variable "devDATA" is used to temporary store a single device information from "devINFO" data.

		SP_DEVINFO_DATA.cbSize field must be specified as function that accpets SP_DEVINFO_DATA
		always check whether the byte size of the SP_DEVINFO_DATA structure is the same as the cbSize field.

		REFERENCE: https://docs.microsoft.com/en-us/windows/win32/api/setupapi/ns-setupapi-sp_devinfo_data
	'''
	devDATA = SP_DEVINFO_DATA()
	devDATA.cbSize = sizeof(SP_DEVINFO_DATA)

	devFOUND = True
	index = 0
	while(devFOUND is True):

		'''
			THE FUNCTION "SetupDiEnumDeviceInfo()" passes single "index"th device instance to devDATA
			from device information set devINFO. When no device instance is found on "index"th element,
			the function return FALSE.

			REFERENCE: https://docs.microsoft.com/en-us/windows/win32/api/setupapi/nf-setupapi-setupdienumdeviceinfo
		'''
		devFOUND = SetupAPI.SetupDiEnumDeviceInfo(devINFO, index, pointer(devDATA))
		if devFOUND:
			if index != 0: print()
			'''
				THE FUNCTION "SetupDiOpenDevRegKey()" returns handle for the registry key (HKEY) of the
				devDATA included in the device information set devINFO. Device has two registry key:
				hardware (DIREG_DEV) and software (DIREG_DRV) key. EDID can be found in hardware key.

				* DEV: \REGISTRY\MACHINE\SYSTEM\ControlSet001\Enum\DISPLAY\??????\*&********&*&UID****\Device Parameters
					   \REGISTRY\MACHINE\SYSTEM\ControlSet001\Enum\DISPLAY\??????\*&********&*&UID****\Device Parameters

				* DRV: \REGISTRY\MACHINE\SYSTEM\ControlSet001\Control\Class\{????????-****-????-****-????????????}\0001
					   \REGISTRY\MACHINE\SYSTEM\ControlSet001\Control\Class\{????????-****-????-****-????????????}\0000
			'''
			devKEY = HANDLE(-1)
			devKEY = SetupAPI.SetupDiOpenDevRegKey(devINFO, pointer(devDATA), DWORD(DICS_FLAG_GLOBAL), DWORD(0), DWORD(DIREG_DEV), DWORD(KEY_READ))
			print("Registry Key: \"{0}\"".format(GetHKEY(devKEY)))
			
			
			index += 1


if __name__ == "__main__":
    main()