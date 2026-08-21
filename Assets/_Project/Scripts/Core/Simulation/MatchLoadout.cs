namespace PongRoyale.Core.Simulation
{
    /// <summary>
    /// As cartas de ataque que cada jogador trouxe, na ordem escolhida.
    ///
    /// A ordem E a atribuicao: a carta da ESQUERDA vai para a torre lateral esquerda do
    /// ADVERSARIO, a da direita para a direita dele. O jogador escolhe o que vai estar
    /// atras de cada torre inimiga; o adversario ve o deck antes da partida, mas nunca a
    /// atribuicao.
    ///
    /// A selecao propriamente dita e o passo 6. Aqui o que importa e que a simulacao receba
    /// isto pronto e imutavel no inicio da partida.
    /// </summary>
    public readonly struct MatchLoadout
    {
        public readonly ushort BottomLeftCard;
        public readonly ushort BottomRightCard;
        public readonly ushort TopLeftCard;
        public readonly ushort TopRightCard;

        public MatchLoadout(
            ushort bottomLeftCard, ushort bottomRightCard, ushort topLeftCard, ushort topRightCard)
        {
            BottomLeftCard = bottomLeftCard;
            BottomRightCard = bottomRightCard;
            TopLeftCard = topLeftCard;
            TopRightCard = topRightCard;
        }

        /// <summary>Partida sem cartas. Util para teste de fisica pura e para o bot.</summary>
        public static MatchLoadout Empty => new MatchLoadout(0, 0, 0, 0);

        /// <summary>Carta que o jogador colocou na torre esquerda do adversario.</summary>
        public ushort LeftCardOf(PlayerSlot chooser) =>
            chooser == PlayerSlot.Bottom ? BottomLeftCard : TopLeftCard;

        public ushort RightCardOf(PlayerSlot chooser) =>
            chooser == PlayerSlot.Bottom ? BottomRightCard : TopRightCard;
    }
}
