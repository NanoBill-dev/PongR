using System.Numerics;
using PongRoyale.Core.Simulation;

namespace PongRoyale.Core.Events
{
    /// <summary>
    /// O que aconteceu durante um tick. A camada de apresentacao le esta lista para
    /// disparar VFX, SFX e feedback; a simulacao nunca chama a UI diretamente (ADR-001).
    /// </summary>
    public enum MatchEventType : byte
    {
        None = 0,
        BallSpawned = 1,
        BallHitWall = 2,
        BallHitPaddle = 3,
        BallHitObstacle = 4,
        TowerDamaged = 5,
        TowerDestroyed = 6,
        CardPlayed = 7,
        PhaseChanged = 8,
        MatchEnded = 9,
        EffectGained = 10,
        EffectExpired = 11
    }

    /// <summary>
    /// Evento de simulacao. Um unico struct compacto para todos os tipos: campos que
    /// nao se aplicam ficam zerados. Alternativa seria uma hierarquia de classes, que
    /// alocaria por evento e por tick — inaceitavel no orcamento mobile (secao 35).
    /// </summary>
    public readonly struct MatchEvent
    {
        public readonly MatchEventType Type;
        public readonly int Tick;

        /// <summary>Jogador envolvido. Quem rebateu, quem jogou a carta, quem venceu.</summary>
        public readonly PlayerSlot Slot;

        /// <summary>Indice da entidade afetada: bola, torre ou carta na mao.</summary>
        public readonly byte EntityIndex;

        /// <summary>Valor numerico do evento: dano causado, velocidade da rebatida, custo.</summary>
        public readonly float Value;

        /// <summary>Onde aconteceu, para posicionar o feedback visual.</summary>
        public readonly Vector2 Position;

        public MatchEvent(
            MatchEventType type,
            int tick,
            PlayerSlot slot,
            byte entityIndex,
            float value,
            Vector2 position)
        {
            Type = type;
            Tick = tick;
            Slot = slot;
            EntityIndex = entityIndex;
            Value = value;
            Position = position;
        }

        public static MatchEvent BallHitPaddle(int tick, PlayerSlot slot, byte ballIndex, float speed, Vector2 position) =>
            new MatchEvent(MatchEventType.BallHitPaddle, tick, slot, ballIndex, speed, position);

        public static MatchEvent BallHitWall(int tick, byte ballIndex, Vector2 position) =>
            new MatchEvent(MatchEventType.BallHitWall, tick, PlayerSlot.Bottom, ballIndex, 0f, position);

        public static MatchEvent TowerDamaged(int tick, PlayerSlot owner, byte towerIndex, float damage, Vector2 position) =>
            new MatchEvent(MatchEventType.TowerDamaged, tick, owner, towerIndex, damage, position);

        public static MatchEvent TowerDestroyed(int tick, PlayerSlot owner, byte towerIndex, Vector2 position) =>
            new MatchEvent(MatchEventType.TowerDestroyed, tick, owner, towerIndex, 0f, position);

        public static MatchEvent PhaseChanged(int tick, MatchPhase phase) =>
            new MatchEvent(MatchEventType.PhaseChanged, tick, PlayerSlot.Bottom, (byte)phase, 0f, Vector2.Zero);

        /// <summary>
        /// O identificador do efeito vai em <see cref="Value"/> e nao em EntityIndex, que e
        /// byte: ids de carta sao ushort para caber um repositorio grande.
        /// </summary>
        public static MatchEvent EffectGained(int tick, PlayerSlot slot, ushort effectId) =>
            new MatchEvent(MatchEventType.EffectGained, tick, slot, 0, effectId, Vector2.Zero);

        public static MatchEvent EffectExpired(int tick, PlayerSlot slot, ushort effectId) =>
            new MatchEvent(MatchEventType.EffectExpired, tick, slot, 0, effectId, Vector2.Zero);

        public static MatchEvent MatchEnded(int tick, MatchOutcome outcome) =>
            new MatchEvent(MatchEventType.MatchEnded, tick, PlayerSlot.Bottom, (byte)outcome, 0f, Vector2.Zero);
    }
}
