# pyright: reportUndefinedVariable=false
# PlatformIO executes this file inside SCons and injects Import()/env.
Import("env")

from pathlib import Path
import re

project = Path(env.subst("$PROJECT_DIR"))
main_path = project / "src" / "main.cpp"
portrait_path = project / "src" / "ui" / "Display_Port_Batt.hpp"
golden = project / "protocol_golden"
config_path = project / "include" / "PhatStatsConfig.h"


def extract_function(text: str, name: str) -> str:
    match = re.search(rf"void\s+{re.escape(name)}\s*\([^)]*\)\s*\{{", text)
    if not match:
        raise RuntimeError(f"Protocol guard: function {name} not found")
    start = match.start()
    pos = match.end()
    depth = 1
    while pos < len(text) and depth:
        ch = text[pos]
        if ch == "{":
            depth += 1
        elif ch == "}":
            depth -= 1
        pos += 1
    if depth != 0:
        raise RuntimeError(f"Protocol guard: unbalanced function {name}")
    return text[start:pos]


def parser_signature(text: str):
    result = []
    for line in text.splitlines():
        stripped = line.strip()
        if ("inputString.indexOf" in stripped or
            "inputString.substring" in stripped or
            'inputString = ""' in stripped or
            "stringComplete = false" in stripped):
            result.append(stripped)
    return result


main = main_path.read_text(encoding="utf-8", errors="ignore")
portrait = portrait_path.read_text(encoding="utf-8", errors="ignore")
config = config_path.read_text(encoding="utf-8", errors="ignore")

for function_name in ("serialEvent", "serialBTEvent"):
    expected = (golden / f"{function_name}.txt").read_text(encoding="utf-8").rstrip("\n")
    actual = extract_function(main, function_name)
    if actual != expected:
        raise RuntimeError(
            f"PROTOCOL GUARD FAILED: {function_name} differs from upstream V3.1.7.3 golden source."
        )

expected_signature = (golden / "portrait_parser_signature.txt").read_text(encoding="utf-8").splitlines()
actual_signature = parser_signature(portrait)
if actual_signature != expected_signature:
    raise RuntimeError(
        "PROTOCOL GUARD FAILED: portrait inputString indexOf/substring parser signature changed."
    )

if "Telemetry_Cache" in main or "ingestTelemetryChar" in main or "buildCurrentPortraitSnapshot" in main:
    raise RuntimeError("PROTOCOL GUARD FAILED: alternate/incremental telemetry parser detected.")

if not re.search(r"baudRate\s*=\s*9600\s*;", config):
    raise RuntimeError("PROTOCOL GUARD FAILED: default baudRate is not 9600.")
if not re.search(r"Serial_eventDelay\s*=\s*15\s*;", config):
    raise RuntimeError("PROTOCOL GUARD FAILED: Serial_eventDelay is not 15 ms.")

print("[PhatStats] Protocol guard: PASS")
print("  USB serialEvent: exact upstream body")
print("  BT serialBTEvent: exact upstream body")
print("  Portrait parser signature: exact upstream indexOf/substring sequence")
print("  baudRate: 9600")
print("  Serial_eventDelay: 15 ms")
