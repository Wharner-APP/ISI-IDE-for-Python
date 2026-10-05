# ISI IDE native core: code intelligence (scanner/outline/diagnostics).
add_library(isi_core SHARED src/core.cpp src/analyzer.cpp)

target_include_directories(isi_core PUBLIC include PRIVATE src)
target_compile_features(isi_core PUBLIC cxx_std_20)
target_compile_definitions(isi_core PRIVATE
  ISI_CORE_BUILD
  ISI_CORE_VERSION="${PROJECT_VERSION}")

set_target_properties(isi_core PROPERTIES
  CXX_VISIBILITY_PRESET hidden
  VISIBILITY_INLINES_HIDDEN ON
  POSITION_INDEPENDENT_CODE ON
  # "$<0:>" stops multi-config generators (MSVC/Xcode) from adding /Release.
  RUNTIME_OUTPUT_DIRECTORY "${ISI_NATIVE_OUT}$<0:>"
  LIBRARY_OUTPUT_DIRECTORY "${ISI_NATIVE_OUT}$<0:>")

if(MSVC)
  target_compile_options(isi_core PRIVATE /W4 /utf-8 /permissive-)
else()
  target_compile_options(isi_core PRIVATE -Wall -Wextra -Wpedantic)
endif()

# Make the Linux build independent of the host libstdc++ version.
if(CMAKE_CXX_COMPILER_ID STREQUAL "GNU")
  target_link_options(isi_core PRIVATE -static-libstdc++ -static-libgcc)
endif()

if(ISI_BUILD_TESTS)
  add_executable(isi_core_tests tests/core_tests.cpp)
  target_link_libraries(isi_core_tests PRIVATE isi_core)
  set_target_properties(isi_core_tests PROPERTIES
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/tests$<0:>")
  add_test(NAME isi_core_tests COMMAND isi_core_tests)
endif()
