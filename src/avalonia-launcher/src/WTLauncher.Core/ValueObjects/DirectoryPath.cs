// <copyright file="DirectoryPath.cs" company="WT Launcher">
// Copyright (c) 2024 WT Launcher Contributors. All rights reserved.
// </copyright>

using Vogen;

namespace WTLauncher.Core.ValueObjects;

/// <summary>Strongly-typed filesystem directory path. Validated at construction.</summary>
[ValueObject<string>]
[Instance("Empty", "")]
public readonly partial struct DirectoryPath
{
    private static Validation Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Validation.Invalid("Directory path must not be empty.");
        if (value.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            return Validation.Invalid("Directory path contains invalid characters.");
        return Validation.Ok;
    }
}
