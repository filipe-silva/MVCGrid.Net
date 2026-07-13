// Entry point for the .NET WebAssembly browser app. Grid registration happens lazily
// on the first interop call (see Interop.EnsureInitialized), so nothing is required
// here — the runtime stays loaded and the [JSExport] methods are called from main.js.
using System;

Console.WriteLine("MVCGrid WASM demo runtime started.");
