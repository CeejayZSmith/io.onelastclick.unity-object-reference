# Unity Object Reference - [Full Documentation Here](https://docs.onelastclick.io/packages/unity-object-reference/start-here/getting-started/)


Serialize **interfaces** directly in the Inspector.

```csharp
[SerializeField] IInterface _instance;

_instance.DoSomething();
```

- No `SerializedReference<T>` like wrapper type required.
- Reference to `ScriptableObject`, `MonoBehaviour`, or any `UnityEngine.Object` type.
- Inspector Support and Validation

## Installation

### Install via Unity Package Manager

1. Open your Unity project.
2. Go to **Window → Package Manager**.
3. Click the **+** button in the top-left corner.
4. Select **Add package from Git URL...**.
5. Paste the following URL and click **Add**:

```text
https://github.com/ceejayzsmith/io.onelastclick.unity-object-reference?path=/Assets/Package
```

### Install via `manifest.json`

If you prefer to manage packages manually, add the package to your project's `Packages/manifest.json` file under `dependencies`:

```json
{
  "dependencies": {
    "io.onelastclick.unity-object-reference": "https://github.com/ceejayzsmith/io.onelastclick.unity-object-reference?path=/Assets/Package"
  }
}
```
