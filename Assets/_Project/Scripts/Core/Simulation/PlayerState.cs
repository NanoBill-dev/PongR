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

        /// <summary>
        /// Interceptacao ainda disponivel. UMA por partida: gastou, o proximo drop do
        /// adversario passa livre. Isso obriga a decidir sob pressao, sem saber o que esta
        /// caindo, e impede que um veterano zere a economia ofensiva do outro lado.
        /// </summary>
        public bool HasInterceptionAvailable;

        /// <summary>Torres adversarias derrubadas. Alimenta missoes e estatisticas.</summary>
        public byte TowersDestroyed;

        public static PlayerState Create()
        {
            return new PlayerState
            {
                DefenseCharges = 0,
                CleanCycles = 0,
                ReceivesCharges = true,
                HasInterceptionAvailable = true,
                TowersDestroyed = 0
            };
        }
    }
}
