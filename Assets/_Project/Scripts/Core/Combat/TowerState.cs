using System.Numerics;

namespace PongRoyale.Core.Combat
{
    /// <summary>
    /// As tres torres de cada lado. A ordem dos valores e usada como indice dentro do
    /// bloco de torres do jogador, entao nao reordenar sem ajustar MatchState.TowerIndex.
    /// </summary>
    public enum TowerKind : byte
    {
        King = 0,
        LeftGuard = 1,
        RightGuard = 2
    }

    /// <summary>Estado de uma torre. Struct mutavel dentro de array, escrita no lugar.</summary>
    public struct TowerState
    {
        public Vector2 Position;
        public float Health;
        public float MaxHealth;

        /// <summary>Meia-largura usada na colisao da bola contra a torre.</summary>
        public float HalfWidth;

        /// <summary>Meia-altura usada na colisao da bola contra a torre.</summary>
        public float HalfHeight;

        public TowerKind Kind;
        public byte OwnerSlot;

        /// <summary>
        /// Carta que cai quando esta torre e destruida. Foi escolhida pelo ADVERSARIO do
        /// dono da torre, e cai em direcao a ele. Zero nas Torres Rei e quando nao ha deck.
        /// </summary>
        public ushort RewardEffectId;

        public bool IsAlive => Health > 0f;

        public bool IsKing => Kind == TowerKind.King;

        /// <summary>Fracao de vida restante, de 0 a 1. Alimenta a barra de HP na UI.</summary>
        public float HealthFraction => MaxHealth > 0f ? Health / MaxHealth : 0f;

        public static TowerState Create(
            Vector2 position,
            float maxHealth,
            float halfWidth,
            float halfHeight,
            TowerKind kind,
            byte ownerSlot)
        {
            return new TowerState
            {
                Position = position,
                Health = maxHealth,
                MaxHealth = maxHealth,
                HalfWidth = halfWidth,
                HalfHeight = halfHeight,
                Kind = kind,
                OwnerSlot = ownerSlot
            };
        }
    }
}
