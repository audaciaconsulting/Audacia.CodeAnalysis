# Purpose of this Library

The `Audacia.CodeAnalysis.Analyzers.Helpers` library contains helper code for the Roslyn analyzers in the `Audacia.CodeAnalysis.Analyzers` library.

If code needs to be referenced by code using the analyzers, it must be located in this library. This is because the analyzer library is not referenced as a standard dll, meaning its types are not accessible.

For example, analyzers may provide an attribute to allow certain values to be overridden.