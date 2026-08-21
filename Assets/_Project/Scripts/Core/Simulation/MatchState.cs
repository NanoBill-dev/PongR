using PongRoyale.Core.Ball;
using PongRoyale.Core.Combat;
using PongRoyale.Core.Effects;
using PongRoyale.Core.Paddle;

namespace PongRoyale.Core.Simulation
{
    /// <summary>
    /// Todo o estado mutavel de uma partida, em um lugar so.
    ///
    /// Campos publicos e arrays de tamanho fixo sao deliberados, nao descuido: isto e
    /// dado de simulacao, nao um objeto de dominio. O comportamento mora nos resolvers,
    /// que recebem este estado e o avancam. Propriedades com backing field privado
    /// custariam chamadas por tick sem proteger nada — quem escreve aqui e sempre o Core.
    ///
    /// Arrays sao alocados uma unica vez na construcao. Depois disso um tick nao aloca.
    /// </summary>
    public sealed class MatchState
    {
        /// <summary>
        /// Teto rigido de bolas simultaneas (risco R4). Multibola e cartas empilhadas nunca
        /// passam disso, entao o custo por tick tem limite conhecido em qualquer aparelho.
        /// </summary>
        public const int MaxBalls = 8;

        public const int TowersPerPlayer = 3;
        public const int TotalTowers = MatchConstants.PlayerCount * TowersPerPlayer;

        /// <summary>
        /// Efeitos simultaneos por jogador. Dois bastariam para as duas torres laterais; o
        /// teto e 4 para caber a redencao e qualquer fonte futura sem realocar nada.
        /// </summary>
        public const int MaxEffectsPerPlayer = 4;

        public readonly MatchConfig Config;

        public readonly BallState[] Balls = new BallState[MaxBalls];
        public readonly PaddleState[] Paddles = new PaddleState[MatchConstants.PlayerCount];
        public readonly TowerState[] Towers = new TowerState[TotalTowers];
        public readonly PlayerState[] Players = new PlayerState[MatchConstants.PlayerCount];

        /// <summary>
        /// Efeitos em vigor, num bloco contiguo por jogador — mesma convencao das torres.
        /// </summary>
        public readonly ActiveEffect[] Effects =
            new ActiveEffect[MatchConstants.PlayerCount * MaxEffectsPerPlayer];

        public int Tick;
        public float ElapsedSeconds;
        public MatchPhase Phase;
        public MatchResult Result;

        public MatchState(MatchConfig config)
        {
            Config = config;
            Tick = 0;
            ElapsedSeconds = 0f;
            Phase = MatchPhase.WarmUp;
            Result = MatchResult.Undecided;
        }

        /// <summary>
        /// Indice da torre dentro do array unico. As torres de um jogador ocupam um bloco
        /// contiguo, entao a ordem de <see cref="TowerKind"/> nao pode mudar.
        /// </summary>
        public static int TowerIndex(PlayerSlot slot, TowerKind kind) =>
            slot.ToIndex() * TowersPerPlayer + (int)kind;

        /// <summary>
        /// Devolve a torre por referencia, permitindo escrever direto no array
        /// (GetTower(...).Health -= dano) sem copiar o struct.
        /// </summary>
        public ref TowerState GetTower(PlayerSlot slot, TowerKind kind) => ref Towers[TowerIndex(slot, kind)];

        public ref PaddleState GetPaddle(PlayerSlot slot) => ref Paddles[slot.ToIndex()];

        public ref PlayerState GetPlayer(PlayerSlot slot) => ref Players[slot.ToIndex()];

        public bool IsKingAlive(PlayerSlot slot) => GetTower(slot, TowerKind.King).IsAlive;

        /// <summary>
        /// Torres do jogador ainda de pe. Primeiro criterio de desempate.
        ///
        /// Conta as PROPRIAS torres vivas, e nao "torres que este jogador destruiu": se a
        /// bola de um jogador derruba a torre dele mesmo, o contador de destruicoes credita
        /// o adversario e daria um desempate errado. O que esta de pe nao mente.
        /// </summary>
        public int CountAliveTowers(PlayerSlot slot)
        {
            int alive = 0;
            int first = slot.ToIndex() * TowersPerPlayer;

            for (int i = first; i < first + TowersPerPlayer; i++)
            {
                if (Towers[i].IsAlive)
                {
                    alive++;
                }
            }

            return alive;
        }

        /// <summary>Vida somada das torres do jogador. Segundo criterio de desempate.</summary>
        public float TotalTowerHealth(PlayerSlot slot)
        {
            float total = 0f;
            int first = slot.ToIndex() * TowersPerPlayer;

            for (int i = first; i < first + TowersPerPlayer; i++)
            {
                total += Towers[i].Health;
            }

            return total;
        }

        public int CountActiveBalls()
        {
            int active = 0;
            for (int i = 0; i < Balls.Length; i++)
            {
                if (Balls[i].IsActive)
                {
                    active++;
                }
            }

            return active;
        }

        /// <summary>
        /// Primeiro indice livre no array de bolas, ou -1 se o teto foi atingido.
        /// Quem spawna bola sempre checa: estourar o teto e falha de design, nao excecao.
        /// </summary>
        public int FindFreeBallSlot()
        {
            for (int i = 0; i < Balls.Length; i++)
            {
                if (!Balls[i].IsActive)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
