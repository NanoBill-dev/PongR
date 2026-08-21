using PongRoyale.Core.Simulation;
using UnityEngine;

namespace PongRoyale.Gameplay.Input
{
    /// <summary>
    /// Traduz a intencao de uma fonte de input em <see cref="MatchCommand"/> e a deposita
    /// na fila do runner.
    ///
    /// A ordem de execucao e explicita e nao acidental: este componente PRECISA rodar antes
    /// do <see cref="MatchRunner"/>, senao o comando so seria consumido no frame seguinte e
    /// o jogador sentiria atraso na propria raquete. A ordem padrao do Unity entre
    /// MonoBehaviours e arbitraria, entao ela e fixada pelo atributo em vez de deixada ao
    /// acaso — bug de ordem de execucao aparece de forma intermitente e custa horas.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class PaddleController : MonoBehaviour
    {
        [SerializeField] private MatchRunner runner;
        [SerializeField] private PlayerSlot slot = PlayerSlot.Bottom;
        [SerializeField] private PaddleInputSource inputSource;

        private void Update()
        {
            if (runner == null || !runner.IsReady || inputSource == null)
            {
                return;
            }

            MatchState state = runner.Simulation.State;

            if (inputSource.TryReadTargetX(state, slot, out float targetX))
            {
                runner.Commands.Add(MatchCommand.PaddleMove(state.Tick, slot, targetX));
            }
        }
    }
}
