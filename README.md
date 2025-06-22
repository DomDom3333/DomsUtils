# DomsUtils

A curated collection of utilities used across several .NET projects. The solution bundles data structures, caching helpers and asynchronous tooling so they can be consumed from a single package.

## Modules

- **[BiMap](DomsUtils/DataStructures/BiMap/README.md)** – bidirectional dictionaries with JSON support and observable variants.
- **[CircularBuffer](DomsUtils/DataStructures/CircularBuffer/Base/README.md)** – fixed size queue offering constant time operations.
- **[Caching](DomsUtils/Services/Caching/README.md)** – memory, file and S3 based caches with hybrid compositions and migration helpers.
- **[Pipeline](DomsUtils/Services/Pipeline/README.md)** – composable channel pipeline for asynchronous workloads.
- **[Async Utils](DomsUtils/Tooling/Async/README.md)** – helpers for running delegates with retries and timeouts.
- **[Enumerable Extensions](DomsUtils/Tooling/ExtensionMethods/README.md)** – LINQ style helpers for `IEnumerable<T>`.

## Getting Started

Requires **.NET 9** or later.

```bash
# clone and build
git clone https://gitea.essenhofer.at/DomDom3333/DomsUtils.git
cd DomsUtils
 dotnet build
```

Reference `DomsUtils/DomsUtils.csproj` from your project or copy the built DLL.

## Quick Examples

### BiMap
```csharp
using DomsUtils.DataStructures.BiMap.Base;
var map = new BiMap<int,string> { {1,"one"}, {2,"two"} };
Console.WriteLine(map["two"]); // prints 2
```

### CircularBuffer
```csharp
var buf = new CircularBuffer<int>(4);
buf.EnqueueOverwrite(1);
buf.TryEnqueue(2);
```

See the module guides linked above for in depth explanations and more samples.

## Testing

Run the unit tests from the solution root:

```bash
 dotnet test -v q
```

## [License](License.md)
