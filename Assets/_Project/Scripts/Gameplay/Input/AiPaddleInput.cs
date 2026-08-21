using PongRoyale.Core.Simulation;
using UnityEngine;

namespace PongRoyale.Gameplay.Input
{
    /// <summary>
    /// Adaptador do bot para o sistema de input. Toda a decisao mora em
    /// <see cref="AiPaddleBrain"/>, que e C# puro e testavel; este componente so liga o
    /// cerebro ao ciclo de vida do Unity.
    ///
    /// E o que torna as FASES 1 e 2 jogaveis sem multiplayer, e vira o modo treino depois.
    /// </summary>
    public sealed class AiPaddleInput : PaddleInputSource
    {
        [SerializeField] private AiSettings settings = AiSettings.Default;

        [Tooltip("Semente do gerador de erro. Fixar torna o comportamento reproduzivel.")]
        [SerializeField] private int randomSeed = 20260820;

        private AiPaddleBrain brain;

        private void Awake()
        {
            brain = new AiPaddleBrain(randomSeed);
        }

        public override bool TryReadTargetX(MatchState state, PlayerSlot slot, out float targetX)
        {
            targetX = brain.Decide(state, slot, Time.deltaTime, settings);
            return true;
        }
    }
}
