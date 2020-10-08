from ctypes import *

'''
    DWORD   -> c_ulong
    PCWSTR  -> c_wchar_p
'''

class c_guid(Structure):
    _field_ = [("Data1", c_ulong),
               ("Data2", c_ushort),
               ("Data3", c_ushort),
               ("Data4", c_ubyte * 8)]

class SetupAPI:
    @staticmethod
    def SetupDiClassGuidsFromNameW(ClassName, ClassGuidList, ClassGuidListSize, RequiredSize):
        __function = windll.setupapi.SetupDiClassGuidsFromNameW
        __function.argtypes = [c_wchar_p, POINTER(c_guid), c_ulong, POINTER(c_ulong)]
        __function.restype  = c_bool

        return __function(ClassName, ClassGuidList, ClassGuidListSize, RequiredSize)

def MessageBoxW(hWnd, lpText, lpCaption, uType):
    windll.user32.MessageBoxW(hWnd, lpText, lpCaption, uType)

MB_ICONERROR        = int("0x00000010", 16)
MB_ICONQUESTION     = int("0x00000020", 16)
MB_MB_ICONWARNING   = int("0x00000030", 16)