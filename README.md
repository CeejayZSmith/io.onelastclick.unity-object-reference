# Unity Object Reference - [Full Documentation Here](https://docs.onelastclick.io/packages/project-settings/start-here/getting-started/)

Serialize **interfaces** directly in the Inspector.

```csharp
[SerializeField] IInterface _instance;

_instance.DoSomething();
```

- No `SerializedReference<T>` like wrapper type required.
- Reference to `ScriptableObject`, `MonoBehaviour`, or any `UnityEngine.Object` type.
- Inspector Support and Validation
