namespace PongRoyale.Core.Effects
{
    /// <summary>
    /// Um efeito de power-up em vigor para um jogador.
    ///
    /// Struct mutavel dentro de array, como o resto do estado de simulacao: o tempo restante
    /// e escrito no lugar a cada tick, sem alocacao.
    ///
    /// O identificador e um ushort ESTAVEL, e nao um nome ou uma referencia de objeto: e ele
    /// que vai trafegar na rede na FASE 3, e e ele que o BalanceData mapeia para a carta.
    /// </summary>
    public struct ActiveEffect
    {
        /// <summary>Identificador vazio. Marca um slot livre.</summary>
        public const ushort None = 0;

        public ushort EffectId;

        /// <summary>Tempo restante em segundos. Zero ou menos significa slot livre.</summary>
        public float RemainingSeconds;

        public bool IsActive => EffectId != None && RemainingSeconds > 0f;

        public static ActiveEffect Create(ushort effectId, float durationSeconds)
        {
            return new ActiveEffect
            {
                EffectId = effectId,
                RemainingSeconds = durationSeconds
            };
        }

        public static ActiveEffect Empty => new ActiveEffect
        {
            EffectId = None,
            RemainingSeconds = 0f
        };
    }
}
