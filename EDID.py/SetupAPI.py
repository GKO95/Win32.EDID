from ctypes import *
from ctypes.wintypes import BOOL
from ctypes.wintypes import DWORD
from ctypes.wintypes import HKEY
from ctypes.wintypes import BYTE

'''
	DWORD, REGSAM								-> c_ulong
	PCWSTR										-> c_wchar_p
	HANDLES (e.g. HDEVINFO, HWND, HKEY, etc)	-> c_void_p
'''

class GUID(Structure):
	_fields_ = [
		("Data1", c_ulong),
		("Data2", c_ushort),
		("Data3", c_ushort),
		("Data4", BYTE * 8)
	]

class c_SP_DEVINFO_DATA(Structure):
	_fields_ = [
		("cbSize", c_ulong),
		("ClassGuid", c_guid),
		("DevInst", c_ulong),
		("Reserved", POINTER(c_ulong))
	]

class SetupAPI:
	@staticmethod
	def SetupDiClassGuidsFromNameW(ClassName, ClassGuidList, ClassGuidListSize, RequiredSize):
		__function = windll.setupapi.SetupDiClassGuidsFromNameW
		__function.argtypes = [c_wchar_p, POINTER(GUID), DWORD, POINTER(DWORD)]
		__function.restype  = BOOL
		return __function(ClassName, ClassGuidList, ClassGuidListSize, RequiredSize)

	@staticmethod
	def SetupDiGetClassDevsW(ClassGuid, Enumerator, hwndParent, Flags):
		__function = windll.setupapi.SetupDiGetClassDevsW
		__function.argtypes = [POINTER(GUID), c_wchar_p, c_void_p, c_ulong]
		__function.restype  = c_void_p
		return __function(ClassGuid, Enumerator, hwndParent, Flags)
	
	@staticmethod
	def SetupDiEnumDeviceInfo(DeviceInfoSet, MemberIndex, DeviceInfoData):
		__function = windll.setupapi.SetupDiEnumDeviceInfo
		__function.argtypes = [c_void_p, c_ulong, POINTER(c_SP_DEVINFO_DATA)]
		__function.restype  = c_bool
		return __function(DeviceInfoSet, MemberIndex, DeviceInfoData)

	@staticmethod
	def SetupDiOpenDevRegKey(DeviceInfoSet, DeviceInfoData, Scope, HwProfile, KeyType, samDesired):
		__function = windll.setupapi.SetupDiOpenDevRegKey
		__function.argtypes = [c_void_p, POINTER(c_SP_DEVINFO_DATA), c_ulong, c_ulong, c_ulong, c_ulong]
		__function.restype  = c_void_p
		return __function(DeviceInfoSet, DeviceInfoData, Scope, HwProfile, KeyType, samDesired)

	@staticmethod
	def NtQueryKey(KeyHandle, KeyInformationClass, KeyInformation, Length, ResultLength):
		__function = windll.ntdll.NtQueryKey
		__function.argtypes = [c_void_p, c_int, c_void_p, c_ulong, POINTER(c_ulong)]
		__function.restype  = c_ulong
		return __function(KeyHandle, KeyInformationClass, KeyInformation, Length, ResultLength)

def GetHKEY(key):
	
	size = c_ulong()
	buff = c_void_p()

	if key != None:
		'''
			SUPPLYING THE FUNCTION "KEY_NAME_INFORMATION" (enumerated as integer 3) structure of the
			registry key. However, since the buffer size is smaller, required size of the buffer will
			be assigned to "size" variable, while returning STATUS_BUFFER_TOO_SMALL.
		'''
		result = SetupAPI.NtQueryKey(key, 3, None, 0, pointer(size))		
		print(hex(result))
		print(hex(0xC0000023))
		print(size)


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

MB_ICONERROR		= 0x00000010
MB_ICONQUESTION		= 0x00000020
MB_MB_ICONWARNING	= 0x00000030