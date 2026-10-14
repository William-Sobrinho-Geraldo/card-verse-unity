namespace SuecaOnline.Domain.Sueca.Match
{
    /// <summary>Fases explicitas da maquina de estados da partida de Sueca.</summary>
    public enum GamePhase
    {
        WaitingForPlayers,
        DealingCards,
        AwaitingPlayerMove,
        ResolvingTrick,
        TrickCompleted,
        RoundCompleted,
        MatchCompleted
    }
}
