#include <cstdio>
#include <string>

#include "isi/core.h"

static int g_failures = 0;

static std::string Analyze(const char* src) {
  char* raw = isi_analyze_json(src);
  std::string s = raw ? raw : "<null>";
  isi_free(raw);
  return s;
}

static void Expect(bool ok, const char* what, const std::string& json) {
  if (!ok) {
    ++g_failures;
    std::fprintf(stderr, "FAIL: %s\n  json: %s\n", what, json.c_str());
  }
}

static bool Has(const std::string& s, const char* part) {
  return s.find(part) != std::string::npos;
}

int main() {
  {
    auto j = Analyze("class A:\n    def f(self):\n        return 1\n\nasync def g():\n    pass\n");
    Expect(Has(j, "\"name\":\"A\""), "class symbol", j);
    Expect(Has(j, "\"name\":\"f\""), "method symbol", j);
    Expect(Has(j, "\"name\":\"g\""), "async def symbol", j);
    Expect(Has(j, "\"diagnostics\":[]"), "no diagnostics", j);
  }
  {
    auto j = Analyze("x = (1,\n     2\n");
    Expect(Has(j, "Unclosed '('"), "unclosed bracket", j);
  }
  {
    auto j = Analyze("x = [1, 2)\n");
    Expect(Has(j, "Mismatched"), "mismatched bracket", j);
  }
  {
    auto j = Analyze("s = \"\"\"abc\ndef = 1\n");
    Expect(Has(j, "Unterminated triple-quoted"), "unterminated triple", j);
  }
  {
    auto j = Analyze("s = 'abc\nprint(s)\n");
    Expect(Has(j, "Unterminated string literal"), "unterminated string", j);
  }
  {
    auto j = Analyze("if True:\n \tx = 1\n");
    Expect(Has(j, "Mixed tabs and spaces"), "mixed indentation", j);
  }
  {
    // 'def' inside a string / multi-line call must not become a symbol.
    auto j = Analyze("s = \"\"\"\ndef hidden():\n\"\"\"\nf(\ndef_=1)\n");
    Expect(!Has(j, "hidden"), "def inside string ignored", j);
  }
  {
    auto j = Analyze("def \xD1\x84\xD1\x83\xD0\xBD\xD0\xBA\xD1\x86\xD0\xB8\xD1\x8F():\n    pass\n");
    Expect(Has(j, "\"kind\":\"function\""), "unicode identifier", j);
  }
  Expect(std::string(isi_version()).size() > 0, "version", "");
  if (g_failures == 0) std::puts("isi_core: all tests passed");
  return g_failures == 0 ? 0 : 1;
}
