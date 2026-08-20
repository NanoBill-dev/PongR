namespace PongRoyale.Core.Simulation
{
    /// <summary>
    /// Como decidir a partida quando o tempo acaba e nenhuma Torre Rei caiu.
    /// E dado, nao codigo: trocar a regra e trocar o valor no BalanceData.
    /// </summary>
    public enum TiebreakRule
    {
        /// <summary>Prorrogacao: a primeira torre destruida encerra a partida.</summary>
        SuddenDeath = 0,

        /// <summary>Vence quem destruiu mais torres; se empatar, empate real.</summary>
        MostTowersDestroyed = 1,

        /// <summary>Vence quem tem mais HP somado nas torres restantes.</summary>
        HighestRemainingHealth = 2
    }
}
