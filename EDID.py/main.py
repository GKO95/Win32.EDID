from ctypes import *
from SetupAPI import *

def main():

	#==============================================================
	# EXTRACTING MONITOR EDID
	#==============================================================
	msgCaption = "EDID.py"

	dwSize = c_ulong()	# GUID container size.

	'''
		IF THE FUNCTION "SetupDiClassGuidsFromNameW()" is passed buffer of GUID smaller
		than the very last argument "&dwSize", the function returns FALSE while assigning
		dwSize a required buffer size to store GUIDs of the specified classes.

		REFERENCE: https://docs.microsoft.com/en-us/windows/win32/api/setupapi/nf-setupapi-setupdiclassguidsfromnamew
	'''
	bResult = SetupAPI.SetupDiClassGuidsFromNameW("Monitor", None, 0, pointer(dwSize))
	if (bResult is False):
		if GetLastError() == 122:
			'''
				IN SECOND ATTEMPT, now that the required buffer size is known,
				array of GUID data type is created with the length of dwSize.
				The GUID array "ptrGUID" will be assigned with GUID of the
				"Monitor" class which is {4d36e96e-e325-11ce-bfc1-08002be10318}.

				REFERENCE: https://docs.microsoft.com/en-us/windows-hardware/drivers/install/system-defined-device-setup-classes-available-to-vendors
			'''
			c_guids = c_guid * dwSize.value
			ptrGUID = c_guids()
			bResult = SetupAPI.SetupDiClassGuidsFromNameW("Monitor", pointer(ptrGUID[0]), dwSize, pointer(dwSize))
			if (bResult is False):
				#IF somehow failed to retrieve the GUID, alert a message box.'''
				MessageBoxW(0, "Unable to retrieve GUID of the specified class.", msgCaption, MB_ICONERROR)
				return -1
		else:
			# HOWEVER, in case the "SetupDiClassGuidsFromNameW()" failed to return
			# required buffer size due to a certain matter, alert a message box.
			MessageBoxW(0, "Unable to retrieve GUID of the specified class.", msgCaption, MB_ICONERROR)
			return -1

if __name__ == "__main__":
    main()