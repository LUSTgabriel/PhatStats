# Build fix v1.3.1

Fixes C# CS0019 in `Hardware/SnapshotValidator.cs`.

Previous expression:

```csharp
DegradedFields = degraded ?? Array.Empty<string>()
```

`degraded` is `List<string>?` while the fallback is `string[]`; the null-coalescing operator requires a compatible operand type before assignment target conversion.

Corrected expression:

```csharp
DegradedFields = (IReadOnlyList<string>?)degraded ?? Array.Empty<string>()
```

This preserves the allocation-saving lazy `List<string>` behavior and the existing `IReadOnlyList<string>` model contract. No serial protocol files were changed.
