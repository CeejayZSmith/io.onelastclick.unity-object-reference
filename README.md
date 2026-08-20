# FinalClick — Unity Object Reference

**Package:** `io.finalclick.unity-object-reference`

Serialize interfaces and base types in the Unity Inspector. Works with both scene objects and assets.

## Installation

Add the package to your `manifest.json`:

```json
"io.finalclick.unity-object-reference": "https://github.com/FinalClick/io.finalclick.unity-object-reference?path=/Assets/Package"
```

## Usage

Reference an interface or base type with `UnityObjectReference<T>`.

```csharp
[SerializeField] UnityObjectReference<IPlayerCueInputProvider> _inputProvider;
```

Access the value through `.Value` or use the implicit conversion.

```csharp
_inputProvider.Value.DoSomething();

// Implicit conversion
IPlayerCueInputProvider input = _inputProvider;
```

## Supported References

- Scene `MonoBehaviour` instances.
- `ScriptableObject` assets.
- Any `UnityEngine.Object` that implements the target interface or inherits the target base type.

If you assign a `GameObject`, the drawer automatically finds a matching component on it.

## Inspector

The custom property drawer:

- Accepts scene objects and assets.
- Resolves matching components from `GameObject` references.
- Displays the resolved object type.
- Warns when the assigned object does not implement the required type.

<img width="1696" height="88" alt="image" src="https://github.com/user-attachments/assets/a0c18304-e809-4d15-ad4c-7366046685e8" />
