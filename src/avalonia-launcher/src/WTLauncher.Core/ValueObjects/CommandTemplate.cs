// <copyright file="CommandTemplate.cs" company="WT Launcher">
// Copyright (c) 2024 WT Launcher Contributors. All rights reserved.
// </copyright>

using Vogen;

namespace WTLauncher.Core.ValueObjects;

/// <summary>Strongly-typed command template key or value. Never null.</summary>
[ValueObject<string>]
[Instance("Empty", "")]
public readonly partial struct CommandTemplate
{
    private static Validation Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Validation.Invalid("Command template must not be empty.");
        return Validation.Ok;
    }
}
