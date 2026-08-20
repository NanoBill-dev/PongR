namespace PongRoyale.Core.Simulation
{
    /// <summary>
    /// Identifica os dois lados da arena. Em retrato (ADR-003) o jogador local fica
    /// sempre embaixo e o adversario em cima — a camera espelha para o outro cliente,
    /// mas a simulacao usa sempre esta convencao absoluta.
    /// </summary>
    public enum PlayerSlot : byte
    {
        Bottom = 0,
        Top = 1
    }

    public static class PlayerSlotExtensions
    {
        /// <summary>Indice de array correspondente ao slot.</summary>
        public static int ToIndex(this PlayerSlot slot) => (int)slot;

        public static PlayerSlot Opponent(this PlayerSlot slot) =>
            slot == PlayerSlot.Bottom ? PlayerSlot.Top : PlayerSlot.Bottom;

        /// <summary>
        /// Sinal do eixo Y do lado: -1 embaixo, +1 em cima. Evita espalhar
        /// "if (slot == Bottom) -1 else 1" por toda a geometria.
        /// </summary>
        public static float DirectionSign(this PlayerSlot slot) =>
            slot == PlayerSlot.Bottom ? -1f : 1f;
    }
}
