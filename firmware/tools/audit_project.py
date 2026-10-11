#!/usr/bin/env python3
from pathlib import Path
import configparser
import re
import sys

root = Path(__file__).resolve().parents[1]
failures = []
checks = []


def ok(name, condition, detail=""):
    passed = bool(condition)
    checks.append((name, passed, detail))
    if not passed:
        failures.append(name)


def extract_function(text, name):
    m = re.search(rf"void\s+{re.escape(name)}\s*\([^)]*\)\s*\{{", text)
    if not m:
        return None
    start = m.start()
    pos = m.end()
    depth = 1
    while pos < len(text) and depth:
        if text[pos] == "{":
            depth += 1
        elif text[pos] == "}":
            depth -= 1
        pos += 1
    return text[start:pos] if depth == 0 else None


def parser_signature(text):
    out = []
    for line in text.splitlines():
        s = line.strip()
        if ("inputString.indexOf" in s or "inputString.substring" in s or
                'inputString = ""' in s or "stringComplete = false" in s):
            out.append(s)
    return out


main = (root / "src/main.cpp").read_text(encoding="utf-8", errors="ignore")
portrait = (root / "src/ui/Display_Port_Batt.hpp").read_text(encoding="utf-8", errors="ignore")
config = (root / "include/PhatStatsConfig.h").read_text(encoding="utf-8", errors="ignore")
pins = (root / "include/BoardPins.h").read_text(encoding="utf-8", errors="ignore")
platform_guards = (root / "include/PlatformGuards.h").read_text(encoding="utf-8", errors="ignore")
renderer = (root / "include/PixelArtRenderer.h").read_text(encoding="utf-8", errors="ignore")
selector = (root / "include/ArtworkAutoSelect.h").read_text(encoding="utf-8", errors="ignore")
art_types = (root / "include/ArtworkTypes.h").read_text(encoding="utf-8", errors="ignore")
indexed = (root / "include/PixelArtIndexed4.h").read_text(encoding="utf-8", errors="ignore")
ini_raw = (root / "platformio.ini").read_text(encoding="utf-8", errors="strict")

# ---------------------------------------------------------------------------
# Frozen legacy protocol constraints
# ---------------------------------------------------------------------------
for fn in ("serialEvent", "serialBTEvent"):
    golden = (root / f"protocol_golden/{fn}.txt").read_text(encoding="utf-8").rstrip("\n")
    ok(f"protocol: {fn} exact golden body", extract_function(main, fn) == golden)

golden_sig = (root / "protocol_golden/portrait_parser_signature.txt").read_text(encoding="utf-8").splitlines()
ok("protocol: portrait parser exact signature", parser_signature(portrait) == golden_sig,
   f"{len(golden_sig)} guarded parser lines")
ok("protocol: no telemetry cache parser", all(x not in main for x in
   ("Telemetry_Cache", "ingestTelemetryChar", "buildCurrentPortraitSnapshot")))
ok("protocol: baud 9600", bool(re.search(r"baudRate\s*=\s*9600\s*;", config)))
ok("protocol: delimiter delay 15ms", bool(re.search(r"Serial_eventDelay\s*=\s*15\s*;", config)))
ok("protocol: input reserve 220 preserved", "inputString.reserve(220)" in main)
ok("protocol: LibreNet remains disabled", not bool(re.search(r"^\s*#define\s+enable_LibreNet\b", config, re.M)))

# ---------------------------------------------------------------------------
# PlatformIO INI grammar and stable IDs
# ---------------------------------------------------------------------------
parser = configparser.ConfigParser(interpolation=None, strict=True)
try:
    with (root / "platformio.ini").open("r", encoding="utf-8") as fh:
        parser.read_file(fh)
    ini_parse_ok = True
except Exception as exc:
    ini_parse_ok = False
    ini_error = str(exc)
ok("pio: INI parses strictly", ini_parse_ok, "" if ini_parse_ok else ini_error)

if ini_parse_ok:
    ok("pio: [platformio] section exists", parser.has_section("platformio"))
    ok("pio: [env:esp32dev] section exists", parser.has_section("env:esp32dev"))
    ok("pio: default env ID is esp32dev", parser.get("platformio", "default_envs", fallback="").strip() == "esp32dev")
    ok("pio: board ID is esp32dev", parser.get("env:esp32dev", "board", fallback="").strip() == "esp32dev")
    ok("pio: platform package is pinned", parser.get("env:esp32dev", "platform", fallback="").strip() == "espressif32@5.2.0")
    ok("pio: framework ID is arduino", parser.get("env:esp32dev", "framework", fallback="").strip() == "arduino")
    ok("pio: MCU fixed to classic esp32", parser.get("env:esp32dev", "board_build.mcu", fallback="").strip() == "esp32")
    ok("pio: CPU frequency fixed to 240MHz", parser.get("env:esp32dev", "board_build.f_cpu", fallback="").strip() == "240000000L")
    ok("pio: flash frequency fixed to 40MHz", parser.get("env:esp32dev", "board_build.f_flash", fallback="").strip() == "40000000L")
    ok("pio: flash mode conservative DIO", parser.get("env:esp32dev", "board_build.flash_mode", fallback="").strip() == "dio")
    ok("pio: upload protocol is esptool", parser.get("env:esp32dev", "upload_protocol", fallback="").strip() == "esptool")
    ok("pio: no hard-coded upload COM", not parser.has_option("env:esp32dev", "upload_port"))
    ok("pio: no hard-coded monitor COM", not parser.has_option("env:esp32dev", "monitor_port"))
    deps = [x.strip() for x in parser.get("env:esp32dev", "lib_deps", fallback="").splitlines() if x.strip()]
    ok("pio: all dependencies avoid Registry IDs", bool(deps) and all(x.startswith("https://github.com/") for x in deps), f"{len(deps)} VCS dependencies")
    ok("pio: all VCS dependencies are revision-pinned", bool(deps) and all("#" in x and len(x.rsplit("#", 1)[1]) >= 7 for x in deps))
    ok("pio: protocol guard registered as pre-script", "pre:tools/pio_protocol_guard.py" in parser.get("env:esp32dev", "extra_scripts", fallback=""))
    ok("pio: local huge-app partition selected", parser.get("env:esp32dev", "board_build.partitions", fallback="").strip() == "partitions/huge_app.csv")

# Official PlatformIO multiline syntax requires continuation lines to be indented.
multiline_keys = {"lib_deps", "extra_scripts", "build_flags"}
lines = ini_raw.splitlines()
for key in multiline_keys:
    found = False
    good = True
    for i, line in enumerate(lines):
        if re.match(rf"^{re.escape(key)}\s*=\s*$", line):
            found = True
            j = i + 1
            while j < len(lines):
                nxt = lines[j]
                if not nxt.strip() or nxt.lstrip().startswith(";"):
                    j += 1
                    continue
                if re.match(r"^[A-Za-z0-9_.-]+\s*=", nxt) or nxt.startswith("["):
                    break
                if len(nxt) - len(nxt.lstrip(" ")) < 2:
                    good = False
                j += 1
            break
    ok(f"pio: multiline indentation valid for {key}", found and good)

# ---------------------------------------------------------------------------
# 4MB WROOM-32U partition constraints
# ---------------------------------------------------------------------------
partition_path = root / "partitions/huge_app.csv"
partition_text = partition_path.read_text(encoding="utf-8", errors="strict") if partition_path.exists() else ""
ok("flash: local huge_app.csv exists", partition_path.exists())
ok("flash: app partition is exactly 0x300000 (3 MiB)", bool(re.search(r"^app0,\s*app,\s*ota_0,\s*0x10000,\s*0x300000,", partition_text, re.M)))
ok("flash: partition layout fits 4MB", "0x310000,0xF0000" in partition_text)
ok("flash: no second application slot", not bool(re.search(r"^app1,", partition_text, re.M)))

# ---------------------------------------------------------------------------
# VS Code / PlatformIO IDE constraints
# ---------------------------------------------------------------------------
ok("vscode: no manual c_cpp_properties.json", not (root / ".vscode/c_cpp_properties.json").exists(),
   "PlatformIO must own IntelliSense/compiler defines")
ok("vscode: PlatformIO extension recommendation present", "platformio.platformio-ide" in (root / ".vscode/extensions.json").read_text(encoding="utf-8"))
ok("vscode: no active .inc fragments", not any((root / "src").rglob("*.inc")))
ok("vscode: no active Arduino .ino translation units", not any((root / "src").rglob("*.ino")))
ok("vscode: UI fragments are C++ headers", all((root / f"src/ui/{n}.hpp").exists() for n in (
   "Display_Port_Batt", "FeatureSet_Indicator_ESP", "Z_Button_Modes", "NeopixelRingGauges", "PCB_ThresholdTriggerEvents")))
guard_py = (root / "tools/pio_protocol_guard.py").read_text(encoding="utf-8")
ok("vscode: SCons injected IDs suppress Pylance false-positive", "reportUndefinedVariable=false" in guard_py)
ok("vscode: no stale lolin build path", "lolin32_lite" not in (root / "build_and_audit.cmd").read_text(errors="ignore") and
   "lolin32_lite" not in (root / "build_and_audit.ps1").read_text(errors="ignore"))

# ---------------------------------------------------------------------------
# Classic ESP32/WROOM platform and reserved GPIO constraints
# ---------------------------------------------------------------------------
ok("platform: classic ESP32 compile guard", "CONFIG_IDF_TARGET_ESP32" in platform_guards)
ok("platform: exact esp32dev board macro guard", "ARDUINO_ESP32_DEV" in platform_guards)
ok("platform: Arduino-ESP32 2.0.5 compile guard", all(x in platform_guards for x in ("ESP_ARDUINO_VERSION_MAJOR", "ESP_ARDUINO_VERSION_MINOR", "ESP_ARDUINO_VERSION_PATCH")))
ok("platform: 240MHz F_CPU compile guard", "F_CPU != 240000000L" in platform_guards)
for pin in (25, 26, 32, 33):
    ok(f"gpio: GPIO{pin} reserved", bool(re.search(rf"PHAT_PIN_EXP{pin}\s+{pin}\b", pins)))
for pin in (34, 35):
    ok(f"gpio: GPIO{pin} reserved input-only", bool(re.search(rf"PHAT_PIN_EXP{pin}\s+{pin}\b", pins)) and "input-only" in pins)
ok("gpio: default functional pins statically blocked from expansion pins", "phatIsReservedExpansionPin" in platform_guards)
ok("gpio: battery monitor disabled by default", not bool(re.search(r"^\s*#define\s+batteryMonitor\b", config, re.M)))

# Reserved expansion pins must not appear as raw operational GPIO literals in src/.
reserved_literal_hits = []
for fp in (root / "src").rglob("*"):
    if fp.suffix.lower() not in {".cpp", ".hpp", ".h"}:
        continue
    t = fp.read_text(encoding="utf-8", errors="ignore")
    for pin in (25,26,32,33,34,35):
        if re.search(rf"\b(?:pinMode|digitalWrite|digitalRead|analogRead|ledcAttachPin)\s*\([^\n;]*\b{pin}\b", t):
            reserved_literal_hits.append(f"{fp.relative_to(root)}:GPIO{pin}")
ok("gpio: no raw operational use of reserved expansion pins", not reserved_literal_hits, ",".join(reserved_literal_hits))

# ---------------------------------------------------------------------------
# UI/artwork integrity checks
# ---------------------------------------------------------------------------
ok("ui: no per-frame CPU/GPU status clear block", "Boost/Turbo Clear Box" not in portrait)
ok("ui: no boost delay(350)", "delay(350)" not in portrait)
ok("ui: independent status calls", "updateCpuStatusIndicator" in portrait and "updateGpuStatusIndicator" in portrait)

# UI geometry is frozen independently from non-geometry UI logic. This permits
# artwork auto-selection while rejecting coordinate/font-size changes.
def geometry_signature(text):
    pat = re.compile(r"\b(?:tft\.(?:drawRoundRect|fillRoundRect|fillRect|drawBitmap|setCursor|fillCircle|drawCircle|drawFastHLine|drawFastVLine|drawLine|setTextSize)|drawCpuPixelArt|drawGpuPixelArt)\s*\(")
    return [line.strip() for line in text.splitlines() if pat.search(line)]

golden_geometry = (root / "protocol_golden/portrait_geometry_signature.txt").read_text(encoding="utf-8").splitlines()
ok("ui: portrait geometry signature unchanged", geometry_signature(portrait) == golden_geometry, f"{len(golden_geometry)} guarded geometry lines")
ok("ui: artwork selector executes before CPU/GPU image draw", portrait.find("phatRefreshArtworkSelection(inputString)") >= 0 and portrait.find("phatRefreshArtworkSelection(inputString)") < portrait.find("drawCpuPixelArt(16, 25)"))

ok("art: indexed renderer uses exact RGB565 path", "drawIndexed4Art" in renderer and "drawRGBBitmap" in renderer)
ok("art: config locks indexed4 renderer", "PHAT_ART_FORMAT_INDEXED4 1" in config)
ok("art: indexed renderer does not layer 1-bit masks", not bool(re.search(r"\btft\.drawBitmap\s*\(", renderer)))
ok("art: legacy generated 1-bit pixel header removed", not (root / "include/PixelArtBitmaps.h").exists())
ok("art: indexed dimensions fixed 82x82", "PHAT_ART_W = 82" in indexed and "PHAT_ART_H = 82" in indexed and "PHAT_ART_ROW_BYTES = 41" in indexed)
ok("art: exact preview exists", (root / "assets/preview_exact_indexed4.png").exists())
ok("art: auto-selection enabled by default", "PHAT_ARTWORK_AUTO = true" in config)
ok("art: selector is read-only frame inspection", "phatRefreshArtworkSelection" in selector and not bool(re.search(r"\bframe\s*=|\bframe\.(?:remove|replace|setCharAt|concat)\s*\(", selector)))
ok("art: selector uses existing CPU/GPU/GMT tags only", all(tag in selector for tag in ('"CPU:"', '"GPU:"', '"GMT"')))
ok("art: selector has no serial transport calls", not any(tok in selector for tok in ("Serial.write", "Serial.print", "SerialBT", "delay(")))
ok("art: manual fallback remains configurable", "PHAT_MANUAL_CPU_ART" in config and "PHAT_MANUAL_GPU_ART" in config)
ok("art: all requested GPU classes represented", all(name in art_types for name in ("NvidiaGtx", "NvidiaGtxTi", "NvidiaRtx", "NvidiaRtxTi", "NvidiaRtxSuper", "NvidiaPro", "AmdRadeon", "AmdRdna", "AmdRadeonIgpu", "AmdRdnaIgpu", "IntelArc", "IntelUhdIgpu", "IntelXeIgpu")))

# Catch missing indexed-art identifiers referenced by PixelArtRenderer.h.
asset_refs = sorted(set(re.findall(r"\b((?:CPU|GPU)_[A-Z0-9_]+_I4)\b", renderer)))
missing_asset_ids = [x for x in asset_refs if not re.search(rf"\b{re.escape(x)}\b", indexed)]
ok("ids: all indexed-art identifiers exist", not missing_asset_ids,
   "missing=" + ",".join(missing_asset_ids) if missing_asset_ids else f"{len(asset_refs)} IDs")

# Every image must contain exactly 3362 packed bytes (82*82/2).
asset_defs = re.finditer(r"const uint8_t\s+((?:CPU|GPU)_[A-Z0-9_]+_I4)\[PHAT_ART_PACKED_BYTES\]\s+PROGMEM\s*=\s*\{(.*?)\};", indexed, re.S)
asset_sizes = {}
for m in asset_defs:
    asset_sizes[m.group(1)] = len(re.findall(r"0x[0-9A-Fa-f]{2}", m.group(2)))
bad_sizes = {k:v for k,v in asset_sizes.items() if v != 3362}
ok("art: all indexed assets are exact packed size", bool(asset_sizes) and not bad_sizes,
   f"{len(asset_sizes)} assets" if not bad_sizes else str(bad_sizes))

print("PhatStats VS Code / PlatformIO project audit")
print("==========================================")
for name, passed, detail in checks:
    print(f"[{'PASS' if passed else 'FAIL'}] {name}" + (f" — {detail}" if detail else ""))
print()
if failures:
    print("AUDIT FAILED:", ", ".join(failures))
    sys.exit(1)
print("AUDIT PASSED")
