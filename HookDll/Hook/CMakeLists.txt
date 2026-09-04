cmake_minimum_required(VERSION 3.29)

if(CMAKE_SOURCE_DIR STREQUAL CMAKE_CURRENT_SOURCE_DIR AND NOT CMAKE_HOST_WIN32)
    set(CMAKE_SYSTEM_NAME Windows)
    set(CMAKE_C_COMPILER x86_64-w64-mingw32-gcc)
    set(CMAKE_CXX_COMPILER x86_64-w64-mingw32-g++)
    set(CMAKE_RC_COMPILER x86_64-w64-mingw32-windres)
endif()

project(Hook)
enable_language(CXX)

set(CMAKE_CXX_STANDARD 17)

if (CMAKE_BUILD_TYPE STREQUAL "Release")
    add_compile_definitions(NOLOG NDEBUG WIN32_LEAN_AND_MEAN)
    add_compile_options(
        -O3
        -g0
        -DNDEBUG
        -ffunction-sections
        -fdata-sections
        -fno-ident
        # -flto
    )
    add_link_options(
        -s
        # -flto
        -Wl,--gc-sections
        -Wl,--strip-all
    )
endif()
if(NOLOG)
    add_compile_definitions(NOLOG)
endif()

set(BOOST_ROOT "${CMAKE_SOURCE_DIR}/boost")
set(BOOST_B2 "${BOOST_ROOT}/b2")
set(BOOST_STAGE "${BOOST_ROOT}/stage")
set(BOOST_LIB   "${BOOST_STAGE}/lib")
set(BOOST_FILESYSTEM_LIB
    "${BOOST_LIB}/libboost_filesystem.a"
)
set(BOOST_THREAD_LIB
    "${BOOST_LIB}/libboost_thread.a"
)

add_custom_command(
    OUTPUT "${BOOST_B2}"
    COMMAND
        /bin/bash "${BOOST_ROOT}/bootstrap.sh"
    WORKING_DIRECTORY
        "${BOOST_ROOT}"
    COMMENT
        "Bootstrapping Boost.Build"
    VERBATIM
)

add_custom_command(
    OUTPUT
        "${BOOST_FILESYSTEM_LIB}"
        "${BOOST_THREAD_LIB}"
    COMMAND
        "${BOOST_B2}"
        --user-config="${CMAKE_SOURCE_DIR}/user-config.jam"
        toolset=gcc-1
        target-os=windows
        address-model=64
        variant=release
        link=static
        runtime-link=static
        threading=multi
        threadapi=win32
        --with-filesystem
        --with-thread
        stage
    WORKING_DIRECTORY
        "${BOOST_ROOT}"
    DEPENDS "${BOOST_B2}"
    COMMENT
        "Building Boost"
)
add_custom_target(boost_build
    DEPENDS
        "${BOOST_FILESYSTEM_LIB}"
        "${BOOST_THREAD_LIB}"
)

add_library(Boost::filesystem STATIC IMPORTED GLOBAL)
set_target_properties(Boost::filesystem PROPERTIES
    IMPORTED_LOCATION
        "${BOOST_FILESYSTEM_LIB}"
    INTERFACE_INCLUDE_DIRECTORIES
        "${BOOST_ROOT}"
)
add_dependencies(Boost::filesystem boost_build)

add_library(Boost::thread STATIC IMPORTED GLOBAL)
set_target_properties(Boost::thread PROPERTIES
    IMPORTED_LOCATION
        "${BOOST_STAGE}/lib/libboost_thread.a"
    INTERFACE_INCLUDE_DIRECTORIES
        "${BOOST_ROOT}"
)
add_dependencies(Boost::thread boost_build)


include(FetchContent)
set(FETCHCONTENT_QUIET OFF)
FetchContent_Declare(
    minhook
    GIT_REPOSITORY https://github.com/TsudaKageyu/minhook.git
    GIT_TAG d94c64d32ea37bc4f5ee47d580709f70c6fb6080
    GIT_PROGRESS TRUE)
FetchContent_MakeAvailable(minhook)

include_directories(.)

add_library(Hook SHARED
    BaseMethodHook.cpp
    DataQueue.cpp
    dllmain.cpp
    GameEngineUpdate.cpp
    GetPrivateStash.cpp
    GrimTypes.cpp
    HookLog.cpp
    InventorySack_AddItem.cpp
    OnDemandSeedInfo.cpp
    SetHardcore.cpp
    SettingsReader.cpp
    Conversions.cpp
)

target_link_libraries(Hook PRIVATE
    minhook
    Boost::filesystem
    Boost::thread
)

target_link_options(Hook PRIVATE
    -static
    -static-libgcc
    -static-libstdc++
)

set_target_properties(Hook PROPERTIES
    PREFIX ""
    OUTPUT_NAME "Hook"
)
