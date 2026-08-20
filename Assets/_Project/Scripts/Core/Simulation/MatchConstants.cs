namespace PongRoyale.Core.Simulation
{
    /// <summary>
    /// Constantes estruturais da simulacao: definem COMO a partida roda.
    /// Nao confundir com balanceamento (velocidade, dano, custo de carta),
    /// que vive em BalanceData e pode ser ajustado sem recompilar.
    /// </summary>
    public static class MatchConstants
    {
        /// <summary>Passos de simulacao por segundo. Fixo e independente do frame rate.</summary>
        public const int SimulationTicksPerSecond = 60;

        /// <summary>Duracao de um tick de simulacao, em segundos.</summary>
        public const float FixedDeltaTime = 1f / SimulationTicksPerSecond;

        /// <summary>Snapshots de estado enviados pela autoridade por segundo.</summary>
        public const int SnapshotsPerSecond = 20;

        /// <summary>Comandos de input enviados pelo cliente por segundo.</summary>
        public const int CommandsPerSecond = 30;

        /// <summary>
        /// Teto de colisoes resolvidas dentro de um unico tick. Impede loop infinito
        /// quando a bola fica encurralada entre duas superficies.
        /// </summary>
        public const int MaxCollisionIterationsPerTick = 4;

        /// <summary>Jogadores numa partida 1v1.</summary>
        public const int PlayerCount = 2;

        /// <summary>Cartas no deck montado antes da partida.</summary>
        public const int DeckSize = 8;

        /// <summary>Cartas visiveis na mao durante a partida.</summary>
        public const int HandSize = 4;
    }
}
