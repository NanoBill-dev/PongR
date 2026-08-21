namespace PongRoyale.Core.Simulation
{
    /// <summary>
    /// Estado por jogador que nao pertence a nenhuma entidade fisica.
    /// </summary>
    public struct PlayerState
    {
        /// <summary>
        /// Cargas de defesa acumuladas. Cada uma anula UM acerto em qualquer torre e some.
        /// </summary>
        public byte DefenseCharges;

        /// <summary>
        /// Batidas do ciclo recebidas sem gastar nenhuma carga. ZERA a cada carga gasta.
        ///
        /// E o contador que a redencao exige, e ele e separado das cargas de proposito:
        /// recuperar uma carga rapido devolve o escudo, nao o progresso. Do contrario tomar
        /// acerto na hora certa sairia quase de graca.
        /// </summary>
        public byte CleanCycles;

        /// <summary>
        /// Se o jogador ainda recebe cargas nas batidas do metronomo. Fica falso ao entrar
        /// em modo berserk, depois da redencao: a barra continua girando para os dois, mas
        /// ele deixa de ser abastecido.
        /// </summary>
        public bool ReceivesCharges;

        /// <summary>Torres adversarias derrubadas. Alimenta missoes e estatisticas.</summary>
        public byte TowersDestroyed;

        public static PlayerState Create()
        {
            return new PlayerState
            {
                DefenseCharges = 0,
                CleanCycles = 0,
                ReceivesCharges = true,
                TowersDestroyed = 0
            };
        }
    }
}
