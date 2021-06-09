from ctypes import *
from ctypes.wintypes import *

class GUID(Structure):
	_fields_ = [
		("Data1", ULONG),
		("Data2", USHORT),
		("Data3", USHORT),
		("Data4", BYTE * 8)
	]

class SP_DEVINFO_DATA(Structure):
	_fields_ = [
		("cbSize", DWORD),
		("ClassGuid", GUID),
		("DevInst", DWORD),
		("Reserved", POINTER(ULONG))
	]

class SetupAPI:
	@staticmethod
	def SetupDiClassGuidsFromNameW(ClassName, ClassGuidList, ClassGuidListSize, RequiredSize):
		__function = windll.setupapi.SetupDiClassGuidsFromNameW
		__function.argtypes = [PWCHAR, POINTER(GUID), DWORD, PDWORD]
		__function.restype  = c_bool
		return __function(ClassName, ClassGuidList, ClassGuidListSize, RequiredSize)

	@staticmethod
	def SetupDiGetClassDevsW(ClassGuid, Enumerator, hwndParent, Flags):
		__function = windll.setupapi.SetupDiGetClassDevsW
		__function.argtypes = [POINTER(GUID), PWCHAR, HWND, DWORD]
		__function.restype  = HANDLE
		return __function(ClassGuid, Enumerator, hwndParent, Flags)
	
	@staticmethod
	def SetupDiEnumDeviceInfo(DeviceInfoSet, MemberIndex, DeviceInfoData):
		__function = windll.setupapi.SetupDiEnumDeviceInfo
		__function.argtypes = [HANDLE, DWORD, POINTER(SP_DEVINFO_DATA)]
		__function.restype  = c_bool
		return __function(DeviceInfoSet, MemberIndex, DeviceInfoData)

	@staticmethod
	def SetupDiOpenDevRegKey(DeviceInfoSet, DeviceInfoData, Scope, HwProfile, KeyType, samDesired):
		__function = windll.setupapi.SetupDiOpenDevRegKey
		__function.argtypes = [HANDLE, POINTER(SP_DEVINFO_DATA), DWORD, DWORD, DWORD, DWORD]
		__function.restype  = HKEY
		return __function(DeviceInfoSet, DeviceInfoData, Scope, HwProfile, KeyType, samDesired)

class Advapi32:
	@staticmethod
	def RegQueryValueExW(hKey, lpValueName, lpReserved, lpType, lpData, lpcbData):
		__function = windll.Advapi32.RegQueryValueExW
		__function.argtypes = [HKEY, LPCWSTR, LPDWORD, LPDWORD, LPVOID, LPDWORD]
		__function.restype  = LARGE_INTEGER
		return __function(hKey, lpValueName, lpReserved, lpType, lpData, lpcbData)

class Ntdll:
	@staticmethod
	def NtQueryKey(KeyHandle, KeyInformationClass, KeyInformation, Length, ResultLength):
		__function = windll.ntdll.NtQueryKey
		__function.argtypes = [HKEY, INT, LPVOID, ULONG, PULONG]
		__function.restype  = DWORD
		return __function(KeyHandle, KeyInformationClass, KeyInformation, Length, ResultLength)

def GetHKEY(key):
	keyPath = ""
	buff = CHAR()

	if key != None:
		size = DWORD()
		result = DWORD()
		'''
			SUPPLYING THE FUNCTION "KEY_NAME_INFORMATION" (enumerated as integer 3) structure of the
			registry key. However, since the buffer size is smaller, required size of the buffer will
			be assigned to "size" variable, while returning STATUS_BUFFER_TOO_SMALL.
		'''
		result = Ntdll.NtQueryKey(key, 3, None, 0, pointer(size))
		if result == 0xC0000023:
			buff = (WCHAR * size.value)()
			result = Ntdll.NtQueryKey(key, 3, pointer(buff), size, pointer(size))
			if result == 0x00000000:
				for i in range(2,size.value):
					keyPath += buff[i]
	return keyPath.rstrip("\0")

def MessageBoxW(hWnd, lpText, lpCaption, uType):
	windll.user32.MessageBoxW(hWnd, lpText, lpCaption, uType)

ERROR_SUCCESS				= 0x0
ERROR_INVALID_HANDLE		= 0x6
ERROR_INSUFFICIENT_BUFFER	= 0x7A

DIGCF_DEFAULT				= 0x00000000
DIGCF_PRESENT				= 0x00000002
DIGCF_ALLCLASSES			= 0x00000004
DIGCF_PROFILE				= 0x00000008
DIGCF_DEVICEINTERFACE		= 0x00000010

DICS_FLAG_GLOBAL 			= 0x00000001
DICS_FLAG_CONFIGSPECIFIC 	= 0x00000002
DICS_FLAG_CONFIGGENERAL 	= 0x00000004

DIREG_DEV					= 0x00000001
DIREG_DRV					= 0x00000002
DIREG_BOTH					= 0x00000004

KEY_QUERY_VALUE 			= 0x0001
KEY_SET_VALUE 				= 0x0002
KEY_CREATE_SUB_KEY 			= 0x0004
KEY_ENUMERATE_SUB_KEYS 		= 0x0008
KEY_NOTIFY 					= 0x0010
KEY_CREATE_LINK 			= 0x0020
KEY_WOW64_32KEY 			= 0x0200
KEY_WOW64_64KEY 			= 0x0100
KEY_WOW64_RES 				= 0x0300
KEY_READ 					= 0x20019 #((0x00020000 | KEY_QUERY_VALUE | KEY_ENUMERATE_SUB_KEYS | KEY_NOTIFY) & (0xffefffff) )
KEY_WRITE 					= 0x20006 #((0x00020000 | KEY_SET_VALUE | KEY_CREATE_SUB_KEY) & (0xffefffff) )
KEY_EXECUTE 				= 0x20019 #((KEY_READ) &  (0xffefffff) )
KEY_ALL_ACCESS 				= 0xF003F #((0x001F0000 | KEY_QUERY_VALUE | KEY_SET_VALUE | KEY_CREATE_SUB_KEY | KEY_ENUMERATE_SUB_KEYS | KEY_NOTIFY | KEY_CREATE_LINK) & (0xffefffff) )

MB_ICONERROR				= 0x00000010
MB_ICONQUESTION				= 0x00000020
MB_ICONWARNING				= 0x00000030

REG_NONE 						= ULONG(0)
REG_SZ 							= ULONG(1)
REG_EXPAND_SZ 					= ULONG(2)
REG_BINARY 						= ULONG(3)
REG_DWORD 						= ULONG(4)
REG_DWORD_LITTLE_ENDIAN 		= ULONG(4)
REG_DWORD_BIG_ENDIAN 			= ULONG(5)
REG_LINK 						= ULONG(6)
REG_MULTI_SZ 					= ULONG(7)
REG_RESOURCE_LIST 				= ULONG(8)
REG_FULL_RESOURCE_DESCRIPTOR 	= ULONG(9)
REG_RESOURCE_REQUIREMENTS_LIST 	= ULONG(10)
REG_QWORD 						= ULONG(11)
REG_QWORD_LITTLE_ENDIAN 		= ULONG(11)