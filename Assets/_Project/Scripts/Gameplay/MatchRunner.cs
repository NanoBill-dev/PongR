using PongRoyale.Core.Simulation;
using PongRoyale.Gameplay.Balance;
using UnityEngine;

namespace PongRoyale.Gameplay
{
    /// <summary>
    /// Liga a simulacao ao tempo do Unity. E a unica ponte entre o loop do jogo e o Core.
    ///
    /// A simulacao roda em passo fixo (ADR-008) e o frame rate do aparelho varia, entao o
    /// tempo real vai para um acumulador e o Tick e chamado quantas vezes couber. Num
    /// celular a 30 fps isso significa dois ticks por frame; a 120 fps, um tick a cada dois
    /// frames. A partida corre igual nos dois casos.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchRunner : MonoBehaviour
    {
        [Header("Configuracao")]
        [SerializeField] private BalanceData balanceData;
        [SerializeField] private PlayerSlot firstServeToward = PlayerSlot.Bottom;
        [SerializeField] private bool beginOnStart = true;

        [Header("Deck (provisorio ate a selecao do passo 6)")]
        [Tooltip("Carta na torre lateral ESQUERDA do adversario. Zero desliga o drop.")]
        [SerializeField, Min(0)] private int bottomLeftCard = 1;

        [Tooltip("Carta na torre lateral DIREITA do adversario.")]
        [SerializeField, Min(0)] private int bottomRightCard = 3;

        [SerializeField, Min(0)] private int topLeftCard = 4;
        [SerializeField, Min(0)] private int topRightCard = 5;

        [Header("Loop")]
        [Tooltip("Teto de ticks por frame. Impede a espiral da morte quando um travamento " +
                 "acumula tempo demais e cada frame passa a simular mais do que consegue.")]
        [SerializeField, Min(1)] private int maxTicksPerFrame = 5;

        private float accumulator;

        public MatchSimulation Simulation { get; private set; }

        public MatchCommandBuffer Commands { get; private set; }

        public MatchConfig Config { get; private set; }

        public bool IsReady => Simulation != null;

        private void Awake()
        {
            if (balanceData == null)
            {
                Debug.LogError($"[{nameof(MatchRunner)}] Sem BalanceData atribuido. A partida nao pode comecar.", this);
                enabled = false;
                return;
            }

            Config = balanceData.ToMatchConfig();

            var loadout = new MatchLoadout(
                (ushort)bottomLeftCard,
                (ushort)bottomRightCard,
                (ushort)topLeftCard,
                (ushort)topRightCard);

            Simulation = new MatchSimulation(Config, firstServeToward, loadout);
            Commands = new MatchCommandBuffer();
        }

        private void Start()
        {
            if (beginOnStart)
            {
                Simulation.Begin();
            }
        }

        private void Update()
        {
            if (!IsReady)
            {
                return;
            }

            // Limpa os eventos do frame ANTERIOR, ja consumidos pelas views no LateUpdate.
            // Limpar aqui, e nao dentro do Tick, e o que permite um frame com varios ticks
            // entregar todos os eventos juntos (ADR-008).
            Simulation.Events.Clear();

            accumulator += Time.deltaTime;

            int ticksThisFrame = 0;
            while (accumulator >= MatchConstants.FixedDeltaTime && ticksThisFrame < maxTicksPerFrame)
            {
                Simulation.Tick(Commands);
                Commands.Clear();

                accumulator -= MatchConstants.FixedDeltaTime;
                ticksThisFrame++;
            }

            if (ticksThisFrame >= maxTicksPerFrame)
            {
                // Estouro do orcamento: descarta o atraso em vez de tentar recuperar. Uma
                // partida que engasga e melhor que uma que trava tentando alcancar o relogio.
                accumulator = 0f;
            }
        }
    }
}
