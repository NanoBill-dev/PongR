using System.Numerics;
using PongRoyale.Core.Simulation;

namespace PongRoyale.Tests.EditMode
{
    /// <summary>
    /// Configuracao canonica para os testes. Deliberadamente NAO le o BalanceData: teste
    /// que depende de asset quebra quando alguem ajusta o balanceamento, e um teste que
    /// quebra por motivo legitimo de design deixa de ser util. Os numeros aqui sao fixos
    /// e a intencao de cada um fica explicita no teste que o usa.
    /// </summary>
    public static class TestConfigs
    {
        public static MatchConfig Default()
        {
            return new MatchConfig(
                new ArenaConfig(width: 10f, height: 18f, paddleLineOffsetFromEdge: 2.5f),
                new BallConfig(
                    initialSpeed: 8f,
                    maxSpeed: 25f,
                    speedGainPerHit: 0.02f,
                    radius: 0.25f,
                    baseDamage: 250f,
                    maxDeflectionFromNormalDegrees: 60f,
                    minAngleFromHorizontalDegrees: 20f),
                new PaddleConfig(
                    width: 2.4f,
                    thickness: 0.4f,
                    maxSpeed: 18f,
                    smoothingTime: 0.05f,
                    dragSensitivity: 1f),
                new TowerConfig(
                    kingMaxHealth: 5000f,
                    guardMaxHealth: 2500f,
                    guardOffsetFromCenter: 3.2f,
                    rowOffsetFromEdge: 1f,
                    kingHalfSize: new Vector2(1.2f, 0.8f),
                    guardHalfSize: new Vector2(0.9f, 0.7f)),
                new ElixirConfig(
                    maxElixir: 10f,
                    startingElixir: 5f,
                    secondsPerElixir: 2f,
                    secondsPerElixirInDoubleMode: 1f),
                new MatchRulesConfig(
                    matchDurationSeconds: 180f,
                    doubleElixirLastSeconds: 60f,
                    overtimeDurationSeconds: 60f,
                    tiebreak: TiebreakRule.SuddenDeath),
                new TrophyConfig(onWin: 30, onLoss: -25, onDraw: 0));
        }
    }
}
