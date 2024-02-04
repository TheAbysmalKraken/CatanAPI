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
    private int knightsRequiredForLargestArmy;
    private bool developmentCardPlayedThisTurn;

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
    }

    public CatanBoard Board { get; private set; } = new();

    public CatanPlayer? LongestRoadPlayer { get; set; } = null;

    public CatanPlayer? LargestArmyPlayer => players.FirstOrDefault(p => p.HasLargestArmy);

    public int DiceTotal => rolledDice.Sum();

    public CatanPlayer CurrentPlayer => players.First();

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

        var currentPlayer = CurrentPlayer;

        currentPlayer.MoveOnHoldDevelopmentCardsToPlayable();

        players.Remove(currentPlayer);
        players.Add(currentPlayer);
    }

    public void RollDice()
    {
        rolledDice.Clear();
        rolledDice.AddRange(DiceRoller.RollDice(2, 6));
    }

    public bool PlayDevelopmentCard(CatanDevelopmentCardType type)
    {
        if (developmentCardPlayedThisTurn || !CurrentPlayer.CanPlayDevelopmentCardOfType(type))
        {
            return false;
        }

        developmentCardPlayedThisTurn = true;

        switch (type)
        {
            case CatanDevelopmentCardType.Knight:
                PlayKnightCard(CurrentPlayer);
                break;

            default:
                throw new ArgumentException($"Invalid development card type: '{type}'");
        }

        CurrentPlayer.PlayDevelopmentCard(type);

        remainingDevelopmentCardTotals[type]++;
        remainingDevelopmentCards.Add(type);

        return true;
    }

    public bool BuildInitialRoad(Coordinates coordinates1, Coordinates coordinates2)
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

    public bool BuildInitialSettlement(Coordinates coordinates)
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
        var random = new Random();
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

    private void PlayKnightCard(CatanPlayer player)
    {

    }
}
