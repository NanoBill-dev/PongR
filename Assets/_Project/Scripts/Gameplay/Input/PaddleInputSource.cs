using PongRoyale.Core.Simulation;
using UnityEngine;

namespace PongRoyale.Gameplay.Input
{
    /// <summary>
    /// Origem de comando para uma raquete. Dedo, mouse, bot e — na FASE 3 — a rede
    /// implementam esta mesma base, e a simulacao nao consegue distinguir qual delas
    /// produziu o comando (secao 3).
    ///
    /// E uma classe abstrata, e nao uma interface, por um motivo pratico: assim o campo
    /// aparece no Inspector como um slot que aceita qualquer fonte, sem gambiarra para
    /// serializar interface.
    /// </summary>
    public abstract class PaddleInputSource : MonoBehaviour
    {
        /// <summary>
        /// Devolve o alvo em X desejado para a raquete, em unidades de mundo.
        /// Retorna false quando nao ha intencao neste frame — dedo fora da tela, bot sem
        /// bola para perseguir — e nesse caso nenhum comando e emitido.
        /// </summary>
        public abstract bool TryReadTargetX(MatchState state, PlayerSlot slot, out float targetX);
    }
}
