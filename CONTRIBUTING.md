# Contributing

Report firmware/host issues with release version, board/display model, Windows version, protocol profile, baud and observed behavior. Redact device serial numbers, usernames and personal paths from diagnostics. Do not post credentials or whole settings folders.

Keep changes small. Firmware parsers and the four `host/src/PhatStats/Protocol` files have byte/signature guards; protocol changes need an explicit compatibility design and updated tests, not removal of the guards. Legacy baseline is reference material.

For UI text edit all four `Strings*.resx` files; preserve key names and format placeholders. Run `python tools/audit_release.py`, firmware `python tools/audit_project.py`, and host `./build_single_exe.ps1`. The translation audit checks parity; fluent review and GUI layout review are still needed for changed translations.

Do not run `firmware/tools/generate_indexed_art.py` casually: it is the historical v4 generator, not a v5 rebuild. Existing v5 indexed headers, PNGs and semantic BMP layers are the editing source. Use the [v5 asset standard](firmware/assets/V5_ICON_STANDARD.md) and compare before/after renders.

New repository contributions are GPL-3.0-or-later; firmware contributions must preserve its upstream GPL-2.0-or-later notices. Retain third-party copyright/license notices and document imported code and exact source revisions.
