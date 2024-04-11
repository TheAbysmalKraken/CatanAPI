﻿using System.Text.Json.Serialization;

namespace Catan.API.Requests;

internal sealed class PointRequest
{
    [JsonPropertyName("x")]
    public required int X { get; init; }

    [JsonPropertyName("y")]
    public required int Y { get; init; }

    public Point ToPoint() => new Point(X, Y);
}
