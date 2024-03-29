﻿using Cadmus.Core;
using System;
using System.Text.Json;
using Xunit;

namespace Cadmus.Chgc.Parts.Test;

internal static class TestHelper
{
    private static readonly JsonSerializerOptions _options =
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

    public static string SerializePart(IPart part)
    {
        ArgumentNullException.ThrowIfNull(part);

        return JsonSerializer.Serialize(part, part.GetType(), _options);
    }

    public static T? DeserializePart<T>(string json)
    where T : class, IPart, new()
    {
        ArgumentNullException.ThrowIfNull(json);

        return JsonSerializer.Deserialize<T>(json, _options);
    }

    public static void AssertPinIds(IPart part, DataPin pin)
    {
        Assert.Equal(part.ItemId, pin.ItemId);
        Assert.Equal(part.Id, pin.PartId);
        Assert.Equal(part.RoleId, pin.RoleId);
    }
}
