#include <stdio.h>
#include <stdlib.h>
#include "MessageType.h"
#include "GetPrivateStash.h"
#include "Exports.h"
#include <codecvt> // wstring_convert
#include "Logger.h"
#include "GrimTypes.h"
#include "MinHook.h"

HANDLE GetPrivateStash::m_hEvent;
DataQueue* GetPrivateStash::m_dataQueue;
GetPrivateStash::OriginalMethodPtr GetPrivateStash::originalMethod;
void* GetPrivateStash::privateStashSack;

void GetPrivateStash::EnableHook() {
    originalMethod = (OriginalMethodPtr)GetProcAddressOrLogToFile(
        L"Game.dll",
        GET_PRIVATE_STASH
    );

    if (originalMethod == NULL) {
        LogToFile(
            LogLevel::FATAL,
            L"Failed to hook GetPrivateStash, instaloot private-stash deposits will not work"
        );
        return;
    }

    LPVOID target = reinterpret_cast<LPVOID>(originalMethod);

    MH_STATUS status = MH_CreateHook(
        target,
        reinterpret_cast<LPVOID>(HookedMethod64),
        reinterpret_cast<LPVOID*>(&originalMethod)
    );

    if (status != MH_OK) {
        LogToFile(
            LogLevel::FATAL,
            L"Failed to create MinHook hook for GetPrivateStash"
        );
        return;
    }

    status = MH_EnableHook(target);

    if (status != MH_OK) {
        LogToFile(
            LogLevel::FATAL,
            L"Failed to enable MinHook hook for GetPrivateStash"
        );

        MH_RemoveHook(target);
        return;
    }
}

GetPrivateStash::GetPrivateStash(DataQueue* dataQueue, HANDLE hEvent) {
	GetPrivateStash::m_dataQueue = dataQueue;
	GetPrivateStash::m_hEvent = hEvent;
	GetPrivateStash::privateStashSack = NULL;
}

GetPrivateStash::GetPrivateStash() {
	GetPrivateStash::m_hEvent = NULL;
	GetPrivateStash::privateStashSack = NULL;
}

void* GetPrivateStash::GetPrivateStashInventorySack() {
	return privateStashSack;
}

void* __stdcall GetPrivateStash::HookedMethod64(void* This) {
	void* v = originalMethod(This);
	try {
		// Capture the private stash inventory sack pointer for instaloot; stash open/close status is no longer reported.
		privateStashSack = v;
	}
	catch (std::exception& ex) {
		std::wstring_convert<std::codecvt_utf8_utf16<wchar_t>> converter;
		std::wstring wide = converter.from_bytes(ex.what());
		LogToFile(LogLevel::FATAL, L"Error parsing in GetPrivateStash.. " + wide);
	}
	catch (...) {
		LogToFile(LogLevel::FATAL, L"Error parsing in GetPrivateStash.. (triple-dot)");
	}

	return v;
}
