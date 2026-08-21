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

        /// <summary>
        /// Quanto do dano sobrevive a cada acerto consecutivo em torre sem a bola voltar ao
        /// campo. 0.65 significa que o segundo acerto causa 65% do primeiro, o terceiro 65%
        /// do segundo, e assim por diante.
        ///
        /// Existe porque uma bola pinballando atras da raquete derrubava uma torre em
        /// segundos por um unico erro do jogador. O PRIMEIRO acerto continua valendo cheio,
        /// entao uma bola bem colocada pelo vao entre as torres e recompensada por inteiro —
        /// o que decai e o acidente, nao a jogada.
        /// </summary>
        public readonly float TowerDamageDecay;

        /// <summary>
        /// Piso do decaimento, como fracao do dano base. Nao pode ser zero: se o dano
        /// zerasse, atras da propria raquete viraria o lugar mais seguro do jogo e quem
        /// estivesse na frente no desempate poderia estagnar a partida de proposito.
        /// </summary>
        public readonly float TowerDamageFloor;

        public BallConfig(
            float initialSpeed,
            float maxSpeed,
            float speedGainPerHit,
            float radius,
            float baseDamage,
            float maxDeflectionFromNormalDegrees,
            float minAngleFromHorizontalDegrees,
            float towerDamageDecay,
            float towerDamageFloor)
        {
            InitialSpeed = initialSpeed;
            MaxSpeed = maxSpeed;
            SpeedGainPerHit = speedGainPerHit;
            Radius = radius;
            BaseDamage = baseDamage;
            MaxDeflectionFromNormalDegrees = maxDeflectionFromNormalDegrees;
            MinAngleFromHorizontalDegrees = minAngleFromHorizontalDegrees;
            TowerDamageDecay = towerDamageDecay;
            TowerDamageFloor = towerDamageFloor;
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

        /// <summary>
        /// Fracao da velocidade da raquete transferida para a bola no impacto.
        ///
        /// Zero devolve o Pong classico, onde so o ponto do impacto define o angulo. Acima
        /// de zero, varrer a raquete no momento da rebatida empurra a bola para o lado —
        /// permite atacar em vez de so devolver, e e a ferramenta para expulsar a bola que
        /// entrou atras da raquete.
        /// </summary>
        public readonly float SweepCarry;

        public PaddleConfig(
            float width,
            float thickness,
            float maxSpeed,
            float smoothingTime,
            float dragSensitivity,
            float sweepCarry)
        {
            Width = width;
            Thickness = thickness;
            MaxSpeed = maxSpeed;
            SmoothingTime = smoothingTime;
            DragSensitivity = dragSensitivity;
            SweepCarry = sweepCarry;
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
    /// O ciclo de elixir. Nao e moeda: e um METRONOMO GLOBAL, um relogio unico que comeca
    /// junto e bate igual para os dois jogadores. O que difere e quem RECEBE a carga na
    /// batida — quem esta em modo berserk simplesmente nao recebe.
    ///
    /// Consequencia deliberada: perder uma carga faltando pouco para a batida devolve ela
    /// quase de graca; perder logo depois deixa o jogador exposto por um ciclo inteiro. E
    /// sorte real e nao injusta, porque ninguem escolhe quando toma o acerto.
    /// </summary>
    public readonly struct ElixirConfig
    {
        /// <summary>Duracao de um ciclo completo da barra.</summary>
        public readonly float CycleSeconds;

        /// <summary>Teto de cargas de defesa acumuladas.</summary>
        public readonly int MaxDefenseCharges;

        /// <summary>
        /// Ciclos consecutivos SEM gastar carga exigidos para a redencao. Gastar zera a
        /// contagem, entao o requisito equivale a atravessar varios ciclos inteiros sem
        /// tomar um acerto sequer.
        /// </summary>
        public readonly int CleanCyclesForRedemption;

        public ElixirConfig(float cycleSeconds, int maxDefenseCharges, int cleanCyclesForRedemption)
        {
            CycleSeconds = cycleSeconds;
            MaxDefenseCharges = maxDefenseCharges;
            CleanCyclesForRedemption = cleanCyclesForRedemption;
        }
    }

    /// <summary>Duracao e condicoes de encerramento da partida.</summary>
    public readonly struct MatchRulesConfig
    {
        public readonly float MatchDurationSeconds;

        /// <summary>Quantos segundos finais rodam com elixir em ritmo dobrado.</summary>
        public readonly float FinalStretchSeconds;

        public MatchRulesConfig(float matchDurationSeconds, float finalStretchSeconds)
        {
            MatchDurationSeconds = matchDurationSeconds;
            FinalStretchSeconds = finalStretchSeconds;
        }

        /// <summary>Instante, em segundos desde o inicio, em que o elixir dobra o ritmo.</summary>
        public float FinalStretchStartTime => MatchDurationSeconds - FinalStretchSeconds;
    }

    /// <summary>Duracao dos efeitos de power-up.</summary>
    public readonly struct EffectConfig
    {
        /// <summary>Duracao de um power-up coletado sozinho.</summary>
        public readonly float DefaultDurationSeconds;

        /// <summary>
        /// Duracao de TODOS os efeitos quando dois ou mais estao em vigor ao mesmo tempo.
        /// Substitui o tempo restante, para mais ou para menos: combinar e sempre uma janela
        /// curta e fixa. E o que transforma a segunda coleta numa decisao de ritmo.
        /// </summary>
        public readonly float CombinedDurationSeconds;

        /// <summary>
        /// Tabela das cartas que sao apenas multiplicadores. Cada entrada e uma carta
        /// inteira, sem codigo dedicado.
        ///
        /// E um array por ser lido a cada consulta e nunca escrito. Tratar como imutavel:
        /// alterar o conteudo depois de montar o MatchConfig quebraria a garantia de que a
        /// regra nao muda no meio da partida.
        /// </summary>
        public readonly Effects.EffectModifier[] Modifiers;

        public EffectConfig(
            float defaultDurationSeconds,
            float combinedDurationSeconds,
            Effects.EffectModifier[] modifiers)
        {
            DefaultDurationSeconds = defaultDurationSeconds;
            CombinedDurationSeconds = combinedDurationSeconds;
            Modifiers = modifiers ?? System.Array.Empty<Effects.EffectModifier>();
        }
    }

    /// <summary>O power-up caindo pela arena.</summary>
    public readonly struct PickupConfig
    {
        /// <summary>Velocidade de queda em unidades por segundo.</summary>
        public readonly float FallSpeed;

        /// <summary>Raio de coleta, somado a meia-largura da raquete.</summary>
        public readonly float Radius;

        public PickupConfig(float fallSpeed, float radius)
        {
            FallSpeed = fallSpeed;
            Radius = radius;
        }
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
        public readonly EffectConfig Effects;
        public readonly PickupConfig Pickup;
        public readonly TrophyConfig Trophies;

        public MatchConfig(
            ArenaConfig arena,
            BallConfig ball,
            PaddleConfig paddle,
            TowerConfig tower,
            ElixirConfig elixir,
            MatchRulesConfig rules,
            EffectConfig effects,
            PickupConfig pickup,
            TrophyConfig trophies)
        {
            Arena = arena;
            Ball = ball;
            Paddle = paddle;
            Tower = tower;
            Elixir = elixir;
            Rules = rules;
            Effects = effects;
            Pickup = pickup;
            Trophies = trophies;
        }
    }
}
