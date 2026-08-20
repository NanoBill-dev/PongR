using System;

namespace PongRoyale.Core.Combat
{
    /// <summary>
    /// Aplicacao de dano. Minusculo de proposito: existe para que exista UM lugar onde
    /// vida diminui. Quando entrarem escudo, dano por carta e dano ao longo do tempo,
    /// todos passam por aqui, e a regra de "nunca abaixo de zero, morre uma unica vez"
    /// nao precisa ser reescrita em cada chamador.
    /// </summary>
    public static class DamageResolver
    {
        /// <summary>
        /// Tira vida da torre e informa se ela morreu NESTA chamada.
        /// Bater numa torre ja destruida nao faz nada e nao dispara nada.
        /// </summary>
        public static bool ApplyDamage(ref TowerState tower, float damage)
        {
            if (!tower.IsAlive || damage <= 0f)
            {
                return false;
            }

            tower.Health = Math.Max(0f, tower.Health - damage);
            return !tower.IsAlive;
        }
    }
}
