﻿using static Catan.Common.Enumerations;

namespace Catan.Domain;

public class CatanGame
{
    private const int MIN_PLAYERS = 3;
    private const int MAX_PLAYERS = 4;

    private readonly List<CatanPlayer> players = new();
    private readonly List<int> rolledDice = new();
    private readonly Dictionary<CatanResourceType, int> remainingResourceCards = new();
    private readonly Dictionary<CatanDevelopmentCardType, int> remainingDevelopmentCardTotals = new();
    private readonly List<CatanDevelopmentCardType> remainingDevelopmentCards = new();
    private int currentPlayerIndex = 0;
    private int knightsRequiredForLargestArmy;
    private bool developmentCardPlayedThisTurn;
    private readonly Random random = new();

    public CatanGame(int numberOfPlayers)
    {
        if (numberOfPlayers < MIN_PLAYERS || numberOfPlayers > MAX_PLAYERS)
        {
            throw new ArgumentException($"Invalid number of players: '{numberOfPlayers}'");
        }

        Board = new CatanBoard();

        RollDice();
        InitialisePlayers(numberOfPlayers);
        InitialiseResourceCards();
        InitialiseDevelopmentCards();

        knightsRequiredForLargestArmy = 3;
        developmentCardPlayedThisTurn = false;

        GamePhase = 0;
    }

    public CatanBoard Board { get; private set; } = new();

    public CatanPlayer? LongestRoadPlayer { get; private set; } = null;

    public CatanPlayer? LargestArmyPlayer => players.FirstOrDefault(p => p.HasLargestArmy);

    public int DiceTotal => rolledDice.Sum();

    public CatanPlayer CurrentPlayer => players[currentPlayerIndex];

    public int GamePhase { get; private set; }

    public List<CatanPlayer> GetPlayers() => players;

    public List<int> GetRolledDice() => rolledDice;

    public Dictionary<CatanResourceType, int> GetRemainingResourceCards()
        => remainingResourceCards;

    public Dictionary<CatanDevelopmentCardType, int> GetRemainingDevelopmentCardTotals()
        => remainingDevelopmentCardTotals;

    public List<CatanDevelopmentCardType> GetRemainingDevelopmentCards()
        => remainingDevelopmentCards;

    public void NextPlayer()
    {
        UpdateLargestArmyPlayer();

        developmentCardPlayedThisTurn = false;

        CurrentPlayer.MoveOnHoldDevelopmentCardsToPlayable();

        if (GamePhase == 1)
        {
            if (currentPlayerIndex == 0)
            {
                GamePhase = 2;
            }
            else
            {
                currentPlayerIndex = (currentPlayerIndex - 1) % players.Count;
            }
        }
        else
        {
            if (GamePhase == 0 && currentPlayerIndex == players.Count - 1)
            {
                GamePhase = 1;
            }
            else
            {
                currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
            }
        }
    }

    public void RollDice()
    {
        rolledDice.Clear();
        rolledDice.AddRange(DiceRoller.RollDice(2, 6));
    }

    public bool DistributeResourcesToPlayers()
    {
        var diceTotal = DiceTotal;

        if (diceTotal == 7)
        {
            return false;
        }

        var tileCoordinatesWithActivationNumber = Board.GetCoordinatesOfTilesWithActivationNumber(diceTotal);

        foreach (var coordinates in tileCoordinatesWithActivationNumber)
        {
            var houses = Board.GetHousesOnTile(coordinates);

            foreach (var house in houses)
            {
                var player = GetPlayerByColour(house.Colour);

                if (player == null)
                {
                    continue;
                }

                var isCity = house.Type == CatanBuildingType.City;

                var resourceType = Board.GetTile(coordinates.X, coordinates.Y).Type;

                if (remainingResourceCards[resourceType] > 0)
                {
                    player.AddResourceCard(resourceType);
                    remainingResourceCards[resourceType]--;

                    if (isCity && remainingResourceCards[resourceType] > 0)
                    {
                        player.AddResourceCard(resourceType);
                        remainingResourceCards[resourceType]--;
                    }
                }
            }
        }

        return true;
    }

    public bool GiveResourcesSurroundingHouse(Coordinates coordinates)
    {
        var tiles = Board.GetTilesSurroundingHouse(coordinates);

        foreach (var tile in tiles)
        {
            var resourceType = tile.Type;

            if (remainingResourceCards[resourceType] > 0)
            {
                CurrentPlayer.AddResourceCard(resourceType);
                remainingResourceCards[resourceType]--;
            }
        }

        return true;
    }

    public bool TradeTwoToOne(CatanResourceType resourceTypeToGive, CatanResourceType resourceTypeToReceive)
    {
        var portTypeValid = Enum.TryParse<CatanPortType>(resourceTypeToReceive.ToString(), out var portType);

        if (!portTypeValid)
        {
            return false;
        }

        if (!CurrentPlayer.CanTradeTwoToOneOfCardType(resourceTypeToGive)
        || !Board.ColourHasPortOfType(CurrentPlayer.Colour, portType))
        {
            return false;
        }

        CurrentPlayer.TradeTwoToOne(resourceTypeToGive, resourceTypeToReceive);

        return true;
    }

    public bool TradeThreeToOne(CatanResourceType resourceTypeToGive, CatanResourceType resourceTypeToReceive)
    {
        if (!CurrentPlayer.CanTradeThreeToOneOfCardType(resourceTypeToGive)
        || !Board.ColourHasPortOfType(CurrentPlayer.Colour, CatanPortType.ThreeToOne))
        {
            return false;
        }

        CurrentPlayer.TradeThreeToOne(resourceTypeToGive, resourceTypeToReceive);

        return true;
    }

    public bool TradeFourToOne(CatanResourceType resourceTypeToGive, CatanResourceType resourceTypeToReceive)
    {
        if (!CurrentPlayer.CanTradeFourToOneOfCardType(resourceTypeToGive))
        {
            return false;
        }

        CurrentPlayer.TradeFourToOne(resourceTypeToGive, resourceTypeToReceive);

        return true;
    }

    public bool EmbargoPlayer(CatanPlayerColour colourEmbargoedBy, CatanPlayerColour colourToEmbargo)
    {
        if (colourEmbargoedBy == colourToEmbargo
        || colourEmbargoedBy == CatanPlayerColour.None
        || colourToEmbargo == CatanPlayerColour.None)
        {
            return false;
        }

        var playerEmbargoedBy = GetPlayerByColour(colourEmbargoedBy);

        if (playerEmbargoedBy == null)
        {
            return false;
        }

        playerEmbargoedBy.EmbargoPlayer(colourToEmbargo);

        return true;
    }

    public bool RemovePlayerEmbargo(CatanPlayerColour colourEmbargoedBy, CatanPlayerColour colourToEmbargo)
    {
        if (colourEmbargoedBy == colourToEmbargo
        || colourEmbargoedBy == CatanPlayerColour.None
        || colourToEmbargo == CatanPlayerColour.None)
        {
            return false;
        }

        var playerEmbargoedBy = GetPlayerByColour(colourEmbargoedBy);

        if (playerEmbargoedBy == null)
        {
            return false;
        }

        playerEmbargoedBy.RemoveEmbargo(colourToEmbargo);

        return true;
    }

    public bool PlayKnightCard(Coordinates robberCoordinates, CatanPlayerColour colourToStealFrom)
    {
        if (!CanPlayDevelopmentCard(CatanDevelopmentCardType.Knight)
        || !Board.GetHouseColoursOnTile(robberCoordinates).Contains(colourToStealFrom))
        {
            return false;
        }

        var originalRobberCoordinates = Board.RobberPosition;

        var moveRobberSuccess = MoveRobber(robberCoordinates);

        if (!moveRobberSuccess)
        {
            return false;
        }

        var stealResourceSuccess = StealResourceCard(colourToStealFrom);

        if (!stealResourceSuccess)
        {
            Board.MoveRobberToCoordinates(originalRobberCoordinates);
            return false;
        }

        PlayDevelopmentCard(CatanDevelopmentCardType.Knight);

        return true;
    }

    public bool PlayYearOfPlentyCard(CatanResourceType resourceType1, CatanResourceType resourceType2)
    {
        if (!CanPlayDevelopmentCard(CatanDevelopmentCardType.YearOfPlenty))
        {
            return false;
        }

        var resourceTotal1 = remainingResourceCards[resourceType1];
        var resourceTotal2 = remainingResourceCards[resourceType2];

        if (resourceTotal1 == 0 || resourceTotal2 == 0)
        {
            return false;
        }

        if (resourceType1 == resourceType2 && resourceTotal1 < 2)
        {
            return false;
        }

        CurrentPlayer.AddResourceCard(resourceType1);
        CurrentPlayer.AddResourceCard(resourceType2);

        PlayDevelopmentCard(CatanDevelopmentCardType.YearOfPlenty);

        return true;
    }

    public bool BuildFreeRoad(Coordinates coordinates1, Coordinates coordinates2)
    {
        if (!Board.CanPlaceRoadBetweenCoordinates(coordinates1, coordinates2, CurrentPlayer.Colour))
        {
            return false;
        }

        Board.PlaceRoad(coordinates1, coordinates2, CurrentPlayer.Colour);

        return true;
    }

    public bool BuildRoad(Coordinates coordinates1, Coordinates coordinates2)
    {
        if (!CurrentPlayer.CanPlaceRoad() || !Board.CanPlaceRoadBetweenCoordinates(coordinates1, coordinates2, CurrentPlayer.Colour))
        {
            return false;
        }

        CurrentPlayer.PlaceRoad();
        Board.PlaceRoad(coordinates1, coordinates2, CurrentPlayer.Colour);

        UpdateLargestRoadPlayer();

        return true;
    }

    public bool BuildFreeSettlement(Coordinates coordinates)
    {
        if (!Board.CanPlaceHouseAtCoordinates(coordinates, CurrentPlayer.Colour, true))
        {
            return false;
        }

        Board.PlaceHouse(coordinates, CurrentPlayer.Colour, true);

        return true;
    }

    public bool BuildSettlement(Coordinates coordinates)
    {
        if (!CurrentPlayer.CanPlaceSettlement() || !Board.CanPlaceHouseAtCoordinates(coordinates, CurrentPlayer.Colour))
        {
            return false;
        }

        CurrentPlayer.PlaceSettlement();
        Board.PlaceHouse(coordinates, CurrentPlayer.Colour);

        UpdateLargestRoadPlayer();

        return true;
    }

    public bool BuildCity(Coordinates coordinates)
    {
        if (!CurrentPlayer.CanPlaceCity() || !Board.CanUpgradeHouseAtCoordinates(coordinates, CurrentPlayer.Colour))
        {
            return false;
        }

        CurrentPlayer.PlaceCity();
        Board.UpgradeHouse(coordinates, CurrentPlayer.Colour);

        return true;
    }

    public bool MoveRobber(Coordinates coordinates)
    {
        if (!Board.CanMoveRobberToCoordinates(coordinates))
        {
            return false;
        }

        Board.MoveRobberToCoordinates(coordinates);

        return true;
    }

    public bool StealResourceCard(CatanPlayerColour victimColour)
    {
        var victim = GetPlayerByColour(victimColour);

        if (victim == null || victim == CurrentPlayer)
        {
            return false;
        }

        var stolenCard = victim.RemoveRandomResourceCard();

        if (stolenCard != null)
        {
            CurrentPlayer.AddResourceCard(stolenCard.Value);
        }

        return true;
    }

    public bool DiscardCards(Dictionary<CatanResourceType, int> cardsToDiscard)
    {
        if (!CurrentPlayer.CanDiscardResourceCards(cardsToDiscard))
        {
            return false;
        }

        CurrentPlayer.DiscardResourceCards(cardsToDiscard);

        foreach (var type in cardsToDiscard.Keys)
        {
            remainingResourceCards[type] += cardsToDiscard[type];
        }

        return true;
    }

    private void InitialisePlayers(int numberOfPlayers)
    {
        players.Clear();

        for (var i = 0; i < numberOfPlayers; i++)
        {
            CatanPlayerColour playerColour = (CatanPlayerColour)(i + 1);
            var newPlayer = new CatanPlayer(playerColour);

            players.Add(newPlayer);
        }

        ShufflePlayers();
    }

    private void ShufflePlayers()
    {
        var totalPlayers = players.Count;

        while (totalPlayers > 1)
        {
            var playerIndexToSwapWith = random.Next(totalPlayers);
            totalPlayers--;
            (players[totalPlayers], players[playerIndexToSwapWith]) = (players[playerIndexToSwapWith], players[totalPlayers]);
        }
    }

    private void InitialiseDevelopmentCards()
    {
        remainingDevelopmentCardTotals.Clear();

        var totals = DomainConstants.GetDevelopmentCardTypeTotals();

        foreach (var type in totals.Keys)
        {
            remainingDevelopmentCardTotals.Add(type, totals[type]);
            for (var i = 0; i < totals[type]; i++)
            {
                remainingDevelopmentCards.Add(type);
            }
        }

        ShuffleDevelopmentCards();
    }

    private void ShuffleDevelopmentCards()
    {
        var totalDevelopmentCards = remainingDevelopmentCards.Count;

        while (totalDevelopmentCards > 1)
        {
            var cardIndexToSwapWith = random.Next(totalDevelopmentCards);
            totalDevelopmentCards--;
            (remainingDevelopmentCards[totalDevelopmentCards], remainingDevelopmentCards[cardIndexToSwapWith]) = (remainingDevelopmentCards[cardIndexToSwapWith], remainingDevelopmentCards[totalDevelopmentCards]);
        }
    }

    private CatanPlayer? GetPlayerByColour(CatanPlayerColour colour)
    {
        return players.FirstOrDefault(p => p.Colour == colour);
    }

    private void InitialiseResourceCards()
    {
        remainingResourceCards.Clear();

        var totals = DomainConstants.GetBankResourceTotals();

        foreach (var type in totals.Keys)
        {
            remainingResourceCards.Add(type, totals[type]);
        }
    }

    private void UpdateLargestArmyPlayer()
    {
        var player = players.OrderByDescending(p => p.KnightsPlayed).FirstOrDefault();

        if (player != null && LargestArmyPlayer != player && player.KnightsPlayed >= knightsRequiredForLargestArmy)
        {
            LargestArmyPlayer?.RemoveLargestArmyCard();

            player.AddLargestArmyCard();
            knightsRequiredForLargestArmy = player.KnightsPlayed + 1;
        }
    }

    private void UpdateLargestRoadPlayer()
    {
        var longestRoadInfo = Board.GetLongestRoadInfo();

        var player = players.FirstOrDefault(p => p.Colour == longestRoadInfo.Colour);

        if (LongestRoadPlayer != player)
        {
            LongestRoadPlayer?.RemoveLongestRoadCard();

            player?.AddLongestRoadCard();
        }
    }

    private bool CanPlayDevelopmentCard(CatanDevelopmentCardType type)
    {
        return !developmentCardPlayedThisTurn && CurrentPlayer.CanPlayDevelopmentCardOfType(type);
    }

    private void PlayDevelopmentCard(CatanDevelopmentCardType type)
    {
        if (!CanPlayDevelopmentCard(type))
        {
            throw new InvalidOperationException($"Cannot play development card of type: '{type}'");
        }

        developmentCardPlayedThisTurn = true;

        CurrentPlayer.PlayDevelopmentCard(type);

        remainingDevelopmentCardTotals[type]++;
        remainingDevelopmentCards.Add(type);
    }
}
