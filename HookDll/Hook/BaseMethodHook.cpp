
#include "BaseMethodHook.h"
#include "MessageType.h"
#include "GrimTypes.h"
#include "MinHook.h"

BaseMethodHook::BaseMethodHook()
    : m_messageId(0),
      m_dataQueue(nullptr),
      m_hEvent(nullptr)
{}
BaseMethodHook::BaseMethodHook(DataQueue* dataQueue, HANDLE hEvent)
    : m_messageId(0),
      m_dataQueue(dataQueue),
      m_hEvent(hEvent)
{}

void BaseMethodHook::EnableHook() {}

void BaseMethodHook::ReportHookError(DataQueue* m_dataQueue, HANDLE m_hEvent, int id) {
	DataItemPtr item(new DataItem(TYPE_ERROR_HOOKING_GENERIC, sizeof(id), (char*)&id));
	m_dataQueue->push(item);
	SetEvent(m_hEvent);
}

void BaseMethodHook::ReportHookSuccess(DataQueue* m_dataQueue, HANDLE m_hEvent, int id) {
	DataItemPtr item(new DataItem(TYPE_SUCCESS_HOOKING_GENERIC, sizeof(id), (char*)&id));
	m_dataQueue->push(item);
	SetEvent(m_hEvent);
}

void BaseMethodHook::TransferData(unsigned int size, const char* data) {
	DataItemPtr item(new DataItem(m_messageId, size, data));
	m_dataQueue->push(item);
	SetEvent(m_hEvent);
}

void* BaseMethodHook::HookDll(
    const wchar_t* dll,
    char* procAddress,
    void* HookedMethod,
    DataQueue* m_dataQueue,
    HANDLE m_hEvent,
    int id)
{
    void* target = GetProcAddressOrLogToFile(dll, procAddress);

    m_messageId = id;

    if (target == nullptr)
    {
        ReportHookError(m_dataQueue, m_hEvent, id);
        return nullptr;
    }

    void* trampoline = nullptr;

    MH_STATUS status = MH_CreateHook(
        target,
        HookedMethod,
        &trampoline
    );

    if (status != MH_OK)
    {
        ReportHookError(m_dataQueue, m_hEvent, id);
        return nullptr;
    }

    status = MH_EnableHook(target);

    if (status != MH_OK)
    {
        MH_RemoveHook(target);

        ReportHookError(m_dataQueue, m_hEvent, id);
        return nullptr;
    }

    ReportHookSuccess(m_dataQueue, m_hEvent, id);

    return trampoline;
}

void* BaseMethodHook::HookGame(char* procAddress, void* HookedMethod, DataQueue* m_dataQueue, HANDLE m_hEvent, int id) {
	return HookDll(L"Game.dll", procAddress, HookedMethod, m_dataQueue, m_hEvent, id);
}

void* BaseMethodHook::HookEngine(char* procAddress, void* HookedMethod, DataQueue* m_dataQueue, HANDLE m_hEvent, int id) {
	return HookDll(L"Engine.dll", procAddress, HookedMethod, m_dataQueue, m_hEvent, id);
}
