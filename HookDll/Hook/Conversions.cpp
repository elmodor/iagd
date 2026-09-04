#include "Conversions.h"
#include "Logger.h"
#include <cstring>
#include <codecvt>
#include <locale>
#include <utility>
#include "windows.h"

std::wstring GameStringToWString(const GAME::GameString& str)
{
    const char* data = str.c_str();
    if (!data || str.size == 0)
        return {};

    return std::wstring_convert<
        std::codecvt_utf8_utf16<wchar_t>
    >{}.from_bytes(data, data + str.size);
}
std::string WStringToUtf8(const wchar_t* wstr)
{
    if (!wstr)
        return {};

    int size = WideCharToMultiByte(CP_UTF8, 0, wstr, -1, nullptr, 0, nullptr, nullptr);
    std::string result(size - 1, '\0'); // exclude null terminator
    WideCharToMultiByte(CP_UTF8, 0, wstr, -1, result.data(), size, nullptr, nullptr);
    return result;
}
std::string WStringToUtf8(const std::wstring& wstr)
{
   return WStringToUtf8(wstr.c_str());
}
std::wstring GameStringToWString(const GAME::GameWString& str)
{
    if (str.size == 0)
        return {};

    const std::uint16_t* data = str.c_str();

    return std::wstring(
        data,
        data + str.size
    );
}

std::string GameStringToStdString(const GAME::GameString& s)
{
    const char* data = s.c_str();
    if (data == nullptr || s.size == 0)
        return {};
    return std::string(data, static_cast<size_t>(s.size));
}
GAME::ItemReplicaInfo ConvertGameReplica(
    const GAME::GameItemReplicaInfo& src)
{
    GAME::ItemReplicaInfo dst{};

    dst.id = src.id;

    dst.baseRecord       = GameStringToStdString(src.baseRecord);
    dst.prefixRecord     = GameStringToStdString(src.prefixRecord);
    dst.suffixRecord     = GameStringToStdString(src.suffixRecord);

    dst.seed             = src.seed;

    dst.modifierRecord   = GameStringToStdString(src.modifierRecord);
    dst.materiaRecord    = GameStringToStdString(src.materiaRecord);
    dst.relicBonus       = GameStringToStdString(src.relicBonus);

    dst.relicSeed        = src.relicSeed;

    dst.enchantmentRecord = GameStringToStdString(src.enchantmentRecord);
    dst.enchantmentLevel  = src.enchantmentLevel;
    dst.enchantmentSeed   = src.enchantmentSeed;

    dst.transmuteRecord  = GameStringToStdString(src.transmuteRecord);
    dst.ascendant1       = GameStringToStdString(src.ascendant1);
    dst.ascendant2       = GameStringToStdString(src.ascendant2);

    dst.var1             = src.var1;

    dst.unknownDropData  = src.unknownDropData;
    dst.stackSize        = src.stackSize;
    dst.seedRerolls      = src.seedRerolls;
    dst.affixRerolls     = src.affixRerolls;

    dst.unknownFoaField1 = src.unknownFoaField1;
    dst.unknownFoaField2 = src.unknownFoaField2;

    return dst;
}
GAME::GameString MakeGameString(const std::string& str)
{
   GAME::GameString result{};

    const std::size_t len = str.size();

    result.size = len;

    if (len < 16)
    {
        result.capacity = 15;

        if (len != 0)
            std::memcpy(result.buffer, str.data(), len);

        result.buffer[len] = '\0';
    }
    else
    {
        result.capacity = len;
        result.ptr = new char[len + 1];

        std::memcpy(result.ptr, str.data(), len);
        result.ptr[len] = '\0';
    }

    return result;
}
GAME::GameItemReplicaInfo ConvertToGameReplica(
    const GAME::ItemReplicaInfo& src)
{
   GAME::GameItemReplicaInfo dst{};

    dst.id = src.id;

    // Game-string conversion here
    dst.baseRecord       = MakeGameString(src.baseRecord);
    dst.prefixRecord     = MakeGameString(src.prefixRecord);
    dst.suffixRecord     = MakeGameString(src.suffixRecord);

    dst.seed = src.seed;

    dst.modifierRecord   = MakeGameString(src.modifierRecord);
    dst.materiaRecord    = MakeGameString(src.materiaRecord);
    dst.relicBonus       = MakeGameString(src.relicBonus);

    dst.relicSeed = src.relicSeed;

    dst.enchantmentRecord =
        MakeGameString(src.enchantmentRecord);

    dst.enchantmentLevel = src.enchantmentLevel;
    dst.enchantmentSeed = src.enchantmentSeed;

    dst.transmuteRecord =
        MakeGameString(src.transmuteRecord);

    dst.ascendant1 =
        MakeGameString(src.ascendant1);

    dst.ascendant2 =
        MakeGameString(src.ascendant2);

    dst.var1 = src.var1;
    dst.unknown164 = 0;
    dst.unknown168 = 0;
    dst.owner = src.owner;
    dst.unknownDropData = src.unknownDropData;
    dst.stackSize = src.stackSize;
    dst.seedRerolls = src.seedRerolls;
    dst.affixRerolls = src.affixRerolls;
    dst.unknownFoaField1 = src.unknownFoaField1;
    dst.unknownFoaField2 = src.unknownFoaField2;

    return dst;
}

std::vector<GAME::GameTextLine> ConvertGameTextLines(
    const GAME::GameVector<GAME::GameTextLineRaw>& gameLines)
{
    std::vector<GAME::GameTextLine> result;

    if (!gameLines.first || gameLines.first == gameLines.last)
        return result;

    const size_t count = gameLines.size();

    result.reserve(count);

    for (size_t i = 0; i < count; ++i)
    {
        const auto& raw = gameLines.first[i];

        GAME::GameTextLine line{};

        line.textClass = raw.textClass;

      try
      {
          line.text = GameStringToWString(raw.text);
      }
      catch (...)
      {
          LogToFile(
              LogLevel::WARNING,
              L"Failed converting GameTextLineRaw.text"
          );

          line.text.clear();
      }

        line.needsProcessing = raw.needsProcessing;

        line.leadingIcon = raw.leadingIcon;

        line._iconScale = raw._iconScale;

        result.emplace_back(std::move(line));
    }

    return result;
}
