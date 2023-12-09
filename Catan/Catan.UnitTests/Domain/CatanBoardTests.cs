﻿using Catan.Domain;
using static Catan.Common.Enumerations;

namespace Catan.UnitTests.Domain;

public sealed class CatanBoardTests
{
    [Fact]
    public void CreateCatanBoard_EmptyTilesAreNull()
    {
        // Act
        var board = new CatanBoard();
        var tiles = board.GetTiles();

        // Assert
        Assert.Null(tiles[0, 0]);
        Assert.Null(tiles[0, 1]);
        Assert.Null(tiles[1, 0]);
        Assert.Null(tiles[4, 3]);
        Assert.Null(tiles[4, 4]);
        Assert.Null(tiles[3, 4]);
    }

    [Fact]
    public void CreateCatanBoard_CorrectResourceCounts()
    {
        // Arrange
        var resourceCounts = DomainConstants.GetTileResourceTypeTotals();

        // Act
        var board = new CatanBoard();
        var tiles = board.GetTiles();

        foreach (var tile in tiles)
        {
            if (tile is null) continue;

            var tileType = tile.Type;

            resourceCounts[tileType]--;
        }

        Assert.Equal(0, resourceCounts[CatanResourceType.Wood]);
        Assert.Equal(0, resourceCounts[CatanResourceType.Brick]);
        Assert.Equal(0, resourceCounts[CatanResourceType.Sheep]);
        Assert.Equal(0, resourceCounts[CatanResourceType.Wheat]);
        Assert.Equal(0, resourceCounts[CatanResourceType.Ore]);
        Assert.Equal(0, resourceCounts[CatanResourceType.Desert]);
    }

    [Fact]
    public void CreateCatanBoard_CorrectActivationNumberCounts()
    {
        // Arrange
        var activationNumberCounts = DomainConstants.GetTileActivationNumberTotals();

        // Act
        var board = new CatanBoard();
        var tiles = board.GetTiles();

        foreach (var tile in tiles)
        {
            if (tile is null) continue;

            var tileNumber = tile.ActivationNumber;

            if (tileNumber == 0) continue;

            activationNumberCounts[tileNumber]--;
        }

        Assert.Equal(0, activationNumberCounts[2]);
        Assert.Equal(0, activationNumberCounts[3]);
        Assert.Equal(0, activationNumberCounts[4]);
        Assert.Equal(0, activationNumberCounts[5]);
        Assert.Equal(0, activationNumberCounts[6]);
        Assert.Equal(0, activationNumberCounts[7]);
        Assert.Equal(0, activationNumberCounts[8]);
        Assert.Equal(0, activationNumberCounts[9]);
        Assert.Equal(0, activationNumberCounts[10]);
        Assert.Equal(0, activationNumberCounts[11]);
        Assert.Equal(0, activationNumberCounts[12]);
    }

    [Fact]
    public void CreateCatanBoard_EmptySettlementsAndCitiesAreNull()
    {
        // Act
        var board = new CatanBoard();
        var houses = board.GetHouses();
        int notNullCount = 0;

        foreach(var house in houses)
        {
            if (house != null) notNullCount++;
        }

        // Assert
        Assert.Null(houses[0, 0]);
        Assert.Null(houses[0, 1]);
        Assert.Null(houses[1, 0]);
        Assert.Null(houses[0, 4]);
        Assert.Null(houses[0, 5]);
        Assert.Null(houses[1, 5]);
        Assert.Null(houses[9, 0]);
        Assert.Null(houses[10, 0]);
        Assert.Null(houses[10, 1]);
        Assert.Null(houses[10, 4]);
        Assert.Null(houses[10, 5]);
        Assert.Null(houses[9, 5]);

        Assert.Equal(54, notNullCount);
    }

    [Fact]
    public void CreateCatanBoard_IndividualRoadCoordinatesAreAllValid()
    {
        // Act
        var board = new CatanBoard();
        var roads = board.GetRoads();

        // Assert
        foreach (var road in roads)
        {
            Assert.NotEqual(new Coordinates(0, 0), road.FirstCornerCoordinates);
            Assert.NotEqual(new Coordinates(0, 0), road.SecondCornerCoordinates);
            Assert.NotEqual(new Coordinates(0, 1), road.FirstCornerCoordinates);
            Assert.NotEqual(new Coordinates(0, 1), road.SecondCornerCoordinates);
            Assert.NotEqual(new Coordinates(1, 0), road.FirstCornerCoordinates);
            Assert.NotEqual(new Coordinates(1, 0), road.SecondCornerCoordinates);
            Assert.NotEqual(new Coordinates(0, 4), road.FirstCornerCoordinates);
            Assert.NotEqual(new Coordinates(0, 4), road.SecondCornerCoordinates);
            Assert.NotEqual(new Coordinates(0, 5), road.FirstCornerCoordinates);
            Assert.NotEqual(new Coordinates(0, 5), road.SecondCornerCoordinates);
            Assert.NotEqual(new Coordinates(1, 5), road.FirstCornerCoordinates);
            Assert.NotEqual(new Coordinates(1, 5), road.SecondCornerCoordinates);
            Assert.NotEqual(new Coordinates(9, 0), road.FirstCornerCoordinates);
            Assert.NotEqual(new Coordinates(9, 0), road.SecondCornerCoordinates);
            Assert.NotEqual(new Coordinates(10, 0), road.FirstCornerCoordinates);
            Assert.NotEqual(new Coordinates(10, 0), road.SecondCornerCoordinates);
            Assert.NotEqual(new Coordinates(10, 1), road.FirstCornerCoordinates);
            Assert.NotEqual(new Coordinates(10, 1), road.SecondCornerCoordinates);
            Assert.NotEqual(new Coordinates(10, 4), road.FirstCornerCoordinates);
            Assert.NotEqual(new Coordinates(10, 4), road.SecondCornerCoordinates);
            Assert.NotEqual(new Coordinates(10, 5), road.FirstCornerCoordinates);
            Assert.NotEqual(new Coordinates(10, 5), road.SecondCornerCoordinates);
            Assert.NotEqual(new Coordinates(9, 5), road.FirstCornerCoordinates);
            Assert.NotEqual(new Coordinates(9, 5), road.SecondCornerCoordinates);
        }

        Assert.Equal(72, roads.Count);
    }

    [Fact]
    public void CreateCatanBoard_CorrectNumberOfPorts()
    {
        // Arrange
        var correctPortLocations = DomainConstants.GetStartingPortCoordinates();

        // Act
        var board = new CatanBoard();
        var ports = board.GetPorts();

        // Assert
        Assert.Equal(correctPortLocations.Count, ports.Count);
    }

    [Fact]
    public void CreateCatanBoard_PortsAreAtCorrectLocations()
    {
        // Arrange
        var correctPortLocations = DomainConstants.GetStartingPortCoordinates();

        // Act
        var board = new CatanBoard();
        var ports = board.GetPorts();

        // Assert
        foreach (var portLocation in correctPortLocations)
        {
            var possiblePorts = ports.Where(x => x.Coordinates.Equals(portLocation));
            Assert.NotEmpty(possiblePorts);
        }
    }

    [Fact]
    public void CreateCatanBoard_PortsAreCorrectTypes()
    {
        // Arrange
        var remainingPortTypeTotals = DomainConstants.GetPortTypeTotals();

        foreach (var type in remainingPortTypeTotals.Keys)
        {
            remainingPortTypeTotals[type] *= 2;
        }

        // Act
        var board = new CatanBoard();
        var ports = board.GetPorts();

        // Assert
        foreach (var port in ports)
        {
            if (port is null) continue;

            var tileType = port.Type;

            remainingPortTypeTotals[tileType]--;
        }

        Assert.Equal(0, remainingPortTypeTotals[CatanPortType.Wood]);
        Assert.Equal(0, remainingPortTypeTotals[CatanPortType.Brick]);
        Assert.Equal(0, remainingPortTypeTotals[CatanPortType.Sheep]);
        Assert.Equal(0, remainingPortTypeTotals[CatanPortType.Wheat]);
        Assert.Equal(0, remainingPortTypeTotals[CatanPortType.Ore]);
        Assert.Equal(0, remainingPortTypeTotals[CatanPortType.ThreeToOne]);
    }
}
