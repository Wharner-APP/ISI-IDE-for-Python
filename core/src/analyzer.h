// Lightweight single-pass Python scanner: outline + structural diagnostics.
#ifndef ISI_ANALYZER_H_
#define ISI_ANALYZER_H_

#include <string>
#include <string_view>
#include <vector>

namespace isi {

struct Symbol {
  std::string kind;  // "function" | "class"
  std::string name;
  int line = 0;    // 1-based
  int indent = 0;  // leading whitespace characters
};

struct Diagnostic {
  std::string severity;  // "error" | "warning"
  std::string message;
  int line = 0;    // 1-based
  int column = 0;  // 1-based, in code points
};

struct AnalysisResult {
  std::vector<Symbol> symbols;
  std::vector<Diagnostic> diagnostics;
};

AnalysisResult Analyze(std::string_view source);
std::string ToJson(const AnalysisResult& result);

}  // namespace isi

#endif  // ISI_ANALYZER_H_
