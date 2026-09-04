#pragma once

#include <string>
#include <vector>
#include <cstdint>
#include <cstddef>
#include "GrimTypes.h"

// Forward declarations/types should already be available here.

std::wstring GameStringToWString(const GAME::GameString& str);

std::string WStringToUtf8(const wchar_t* wstr);
std::string WStringToUtf8(const std::wstring& wstr);

std::wstring GameStringToWString(const GAME::GameWString& str);

std::string GameStringToStdString(const GAME::GameString& s);

GAME::ItemReplicaInfo ConvertGameReplica(
    const GAME::GameItemReplicaInfo& src);

GAME::GameString MakeGameString(
    const std::string& str);

GAME::GameItemReplicaInfo ConvertToGameReplica(
    const GAME::ItemReplicaInfo& src);

std::vector<GAME::GameTextLine> ConvertGameTextLines(
    const GAME::GameVector<GAME::GameTextLineRaw>& gameLines);
