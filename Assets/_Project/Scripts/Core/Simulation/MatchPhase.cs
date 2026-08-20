namespace PongRoyale.Core.Simulation
{
    /// <summary>Etapas pelas quais uma partida passa, do saque ao resultado.</summary>
    public enum MatchPhase : byte
    {
        /// <summary>Contagem regressiva antes do primeiro saque. Nada se move.</summary>
        WarmUp = 0,

        /// <summary>Partida em andamento.</summary>
        Playing = 1,

        /// <summary>Prorrogacao apos o tempo regulamentar sem Torre Rei destruida.</summary>
        Overtime = 2,

        /// <summary>Resultado definido. A simulacao nao avanca mais.</summary>
        Finished = 3
    }
}
