namespace PongRoyale.Core.Simulation
{
    public enum MatchOutcome : byte
    {
        /// <summary>Ainda em disputa.</summary>
        Undecided = 0,
        Victory = 1,
        Draw = 2
    }

    /// <summary>Por que a partida terminou. Serve para telemetria e para a tela de fim.</summary>
    public enum MatchEndReason : byte
    {
        None = 0,
        KingTowerDestroyed = 1,
        TimeExpired = 2,
        TiebreakResolved = 3,
        OpponentForfeited = 4
    }

    /// <summary>
    /// Resultado final. Imutavel: uma vez decidido, nada na simulacao o altera.
    /// </summary>
    public readonly struct MatchResult
    {
        /// <summary>Valor usado quando nao ha vencedor (indefinido ou empate).</summary>
        public const sbyte NoWinner = -1;

        public readonly MatchOutcome Outcome;
        public readonly sbyte WinnerSlot;
        public readonly MatchEndReason Reason;

        private MatchResult(MatchOutcome outcome, sbyte winnerSlot, MatchEndReason reason)
        {
            Outcome = outcome;
            WinnerSlot = winnerSlot;
            Reason = reason;
        }

        public static MatchResult Undecided => new MatchResult(MatchOutcome.Undecided, NoWinner, MatchEndReason.None);

        public static MatchResult Victory(PlayerSlot winner, MatchEndReason reason) =>
            new MatchResult(MatchOutcome.Victory, (sbyte)winner, reason);

        public static MatchResult Draw(MatchEndReason reason) =>
            new MatchResult(MatchOutcome.Draw, NoWinner, reason);

        public bool IsDecided => Outcome != MatchOutcome.Undecided;
    }
}
