#include "isi/core.h"

#include <cstdlib>
#include <cstring>
#include <string>

#include "analyzer.h"

#ifndef ISI_CORE_VERSION
#define ISI_CORE_VERSION "0.0.0"
#endif

extern "C" {

const char* isi_version(void) { return ISI_CORE_VERSION; }

int isi_abi_version(void) { return ISI_CORE_ABI_VERSION; }

char* isi_analyze_json(const char* source_utf8) {
  try {
    const std::string json =
        isi::ToJson(isi::Analyze(source_utf8 != nullptr ? source_utf8 : ""));
    char* out = static_cast<char*>(std::malloc(json.size() + 1));
    if (out == nullptr) return nullptr;
    std::memcpy(out, json.c_str(), json.size() + 1);
    return out;
  } catch (...) {
    return nullptr;  // never let C++ exceptions cross the C boundary
  }
}

void isi_free(char* ptr) { std::free(ptr); }

}  // extern "C"
