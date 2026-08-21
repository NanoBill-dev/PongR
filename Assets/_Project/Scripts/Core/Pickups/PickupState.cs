using System.Numerics;
using PongRoyale.Core.Simulation;

namespace PongRoyale.Core.Pickups
{
    /// <summary>
    /// Um power-up caindo pela arena, esperando ser coletado.
    ///
    /// Struct mutavel dentro de array, como o resto do estado de simulacao.
    /// </summary>
    public struct PickupState
    {
        public Vector2 Position;

        /// <summary>Carta que este drop entrega.</summary>
        public ushort EffectId;

        /// <summary>
        /// Quem escolheu a carta e para quem ela cai. E o dono da TORRE DESTRUIDA vista do
        /// outro lado: quem derruba a torre lateral inimiga recebe o proprio power-up.
        /// </summary>
        public byte CollectorSlot;

        /// <summary>
        /// Se o adversario pode roubar este drop no caminho. O drop de REDENCAO nao pode:
        /// ele foi comprado com uma sequencia inteira de defesa impecavel e ja custou as
        /// tres cargas, entao deixar rouba-lo puniria duas vezes o mesmo jogador.
        /// </summary>
        public bool CanBeIntercepted;

        public bool IsActive;

        public PlayerSlot Collector => (PlayerSlot)CollectorSlot;

        public static PickupState Create(
            Vector2 position, ushort effectId, PlayerSlot collector, bool canBeIntercepted)
        {
            return new PickupState
            {
                Position = position,
                EffectId = effectId,
                CollectorSlot = (byte)collector,
                CanBeIntercepted = canBeIntercepted,
                IsActive = true
            };
        }

        public static PickupState Empty => new PickupState
        {
            Position = Vector2.Zero,
            EffectId = 0,
            CollectorSlot = 0,
            CanBeIntercepted = true,
            IsActive = false
        };
    }
}
