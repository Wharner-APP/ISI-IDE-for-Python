// ISI IDE for Python - native core (C ABI).
// (c) Wharner Group. Developed by Wharner APP.
#ifndef ISI_CORE_H_
#define ISI_CORE_H_

#if defined(_WIN32)
#ifdef ISI_CORE_BUILD
#define ISI_API __declspec(dllexport)
#else
#define ISI_API __declspec(dllimport)
#endif
#else
#define ISI_API __attribute__((visibility("default")))
#endif

#ifdef __cplusplus
extern "C" {
#endif

#define ISI_CORE_ABI_VERSION 1

// Semantic version of the native core ("0.1.0"). Static string, do not free.
ISI_API const char* isi_version(void);

// ABI version, bumped on breaking changes of the exported functions.
ISI_API int isi_abi_version(void);

// Analyzes Python source (UTF-8) and returns a malloc'ed UTF-8 JSON string:
//   {"symbols":[{"kind","name","line","indent"}],
//    "diagnostics":[{"severity","message","line","column"}]}
// Returns NULL on failure. The result MUST be released with isi_free().
ISI_API char* isi_analyze_json(const char* source_utf8);

ISI_API void isi_free(char* ptr);

#ifdef __cplusplus
}  // extern "C"
#endif

#endif  // ISI_CORE_H_
