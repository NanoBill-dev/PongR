namespace PongRoyale.Core.Simulation
{
    /// <summary>
    /// Estado por jogador que nao pertence a nenhuma entidade fisica.
    /// O deck e a mao entram aqui na FASE 2, quando o sistema de cartas existir.
    /// </summary>
    public struct PlayerState
    {
        /// <summary>Elixir disponivel. A simulacao garante que fica entre 0 e MaxElixir.</summary>
        public float Elixir;

        /// <summary>Torres adversarias derrubadas. Criterio de desempate MostTowersDestroyed.</summary>
        public byte TowersDestroyed;

        public static PlayerState Create(float startingElixir)
        {
            return new PlayerState
            {
                Elixir = startingElixir,
                TowersDestroyed = 0
            };
        }
    }
}
