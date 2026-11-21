// ================================================================================
// FILE: UpdaterCoreTests.cpp
// PROJECT: RaggieSoftUpdate (Cross-Platform C++20 Unit Tests for UpdaterCore.hpp)
// LICENSE: MIT License
//
// PLAIN-ENGLISH OVERVIEW:
//   Compiles and runs directly on macOS/Linux ('clang++ -std=c++20 UpdaterCoreTests.cpp')
//   or Windows to verify our zero-dependency JSON parser, dotted version comparator,
//   and Inno-style '/SILENT' & '/VERYSILENT' command-line switch parser!
// ================================================================================

#include <cassert>
#include <iostream>
#include "UpdaterCore.hpp"

int main() {
    using namespace raggiesoft::updater;

    // 1. Test semantic dotted version comparisons
    assert(CompareVersionStrings("1.0.0", "1.0.0") == 0);
    assert(CompareVersionStrings("1.0.0", "1.1.0") == -1);
    assert(CompareVersionStrings("1.1.0", "2.0.0") == -1);
    assert(CompareVersionStrings("1.10.0", "1.2.0") == 1);
    assert(CompareVersionStrings("v2.0.0+abc123", "2.0.0") == 0);

    // 2. Test Inno-style and POSIX command-line flags
    auto a1 = ParseUpdaterArgs({"/VERYSILENT", "--check-only", "--app", "StoreTrainer"});
    assert(a1.mode == UiMode::VerySilent);
    assert(a1.checkOnly == true);
    assert(a1.focusAppId == "storetrainer");

    auto a2 = ParseUpdaterArgs({"/SILENT", "/APP=book-reader"});
    assert(a2.mode == UiMode::SilentProgress);
    assert(a2.focusAppId == "book-reader");

    // 3. Test zero-dependency 'apps-manifest.json' parsing & merging
    auto catalog = GetBuiltInSuiteCatalog();
    assert(catalog.size() == 5);

    const char* sampleManifest = R"JSON([
      {
        "id": "storetrainer",
        "innoAppId": "{8E92D4A1-63F0-4C8B-9B7A-10164500B004}",
        "title": "RaggieSoft StoreTrainer",
        "latestVersion": "2.1.0",
        "latestClassicVersion": "1.1.2"
      },
      {
        "id": "book-reader",
        "latestVersion": "1.0.5"
      }
    ])JSON";

    bool ok = MergeRemoteManifestJson(sampleManifest, catalog);
    assert(ok);

    for (auto& item : catalog) {
        if (item.id == "storetrainer") {
            assert(item.latestVersion == "2.1.0");
            assert(item.latestClassicVersion == "1.1.2");
            item.isInstalled = true;
            item.installedVersion = "1.1.0";
            assert(item.HasUpdate() == true);
        }
        if (item.id == "book-reader") {
            assert(item.latestVersion == "1.0.5");
        }
    }

    std::cout << "✅ RaggieSoftUpdate C++20 core unit tests passed (version math, CLI parser, & JSON manifest parser)!\n";
    return 0;
}
