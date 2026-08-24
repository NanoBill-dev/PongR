using PongRoyale.Core.Simulation;
using UnityEngine;

namespace PongRoyale.Gameplay.Paddle
{
    /// <summary>
    /// Espelha uma raquete da simulacao. A posicao em Y vem da linha definida pela arena no
    /// inicio da partida; so o X se move.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PaddleView : MonoBehaviour
    {
        [SerializeField] private MatchRunner runner;
        [SerializeField] private PlayerSlot slot = PlayerSlot.Bottom;

        private float lineY;

        private void Start()
        {
            if (runner == null || !runner.IsReady)
            {
                return;
            }

            SpriteFitter.Fit(
                GetComponent<SpriteRenderer>(),
                runner.Config.Paddle.Width,
                runner.Config.Paddle.Thickness);

            lineY = runner.Simulation.State.GetPaddle(slot).LineY;
        }

        private void LateUpdate()
        {
            if (runner == null || !runner.IsReady)
            {
                return;
            }

            transform.position = new Vector3(runner.Simulation.State.GetPaddle(slot).PositionX, lineY, 0f);
        }
    }
}
