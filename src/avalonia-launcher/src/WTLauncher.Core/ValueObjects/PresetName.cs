// <copyright file="PresetName.cs" company="WT Launcher">
// Copyright (c) 2024 WT Launcher Contributors. All rights reserved.
// </copyright>

using Vogen;

namespace WTLauncher.Core.ValueObjects;

/// <summary>Strongly-typed preset display name. Never null, never empty.</summary>
[ValueObject<string>]
[Instance("Empty", "")]
public readonly partial struct PresetName
{
    private static Validation Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Validation.Invalid("Preset name must not be empty.");
        return Validation.Ok;
    }
}
