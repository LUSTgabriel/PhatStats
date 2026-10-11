# Build fix v1.2

This package fixes the two compile failures reported from .NET SDK 10.0.400.

1. **CS0246 `EncodedField`** — restored the `EncodedField` record used by `ProtocolEncoder.GetCurrentFields()`.
2. **NETSDK1151** — ordinary solution builds are now framework-dependent. `win-x64`, `SelfContained=true`, and `PublishSingleFile=true` are applied only by the publish step.
3. Added a dedicated `win-x64` restore before the `--no-restore` publish step.
4. `build_single_exe.cmd` still pauses on success or failure so the console log remains visible.

No telemetry wire-format change is made by this fix.
