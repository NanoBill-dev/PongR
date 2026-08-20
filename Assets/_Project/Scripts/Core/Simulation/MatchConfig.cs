using System.Numerics;

namespace PongRoyale.Core.Simulation
{
    /// <summary>
    /// Dimensoes do campo. Origem no centro da arena; X e o eixo de movimento das
    /// raquetes, Y e o eixo que a bola percorre entre os dois lados (arena em retrato).
    /// </summary>
    public readonly struct ArenaConfig
    {
        public readonly float Width;
        public readonly float Height;

        /// <summary>Distancia da linha da raquete ate a borda do seu lado.</summary>
        public readonly float PaddleLineOffsetFromEdge;

        public ArenaConfig(float width, float height, float paddleLineOffsetFromEdge)
        {
            Width = width;
            Height = height;
            PaddleLineOffsetFromEdge = paddleLineOffsetFromEdge;
        }

        public float HalfWidth => Width * 0.5f;

        public float HalfHeight => Height * 0.5f;
    }

    /// <summary>
    /// Comportamento da bola. Ver ADR-002: a fisica e custom, nao Rigidbody2D.
    /// </summary>
    public readonly struct BallConfig
    {
        public readonly float InitialSpeed;
        public readonly float MaxSpeed;

        /// <summary>Ganho de velocidade por rebatida. 0.02 significa mais 2 por cento.</summary>
        public readonly float SpeedGainPerHit;

        public readonly float Radius;
        public readonly float BaseDamage;

        /// <summary>
        /// Deflexao maxima em relacao a normal da raquete, aplicada conforme o offset
        /// do impacto. E daqui que sai a skill do jogo: acertar de raspao muda o angulo.
        /// </summary>
        public readonly float MaxDeflectionFromNormalDegrees;

        /// <summary>
        /// Angulo minimo entre a direcao da bola e a horizontal. Impede a bola de entrar
        /// num vai-e-vem quase horizontal que nunca chega a uma raquete.
        /// </summary>
        public readonly float MinAngleFromHorizontalDegrees;

        public BallConfig(
            float initialSpeed,
            float maxSpeed,
            float speedGainPerHit,
            float radius,
            float baseDamage,
            float maxDeflectionFromNormalDegrees,
            float minAngleFromHorizontalDegrees)
        {
            InitialSpeed = initialSpeed;
            MaxSpeed = maxSpeed;
            SpeedGainPerHit = speedGainPerHit;
            Radius = radius;
            BaseDamage = baseDamage;
            MaxDeflectionFromNormalDegrees = maxDeflectionFromNormalDegrees;
            MinAngleFromHorizontalDegrees = minAngleFromHorizontalDegrees;
        }
    }

    /// <summary>
    /// Raquete do jogador. Move apenas no eixo X, porque a arena e em retrato (ADR-003).
    /// </summary>
    public readonly struct PaddleConfig
    {
        public readonly float Width;
        public readonly float Thickness;
        public readonly float MaxSpeed;

        /// <summary>Tempo de suavizacao ate alcancar a posicao alvo do dedo, em segundos.</summary>
        public readonly float SmoothingTime;

        /// <summary>Multiplicador do arraste do dedo em unidades de mundo.</summary>
        public readonly float DragSensitivity;

        public PaddleConfig(
            float width,
            float thickness,
            float maxSpeed,
            float smoothingTime,
            float dragSensitivity)
        {
            Width = width;
            Thickness = thickness;
            MaxSpeed = maxSpeed;
            SmoothingTime = smoothingTime;
            DragSensitivity = dragSensitivity;
        }

        public float HalfWidth => Width * 0.5f;
    }

    /// <summary>Torres de cada lado: Rei ao centro e duas laterais nos cantos.</summary>
    public readonly struct TowerConfig
    {
        public readonly float KingMaxHealth;
        public readonly float GuardMaxHealth;

        /// <summary>Distancia em X do centro ate cada torre lateral.</summary>
        public readonly float GuardOffsetFromCenter;

        /// <summary>Distancia da borda da arena ate a linha das torres.</summary>
        public readonly float RowOffsetFromEdge;

        /// <summary>Meias-extensoes da Torre Rei, usadas na colisao da bola.</summary>
        public readonly Vector2 KingHalfSize;

        /// <summary>Meias-extensoes das torres laterais.</summary>
        public readonly Vector2 GuardHalfSize;

        public TowerConfig(
            float kingMaxHealth,
            float guardMaxHealth,
            float guardOffsetFromCenter,
            float rowOffsetFromEdge,
            Vector2 kingHalfSize,
            Vector2 guardHalfSize)
        {
            KingMaxHealth = kingMaxHealth;
            GuardMaxHealth = guardMaxHealth;
            GuardOffsetFromCenter = guardOffsetFromCenter;
            RowOffsetFromEdge = rowOffsetFromEdge;
            KingHalfSize = kingHalfSize;
            GuardHalfSize = guardHalfSize;
        }
    }

    /// <summary>
    /// Economia. A simulacao nunca permite elixir negativo nem acima do maximo.
    /// </summary>
    public readonly struct ElixirConfig
    {
        public readonly float MaxElixir;
        public readonly float StartingElixir;

        /// <summary>Segundos por ponto de elixir no ritmo normal.</summary>
        public readonly float SecondsPerElixir;

        /// <summary>Segundos por ponto de elixir durante o modo duplo.</summary>
        public readonly float SecondsPerElixirInDoubleMode;

        public ElixirConfig(
            float maxElixir,
            float startingElixir,
            float secondsPerElixir,
            float secondsPerElixirInDoubleMode)
        {
            MaxElixir = maxElixir;
            StartingElixir = startingElixir;
            SecondsPerElixir = secondsPerElixir;
            SecondsPerElixirInDoubleMode = secondsPerElixirInDoubleMode;
        }
    }

    /// <summary>Duracao e condicoes de encerramento da partida.</summary>
    public readonly struct MatchRulesConfig
    {
        public readonly float MatchDurationSeconds;

        /// <summary>Quantos segundos finais rodam com elixir em ritmo dobrado.</summary>
        public readonly float DoubleElixirLastSeconds;

        public readonly float OvertimeDurationSeconds;
        public readonly TiebreakRule Tiebreak;

        public MatchRulesConfig(
            float matchDurationSeconds,
            float doubleElixirLastSeconds,
            float overtimeDurationSeconds,
            TiebreakRule tiebreak)
        {
            MatchDurationSeconds = matchDurationSeconds;
            DoubleElixirLastSeconds = doubleElixirLastSeconds;
            OvertimeDurationSeconds = overtimeDurationSeconds;
            Tiebreak = tiebreak;
        }

        /// <summary>Instante, em segundos desde o inicio, em que o elixir dobra o ritmo.</summary>
        public float DoubleElixirStartTime => MatchDurationSeconds - DoubleElixirLastSeconds;
    }

    /// <summary>Trofeus ganhos ou perdidos ao fim da partida.</summary>
    public readonly struct TrophyConfig
    {
        public readonly int OnWin;
        public readonly int OnLoss;
        public readonly int OnDraw;

        public TrophyConfig(int onWin, int onLoss, int onDraw)
        {
            OnWin = onWin;
            OnLoss = onLoss;
            OnDraw = onDraw;
        }
    }

    /// <summary>
    /// Pacote imutavel com todo o balanceamento de uma partida. Montado uma unica vez
    /// no inicio, a partir do BalanceData, e repassado a simulacao. Ser imutavel garante
    /// que nada altere as regras no meio da partida, inclusive um cliente malicioso.
    /// </summary>
    public sealed class MatchConfig
    {
        public readonly ArenaConfig Arena;
        public readonly BallConfig Ball;
        public readonly PaddleConfig Paddle;
        public readonly TowerConfig Tower;
        public readonly ElixirConfig Elixir;
        public readonly MatchRulesConfig Rules;
        public readonly TrophyConfig Trophies;

        public MatchConfig(
            ArenaConfig arena,
            BallConfig ball,
            PaddleConfig paddle,
            TowerConfig tower,
            ElixirConfig elixir,
            MatchRulesConfig rules,
            TrophyConfig trophies)
        {
            Arena = arena;
            Ball = ball;
            Paddle = paddle;
            Tower = tower;
            Elixir = elixir;
            Rules = rules;
            Trophies = trophies;
        }
    }
}
