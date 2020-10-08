#include <iostream>
#include <Windows.h>

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
