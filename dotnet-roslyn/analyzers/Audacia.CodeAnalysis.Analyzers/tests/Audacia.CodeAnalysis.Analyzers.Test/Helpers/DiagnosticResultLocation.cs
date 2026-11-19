using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Audacia.CodeAnalysis.Analyzers.Test.Helpers;

/// <summary>
/// Location where the diagnostic appears, as determined by path, line number, and column number.
/// </summary>
public readonly struct DiagnosticResultLocation
{
    public DiagnosticResultLocation(string path, int line, int column)
    {
        if (line < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(line), "line must be >= -1");
        }

        if (column < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(column), "column must be >= -1");
        }

        Path = path;
        Line = line;
        Column = column;
    }

    public string Path { get; }

    public int Line { get; }

    public int Column { get; }
}

/// <summary>
/// Struct that stores information about a Diagnostic appearing in a source
/// </summary>
public struct DiagnosticResult
{
    public List<DiagnosticResultLocation> Locations
    {
        get => field ??= [];
        set;
    }

    public DiagnosticSeverity Severity { get; set; }

    public string Id { get; set; }

    public string Message { get; set; }

    public string Path => Locations.Count > 0 ? Locations[0].Path : string.Empty;

    public int Line => Locations.Count > 0 ? Locations[0].Line : -1;

    public int Column => Locations.Count > 0 ? Locations[0].Column : -1;
}
