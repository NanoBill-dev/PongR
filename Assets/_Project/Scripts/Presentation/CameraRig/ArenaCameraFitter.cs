using PongRoyale.Gameplay.Balance;
using UnityEngine;

namespace PongRoyale.Presentation.CameraRig
{
    /// <summary>
    /// Ajusta a camera ortografica para que a arena INTEIRA caiba na tela, seja qual for a
    /// proporcao do aparelho.
    ///
    /// Encaixar so pela altura cortaria as laterais num celular estreito; so pela largura
    /// cortaria o topo e a base num 9:16. O encaixe correto e o maior dos dois requisitos:
    ///
    ///     size = max(metadeDaAltura, metadeDaLargura / aspect)
    ///
    /// Num 9:16 isso da 9 (a arena preenche a altura, sobra um fio nas laterais). Num 9:19.5
    /// da 10,83: a largura manda e sobra espaco vertical — que e exatamente onde a HUD de
    /// cartas e elixir vai morar na FASE 2.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class ArenaCameraFitter : MonoBehaviour
    {
        [SerializeField] private BalanceData balanceData;

        [Tooltip("Folga extra em unidades de mundo alem da arena.")]
        [SerializeField, Min(0f)] private float margin;

        private Camera targetCamera;
        private float lastAspect;

        private void OnEnable()
        {
            targetCamera = GetComponent<Camera>();
            Fit();
        }

        private void Update()
        {
            // A proporcao muda ao girar o aparelho, ao redimensionar a Game view e ao trocar
            // de device no simulador. Reagir a mudanca custa uma comparacao por frame.
            if (!Mathf.Approximately(lastAspect, targetCamera.aspect))
            {
                Fit();
            }
        }

        private void Fit()
        {
            if (balanceData == null || targetCamera == null || targetCamera.aspect <= 0f)
            {
                return;
            }

            var arena = balanceData.ToMatchConfig().Arena;

            float sizeToFitHeight = arena.HalfHeight + margin;
            float sizeToFitWidth = (arena.HalfWidth + margin) / targetCamera.aspect;

            targetCamera.orthographic = true;
            targetCamera.orthographicSize = Mathf.Max(sizeToFitHeight, sizeToFitWidth);
            lastAspect = targetCamera.aspect;
        }
    }
}
