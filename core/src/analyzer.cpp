#include "analyzer.h"

#include <cctype>
#include <cstdio>
#include <cstring>

namespace isi {
namespace {

bool IsIdentChar(unsigned char c) {
  return std::isalnum(c) != 0 || c == '_' || c >= 0x80;
}

class Scanner {
 public:
  explicit Scanner(std::string_view source) : s_(source) {}

  AnalysisResult Run() {
    bool line_start = true;
    bool continued = false;  // explicit backslash line continuation
    while (!Eof()) {
      if (line_start && brackets_.empty()) {
        HandleLineStart();
        line_start = false;
        continue;
      }
      const char c = Cur();
      if (c == '\n') {
        Step();
        if (brackets_.empty() && !continued) line_start = true;
        continued = false;
      } else if (c == '\\') {
        if (Peek(1) == '\n' || (Peek(1) == '\r' && Peek(2) == '\n')) {
          continued = true;
        }
        Step();
      } else if (c == '#') {
        while (!Eof() && Cur() != '\n') Step();
      } else if (c == '"' || c == '\'') {
        SkipString();
      } else if (c == '(' || c == '[' || c == '{') {
        brackets_.push_back({c, line_, col_});
        Step();
      } else if (c == ')' || c == ']' || c == '}') {
        CloseBracket(c);
        Step();
      } else {
        Step();
      }
    }
    for (const Bracket& b : brackets_) {
      Add(b.line, b.col, "error", std::string("Unclosed '") + b.ch + "'");
    }
    return std::move(result_);
  }

 private:
  struct Bracket {
    char ch;
    int line;
    int col;
  };

  bool Eof() const { return i_ >= s_.size(); }
  char Cur() const { return s_[i_]; }
  char Peek(size_t offset) const {
    return i_ + offset < s_.size() ? s_[i_ + offset] : '\0';
  }

  // Consumes one byte, tracking line/column (column counts code points).
  void Step() {
    const unsigned char c = static_cast<unsigned char>(s_[i_++]);
    if (c == '\n') {
      ++line_;
      col_ = 1;
    } else if ((c & 0xC0) != 0x80) {
      ++col_;
    }
  }

  void Add(int line, int col, const char* severity, std::string message) {
    result_.diagnostics.push_back({severity, std::move(message), line, col});
  }

  static char Opener(char closer) {
    return closer == ')' ? '(' : (closer == ']' ? '[' : '{');
  }

  void CloseBracket(char closer) {
    if (brackets_.empty()) {
      Add(line_, col_, "error", std::string("Unmatched '") + closer + "'");
      return;
    }
    const Bracket top = brackets_.back();
    if (top.ch != Opener(closer)) {
      Add(line_, col_, "error",
          std::string("Mismatched '") + closer + "' (opened with '" + top.ch +
              "' on line " + std::to_string(top.line) + ")");
    }
    brackets_.pop_back();
  }

  void SkipString() {
    const char q = Cur();
    const int start_line = line_;
    const int start_col = col_;
    if (Peek(1) == q && Peek(2) == q) {  // triple-quoted
      Step();
      Step();
      Step();
      while (!Eof()) {
        const char c = Cur();
        if (c == '\\') {
          Step();
          if (!Eof()) Step();
          continue;
        }
        if (c == q && Peek(1) == q && Peek(2) == q) {
          Step();
          Step();
          Step();
          return;
        }
        Step();
      }
      Add(start_line, start_col, "error", "Unterminated triple-quoted string");
      return;
    }
    Step();
    while (!Eof()) {
      const char c = Cur();
      if (c == '\\') {
        Step();
        if (!Eof()) Step();
        continue;
      }
      if (c == q) {
        Step();
        return;
      }
      if (c == '\n') break;
      Step();
    }
    Add(start_line, start_col, "error", "Unterminated string literal");
  }

  // Called at the first byte of every logical line.
  void HandleLineStart() {
    size_t j = i_;
    bool tab = false;
    bool space = false;
    while (j < s_.size() && (s_[j] == ' ' || s_[j] == '\t' || s_[j] == '\f')) {
      if (s_[j] == '\t') tab = true;
      if (s_[j] == ' ') space = true;
      ++j;
    }
    const bool blank =
        j >= s_.size() || s_[j] == '\n' || s_[j] == '\r' || s_[j] == '#';
    if (!blank && tab && space) {
      Add(line_, 1, "warning", "Mixed tabs and spaces in indentation");
    }

    auto starts_with = [&](const char* kw) {
      const size_t n = std::strlen(kw);
      return j + n <= s_.size() && s_.compare(j, n, kw) == 0;
    };
    const char* kind = nullptr;
    size_t k = j;
    if (starts_with("def ")) {
      kind = "function";
      k = j + 4;
    } else if (starts_with("async def ")) {
      kind = "function";
      k = j + 10;
    } else if (starts_with("class ")) {
      kind = "class";
      k = j + 6;
    }
    if (kind != nullptr) {
      while (k < s_.size() && s_[k] == ' ') ++k;
      const size_t begin = k;
      while (k < s_.size() && IsIdentChar(static_cast<unsigned char>(s_[k]))) {
        ++k;
      }
      if (k > begin) {
        result_.symbols.push_back({kind, std::string(s_.substr(begin, k - begin)),
                                   line_, static_cast<int>(j - i_)});
      }
    }
    while (i_ < j) Step();
  }

  std::string_view s_;
  size_t i_ = 0;
  int line_ = 1;
  int col_ = 1;
  std::vector<Bracket> brackets_;
  AnalysisResult result_;
};

void AppendEscaped(std::string* out, const std::string& s) {
  out->push_back('"');
  for (const unsigned char c : s) {
    switch (c) {
      case '"': out->append("\\\""); break;
      case '\\': out->append("\\\\"); break;
      case '\n': out->append("\\n"); break;
      case '\r': out->append("\\r"); break;
      case '\t': out->append("\\t"); break;
      default:
        if (c < 0x20) {
          char buf[8];
          std::snprintf(buf, sizeof(buf), "\\u%04x", c);
          out->append(buf);
        } else {
          out->push_back(static_cast<char>(c));
        }
    }
  }
  out->push_back('"');
}

}  // namespace

AnalysisResult Analyze(std::string_view source) {
  return Scanner(source).Run();
}

std::string ToJson(const AnalysisResult& r) {
  std::string out = "{\"symbols\":[";
  for (size_t i = 0; i < r.symbols.size(); ++i) {
    const Symbol& s = r.symbols[i];
    if (i != 0) out.push_back(',');
    out.append("{\"kind\":");
    AppendEscaped(&out, s.kind);
    out.append(",\"name\":");
    AppendEscaped(&out, s.name);
    out.append(",\"line\":" + std::to_string(s.line));
    out.append(",\"indent\":" + std::to_string(s.indent) + "}");
  }
  out.append("],\"diagnostics\":[");
  for (size_t i = 0; i < r.diagnostics.size(); ++i) {
    const Diagnostic& d = r.diagnostics[i];
    if (i != 0) out.push_back(',');
    out.append("{\"severity\":");
    AppendEscaped(&out, d.severity);
    out.append(",\"message\":");
    AppendEscaped(&out, d.message);
    out.append(",\"line\":" + std::to_string(d.line));
    out.append(",\"column\":" + std::to_string(d.column) + "}");
  }
  out.append("]}");
  return out;
}

}  // namespace isi
