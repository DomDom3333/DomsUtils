# CircularBuffer

A fixed-size circular buffer optimized for constant time enqueue and dequeue operations. Capacity must be a power of two which allows wrap around using bit masks.

## Key Members
- `TryEnqueue(item)` – attempts to add an item if space is available.
- `EnqueueOverwrite(item)` – adds an item, overwriting the oldest when full.
- `TryDequeue(out item)` – removes the oldest element.
- `TryPeek(out item)` – reads the next element without removing it.
- `Clear(bool zeroMemory)` – resets the buffer.
- `GetEnumerator()` – allocation-free enumerator over contents.

## Example
```csharp
using DomsUtils.DataStructures.CircularBuffer.Base;

var buffer = new CircularBuffer<int>(8);

buffer.TryEnqueue(1);
buffer.EnqueueOverwrite(2);

if (buffer.TryPeek(out int head))
    Console.WriteLine($"next: {head}");

while (buffer.TryDequeue(out int value))
    Console.WriteLine(value);
```
