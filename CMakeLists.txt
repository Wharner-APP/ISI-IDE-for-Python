cmake_minimum_required(VERSION 3.20)
project(ISI_IDE VERSION 0.1.0 LANGUAGES CXX)

# ---------------------------------------------------------------------------
# ISI IDE for Python - root build
#   (c) Wharner Group. Developed by Wharner APP.
#
# Native part (C++20):   cmake -S . -B build-native -DCMAKE_BUILD_TYPE=Release
#                        cmake --build build-native --config Release
# Managed part (C#/.NET): see build.ps1 / build.sh or the optional
#                        `isi_ide_publish` target below.
# ---------------------------------------------------------------------------

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

# Static MSVC runtime: no vcredist needed on the user's machine.
set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")

if(NOT CMAKE_BUILD_TYPE AND NOT CMAKE_CONFIGURATION_TYPES)
  set(CMAKE_BUILD_TYPE Release CACHE STRING "Build type" FORCE)
endif()

option(ISI_BUILD_TESTS "Build native unit tests" ON)
set(ISI_NATIVE_OUT "${CMAKE_BINARY_DIR}/native-out" CACHE PATH
    "Where isi_core.dll / libisi_core.so / libisi_core.dylib is placed")
set(ISI_RID "" CACHE STRING
    ".NET runtime identifier for the isi_ide_publish target (e.g. win-x64)")

if(ISI_BUILD_TESTS)
  enable_testing()
endif()

add_subdirectory(core)

# Optional convenience target: builds the C# GUI around the native core.
find_program(ISI_DOTNET dotnet)
if(ISI_DOTNET AND ISI_RID)
  add_custom_target(isi_ide_publish
    COMMAND "${ISI_DOTNET}" publish
            "${CMAKE_SOURCE_DIR}/src/ISI.IDE/ISI.IDE.csproj"
            -c Release -r ${ISI_RID} --self-contained true
            -p:NativeLibDir=${ISI_NATIVE_OUT}
            -o "${CMAKE_BINARY_DIR}/publish/ISI IDE for Python"
    DEPENDS isi_core
    COMMENT "Publishing ISI IDE for Python (${ISI_RID})"
    VERBATIM)
endif()
