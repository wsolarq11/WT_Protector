// <copyright file="SearchTerm.cs" company="WT Launcher">
// Copyright (c) 2024 WT Launcher Contributors. All rights reserved.
// </copyright>

using Vogen;

namespace WTLauncher.Core.ValueObjects;

/// <summary>Strongly-typed search history term. Guards against empty strings.</summary>
[ValueObject<string>]
[Instance("Empty", "")]
public readonly partial struct SearchTerm
{
    private static Validation Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Validation.Invalid("Search term must not be empty.");
        return Validation.Ok;
    }
}
