using PongRoyale.Core.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PongRoyale.Gameplay.Input
{
    /// <summary>
    /// Controle por ponteiro. `Pointer.current` do Input System unifica dedo, mouse e
    /// caneta, entao o mesmo componente serve para testar no Editor e para jogar no
    /// celular — sem duas implementacoes que precisam ser mantidas em sincronia.
    ///
    /// O modo padrao e ARRASTE RELATIVO: a raquete se move pelo quanto o dedo andou, nao
    /// para onde o dedo esta. Isso resolve dois problemas de ergonomia mobile de uma vez:
    /// o dedo nao precisa cobrir a raquete, e o jogador nao precisa "pegar" a raquete
    /// antes de comecar a mover.
    /// </summary>
    public sealed class PointerPaddleInput : PaddleInputSource
    {
        [SerializeField] private Camera worldCamera;

        [Tooltip("Liga o modo absoluto: a raquete vai direto para onde o dedo esta. " +
                 "Menos ergonomico, mas alguns jogadores preferem.")]
        [SerializeField] private bool absoluteMode;

        private bool isDragging;
        private float dragStartWorldX;
        private float paddleStartX;

        private void Awake()
        {
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }
        }

        public override bool TryReadTargetX(MatchState state, PlayerSlot slot, out float targetX)
        {
            targetX = 0f;

            Pointer pointer = Pointer.current;
            if (pointer == null || worldCamera == null)
            {
                return false;
            }

            if (!pointer.press.isPressed)
            {
                isDragging = false;
                return false;
            }

            float worldX = ScreenToWorldX(pointer.position.ReadValue());

            if (!isDragging)
            {
                isDragging = true;
                dragStartWorldX = worldX;
                paddleStartX = state.GetPaddle(slot).PositionX;
            }

            targetX = absoluteMode
                ? worldX
                : paddleStartX + (worldX - dragStartWorldX) * state.Config.Paddle.DragSensitivity;

            return true;
        }

        private float ScreenToWorldX(Vector2 screenPosition)
        {
            // A distancia ate o plano do jogo e a propria profundidade da camera, ja que a
            // arena esta em z = 0.
            float depth = -worldCamera.transform.position.z;
            return worldCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, depth)).x;
        }
    }
}
