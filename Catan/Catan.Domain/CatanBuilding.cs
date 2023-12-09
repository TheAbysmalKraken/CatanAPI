﻿using static Catan.Common.Enumerations;

namespace Catan.Domain;

public class CatanBuilding
{
    public CatanBuilding()
    {
        Colour = CatanPlayerColour.None;
        Type = CatanBuildingType.None;
    }

    public CatanBuilding(CatanPlayerColour colour, CatanBuildingType type)
    {
        Colour = colour;
        Type = type;
    }

    public CatanPlayerColour Colour { get; private set; }
    
    public CatanBuildingType Type { get; private set; }
}
