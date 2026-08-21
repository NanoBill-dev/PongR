using System.Numerics;
using PongRoyale.Core.Simulation;

namespace PongRoyale.Core.Ball
{
    /// <summary>
    /// Estado de uma bola. Struct MUTAVEL de proposito: vive dentro de um array em
    /// <see cref="MatchState"/> e e escrita no lugar (state.Balls[i].Position = ...),
    /// sem alocacao por tick. Nunca guardar bolas em List&lt;T&gt;: o indexador de List
    /// devolve uma copia e a escrita seria silenciosamente perdida.
    /// </summary>
    public struct BallState
    {
        /// <summary>Valor de <see cref="LastHitByPlayer"/> quando ninguem rebateu ainda.</summary>
        public const sbyte NoPlayer = -1;

        public Vector2 Position;

        /// <summary>Direcao normalizada. Velocidade mora separada para os modificadores agirem.</summary>
        public Vector2 Direction;

        /// <summary>Velocidade acumulada pelas rebatidas, antes dos modificadores.</summary>
        public float BaseSpeed;

        /// <summary>
        /// Produto dos modificadores ativos (Congelamento 0.4, Turbina 1.4, ...).
        /// Manter separado de <see cref="BaseSpeed"/> permite um efeito expirar sem
        /// desfazer o ganho legitimo das rebatidas.
        /// </summary>
        public float SpeedMultiplier;

        public float Damage;

        /// <summary>
        /// Incrementa a cada colisao. E o que permite ao cliente extrapolar a bola com
        /// seguranca e detectar que precisa corrigir (ADR-004).
        /// </summary>
        public ushort CollisionSequence;

        /// <summary>Quem rebateu por ultimo. Define a quem creditar o dano nas torres.</summary>
        public sbyte LastHitByPlayer;

        /// <summary>
        /// Acertos em torre desde a ultima vez que a bola voltou ao campo. Cada acerto
        /// consecutivo causa menos dano: e o que impede uma bola pinballando atras da
        /// raquete de derreter uma torre em segundos.
        /// </summary>
        public byte ConsecutiveTowerHits;

        public bool IsActive;

        public float CurrentSpeed => BaseSpeed * SpeedMultiplier;

        public Vector2 Velocity => Direction * CurrentSpeed;

        public static BallState Create(Vector2 position, Vector2 direction, float speed, float damage)
        {
            return new BallState
            {
                Position = position,
                Direction = Vector2.Normalize(direction),
                BaseSpeed = speed,
                SpeedMultiplier = 1f,
                Damage = damage,
                CollisionSequence = 0,
                LastHitByPlayer = NoPlayer,
                ConsecutiveTowerHits = 0,
                IsActive = true
            };
        }
    }
}
