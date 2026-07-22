// <copyright file="PresetId.cs" company="WT Launcher">
// Copyright (c) 2024 WT Launcher Contributors. All rights reserved.
// </copyright>

using Vogen;

namespace WTLauncher.Core.ValueObjects;

/// <summary>Strongly-typed preset identifier. Never null, never empty.</summary>
[ValueObject<string>]
[Instance("Empty", "")]
public readonly partial struct PresetId
{
    private static Validation Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Validation.Invalid("Preset ID must not be empty.");
        return Validation.Ok;
    }
}
