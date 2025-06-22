# DomsUtils

A growing collection of general‑purpose utilities for .NET projects. The library bundles data structures, caching helpers and small tooling components so they can be reused across applications.

## Modules

- **[BiMap](DomsUtils/DataStructures/BiMap/README.md)** – bidirectional dictionary implementation.
- **[CircularBuffer](DomsUtils/DataStructures/CircularBuffer/Base/README.md)** – fixed size queue with constant time operations.
- **[Caching](DomsUtils/Services/Caching/README.md)** – memory, file and S3 based caches with hybrid compositions.
- **[Pipeline](DomsUtils/Services/Pipeline/README.md)** – composable channel pipeline for async workloads.
- **[Async Utils](DomsUtils/Tooling/Async/README.md)** – helpers for running delegates with retries and timeouts.
- **[Enumerable Extensions](DomsUtils/Tooling/ExtensionMethods/README.md)** – LINQ style helpers for `IEnumerable<T>`.

## Getting Started

Requires **.NET 9** or later.

```bash
# clone and build
git clone https://github.com/DomDom3333/DomsUtils.git
cd DomsUtils
 dotnet build
```

Add a project reference to `DomsUtils/DomsUtils.csproj` or copy the built DLL to your project.

## Example

A simple use of the bidirectional map:

```csharp
using DomsUtils.Classes.BiMap.Base;

var map = new BiMap<int, string>();
map.Add(1, "One");
map.Add(2, "Two");
Console.WriteLine(map["Two"]); // prints 2
```

See the module guides linked above for more details and samples.

## Testing

Run the unit tests from the solution root:

```bash
 dotnet test -v q
```

## [License](License.md)
