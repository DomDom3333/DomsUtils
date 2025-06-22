# EnumerableExtensions

Convenience methods for working with `IEnumerable<T>` collections. They avoid boilerplate when checking for nulls or converting between common collection types.

## Functions
- `IsNullOrEmpty()` / `HasItems()` – quick null and count checks.
- `ForEach(action)` – iterate with an action delegate.
- `WhereNotNull()` – filter out nulls.
- `FirstOrDefaultSafe()` / `LastOrDefaultSafe()` – efficient boundary retrieval.
- `ToHashSetSafe()` – convert to `HashSet` even if source is null.
- `DistinctSafe()` – distinct elements with null safety.

## Example
```csharp
using DomsUtils.Tooling.ExtensionMethods;

var items = new[] { 1, 2, 2, 3 };
items.DistinctSafe().ForEach(Console.WriteLine);
```
