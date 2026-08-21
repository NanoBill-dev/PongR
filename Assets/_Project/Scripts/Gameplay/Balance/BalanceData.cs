using System;
using PongRoyale.Core.Simulation;
using UnityEngine;

namespace PongRoyale.Gameplay.Balance
{
    /// <summary>
    /// Fonte unica de verdade do balanceamento (ADR-005). Todo numero de gameplay mora
    /// aqui e em nenhum script. Ajustar o jogo nao exige recompilar, e da para manter
    /// varios assets em paralelo (producao, experimento, teste de carta nova).
    ///
    /// Este ScriptableObject e camada de AUTORIA. Ele nunca entra na simulacao: no inicio
    /// da partida <see cref="ToMatchConfig"/> produz um <see cref="MatchConfig"/> imutavel,
    /// que e o que o Core enxerga. Assim o Core continua sem depender de UnityEngine e o
    /// asset nunca carrega estado mutavel de runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Pong Royale/Balance Data", fileName = "BalanceData")]
    public sealed class BalanceData : ScriptableObject
    {
        [SerializeField] private ArenaSettings arena = new ArenaSettings();
        [SerializeField] private BallSettings ball = new BallSettings();
        [SerializeField] private PaddleSettings paddle = new PaddleSettings();
        [SerializeField] private TowerSettings towers = new TowerSettings();
        [SerializeField] private ElixirSettings elixir = new ElixirSettings();
        [SerializeField] private MatchRulesSettings rules = new MatchRulesSettings();
        [SerializeField] private TrophySettings trophies = new TrophySettings();

        /// <summary>Converte os dados de autoria no pacote imutavel consumido pela simulacao.</summary>
        public MatchConfig ToMatchConfig()
        {
            return new MatchConfig(
                new ArenaConfig(arena.width, arena.height, arena.paddleLineOffsetFromEdge),
                new BallConfig(
                    ball.initialSpeed,
                    ball.maxSpeed,
                    ball.speedGainPerHit,
                    ball.radius,
                    ball.baseDamage,
                    ball.maxDeflectionFromNormalDegrees,
                    ball.minAngleFromHorizontalDegrees),
                new PaddleConfig(
                    paddle.width,
                    paddle.thickness,
                    paddle.maxSpeed,
                    paddle.smoothingTime,
                    paddle.dragSensitivity,
                    paddle.sweepCarry),
                new TowerConfig(
                    towers.kingMaxHealth,
                    towers.guardMaxHealth,
                    towers.guardOffsetFromCenter,
                    towers.rowOffsetFromEdge,
                    ToNumerics(towers.kingHalfSize),
                    ToNumerics(towers.guardHalfSize)),
                new ElixirConfig(
                    elixir.maxElixir,
                    elixir.startingElixir,
                    elixir.secondsPerElixir,
                    elixir.secondsPerElixirInDoubleMode),
                new MatchRulesConfig(rules.matchDurationSeconds, rules.doubleElixirLastSeconds),
                new TrophyConfig(trophies.onWin, trophies.onLoss, trophies.onDraw));
        }

        /// <summary>
        /// Ponte de vetor entre as camadas: o Inspector edita UnityEngine.Vector2, mas o
        /// Core so conhece System.Numerics.Vector2 porque nao referencia a engine (ADR-001).
        /// </summary>
        private static System.Numerics.Vector2 ToNumerics(Vector2 value) =>
            new System.Numerics.Vector2(value.x, value.y);

        private void OnValidate()
        {
            // Invariantes que nao fazem sentido violar nem durante experimentos de balanceamento.
            ball.maxSpeed = Mathf.Max(ball.maxSpeed, ball.initialSpeed);
            elixir.startingElixir = Mathf.Clamp(elixir.startingElixir, 0f, elixir.maxElixir);
            rules.doubleElixirLastSeconds = Mathf.Clamp(rules.doubleElixirLastSeconds, 0f, rules.matchDurationSeconds);
            paddle.width = Mathf.Min(paddle.width, arena.width);
        }

        [Serializable]
        private sealed class ArenaSettings
        {
            [Tooltip("Largura da arena em unidades de mundo. 1 unidade = 1 metro.")]
            [Min(1f)] public float width = 10f;

            [Tooltip("Altura da arena. Retrato: a bola percorre este eixo.")]
            [Min(1f)] public float height = 18f;

            [Tooltip("Distancia da linha da raquete ate a borda do proprio lado.")]
            [Min(0.1f)] public float paddleLineOffsetFromEdge = 2.5f;
        }

        [Serializable]
        private sealed class BallSettings
        {
            [Min(0.1f)] public float initialSpeed = 8f;
            [Min(0.1f)] public float maxSpeed = 25f;

            [Tooltip("Ganho de velocidade por rebatida. 0.02 = mais 2 por cento.")]
            [Range(0f, 0.25f)] public float speedGainPerHit = 0.02f;

            [Min(0.01f)] public float radius = 0.25f;
            [Min(0f)] public float baseDamage = 250f;

            [Tooltip("Deflexao maxima a partir da normal da raquete, conforme o offset do impacto.")]
            [Range(5f, 85f)] public float maxDeflectionFromNormalDegrees = 60f;

            [Tooltip("Angulo minimo com a horizontal. Impede a bola de nunca chegar a uma raquete.")]
            [Range(1f, 45f)] public float minAngleFromHorizontalDegrees = 20f;
        }

        [Serializable]
        private sealed class PaddleSettings
        {
            [Min(0.1f)] public float width = 2.4f;
            [Min(0.05f)] public float thickness = 0.4f;

            [Tooltip("Teto de velocidade. Tambem serve de limite antifraude no servidor.")]
            [Min(0.1f)] public float maxSpeed = 18f;

            [Tooltip("Tempo de suavizacao ate a posicao alvo do dedo. Zero deixa o controle seco.")]
            [Range(0f, 0.3f)] public float smoothingTime = 0.05f;

            [Tooltip("Multiplicador do arraste do dedo em unidades de mundo.")]
            [Min(0.1f)] public float dragSensitivity = 1f;

            [Tooltip("Quanto da velocidade da raquete e transferida para a bola no impacto. " +
                     "Zero = Pong classico, so o ponto do impacto conta. Acima de zero, " +
                     "varrer a raquete empurra a bola para o lado.")]
            [Range(0f, 1f)] public float sweepCarry = 0.35f;
        }

        [Serializable]
        private sealed class TowerSettings
        {
            [Min(1f)] public float kingMaxHealth = 5000f;
            [Min(1f)] public float guardMaxHealth = 2500f;

            [Tooltip("Distancia em X do centro ate cada torre lateral.")]
            [Min(0f)] public float guardOffsetFromCenter = 3.2f;

            [Tooltip("Distancia da borda da arena ate a linha das torres.")]
            [Min(0.1f)] public float rowOffsetFromEdge = 1f;

            [Tooltip("Meias-extensoes da Torre Rei, usadas na colisao da bola.")]
            public Vector2 kingHalfSize = new Vector2(1.2f, 0.8f);

            [Tooltip("Meias-extensoes das torres laterais.")]
            public Vector2 guardHalfSize = new Vector2(0.9f, 0.7f);
        }

        [Serializable]
        private sealed class ElixirSettings
        {
            [Min(1f)] public float maxElixir = 10f;
            [Min(0f)] public float startingElixir = 5f;

            [Tooltip("Segundos por ponto de elixir no ritmo normal.")]
            [Min(0.05f)] public float secondsPerElixir = 2f;

            [Tooltip("Segundos por ponto de elixir no ultimo minuto.")]
            [Min(0.05f)] public float secondsPerElixirInDoubleMode = 1f;
        }

        [Serializable]
        private sealed class MatchRulesSettings
        {
            [Min(10f)] public float matchDurationSeconds = 180f;

            [Tooltip("Segundos finais com elixir em ritmo dobrado.")]
            [Min(0f)] public float doubleElixirLastSeconds = 60f;
        }

        [Serializable]
        private sealed class TrophySettings
        {
            public int onWin = 30;
            public int onLoss = -25;
            public int onDraw = 0;
        }
    }
}
