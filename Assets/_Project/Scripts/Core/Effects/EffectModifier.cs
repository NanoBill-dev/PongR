namespace PongRoyale.Core.Effects
{
    /// <summary>
    /// O que um power-up modifica. Cada alvo corresponde a um numero que ja existe no
    /// MatchConfig e que algum resolver ja consulta.
    ///
    /// Manter esta lista curta e deliberado: se uma carta precisa de um alvo novo, ela nao
    /// e um modificador — e uma carta com logica propria, e deve ser escrita como tal.
    /// </summary>
    public enum ModifierTarget : byte
    {
        None = 0,

        /// <summary>Velocidade da bola, enquanto a posse for do dono do efeito.</summary>
        BallSpeed = 1,

        /// <summary>Dano da bola, enquanto a posse for do dono do efeito.</summary>
        BallDamage = 2,

        /// <summary>Teto de velocidade da bola.</summary>
        BallMaxSpeed = 3,

        /// <summary>Fracao da velocidade da raquete transferida a bola.</summary>
        PaddleSweepCarry = 4,

        /// <summary>Velocidade maxima da raquete.</summary>
        PaddleMaxSpeed = 5,

        /// <summary>Largura da raquete.</summary>
        PaddleWidth = 6,

        /// <summary>Deflexao maxima a partir da normal da raquete.</summary>
        PaddleMaxDeflection = 7,

        /// <summary>Dano recebido pelas torres laterais.</summary>
        GuardTowerDamageTaken = 8,

        /// <summary>Dano recebido pela Torre Rei.</summary>
        KingTowerDamageTaken = 9,

        /// <summary>Meia-largura das torres laterais.</summary>
        GuardTowerHalfWidth = 10
    }

    /// <summary>
    /// A definicao de uma carta que e apenas um multiplicador. Dado puro: nao ha codigo por
    /// carta, so uma entrada nesta tabela.
    /// </summary>
    public readonly struct EffectModifier
    {
        public readonly ushort EffectId;
        public readonly ModifierTarget Target;

        /// <summary>Multiplicador aplicado sobre o valor base. 1 nao muda nada.</summary>
        public readonly float Multiplier;

        /// <summary>
        /// Se o alvo do modificador e o ADVERSARIO de quem coletou o power-up.
        ///
        /// Coice acelera a propria raquete: falso. Lodo desacelera a raquete do adversario:
        /// verdadeiro. Fundacao Rachada aumenta o dano recebido pelas torres DELE, entao
        /// tambem verdadeiro.
        /// </summary>
        public readonly bool TargetsOpponent;

        public EffectModifier(ushort effectId, ModifierTarget target, float multiplier, bool targetsOpponent)
        {
            EffectId = effectId;
            Target = target;
            Multiplier = multiplier;
            TargetsOpponent = targetsOpponent;
        }
    }
}
