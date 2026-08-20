using System.Numerics;

namespace PongRoyale.Core.Simulation
{
    public enum CommandType : byte
    {
        None = 0,

        /// <summary>Pedido de movimento da raquete para uma posicao em X.</summary>
        PaddleMove = 1,

        /// <summary>Uso de uma carta da mao, com alvo no campo.</summary>
        PlayCard = 2,

        /// <summary>Emoji. Nao afeta a simulacao, mas trafega pelo mesmo canal.</summary>
        Emote = 3
    }

    /// <summary>
    /// Tudo que um jogador pode pedir a simulacao, em um unico tipo. E a fronteira de
    /// entrada do Core (ADR-001): input local, bot e rede produzem o mesmo comando, e a
    /// simulacao nao sabe distinguir a origem.
    ///
    /// Imutavel e compacto de proposito: e este struct que vai trafegar na rede.
    /// </summary>
    public readonly struct MatchCommand
    {
        public readonly int Tick;
        public readonly PlayerSlot Slot;
        public readonly CommandType Type;

        /// <summary>Alvo em X da raquete. Valido apenas para <see cref="CommandType.PaddleMove"/>.</summary>
        public readonly float PaddleTargetX;

        /// <summary>Posicao na mao, de 0 a HandSize-1. Valido para PlayCard.</summary>
        public readonly byte HandSlot;

        /// <summary>Onde a carta foi solta. Valido para PlayCard.</summary>
        public readonly Vector2 Target;

        /// <summary>Identificador do emoji. Valido para Emote.</summary>
        public readonly byte EmoteId;

        private MatchCommand(
            int tick,
            PlayerSlot slot,
            CommandType type,
            float paddleTargetX,
            byte handSlot,
            Vector2 target,
            byte emoteId)
        {
            Tick = tick;
            Slot = slot;
            Type = type;
            PaddleTargetX = paddleTargetX;
            HandSlot = handSlot;
            Target = target;
            EmoteId = emoteId;
        }

        public static MatchCommand PaddleMove(int tick, PlayerSlot slot, float targetX) =>
            new MatchCommand(tick, slot, CommandType.PaddleMove, targetX, 0, Vector2.Zero, 0);

        public static MatchCommand PlayCard(int tick, PlayerSlot slot, byte handSlot, Vector2 target) =>
            new MatchCommand(tick, slot, CommandType.PlayCard, 0f, handSlot, target, 0);

        public static MatchCommand Emote(int tick, PlayerSlot slot, byte emoteId) =>
            new MatchCommand(tick, slot, CommandType.Emote, 0f, 0, Vector2.Zero, emoteId);
    }
}
