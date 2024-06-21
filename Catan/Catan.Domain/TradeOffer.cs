﻿using Catan.Domain.Enums;

namespace Catan.Domain;

public sealed class TradeOffer
{
    public bool IsActive { get; set; } = true;

    public required Dictionary<ResourceType, int> Offer { get; init; }

    public required Dictionary<ResourceType, int> Request { get; init; }

    public List<PlayerColour> RejectedBy { get; init; } = [];
}
