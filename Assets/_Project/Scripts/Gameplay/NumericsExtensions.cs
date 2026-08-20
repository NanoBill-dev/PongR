using UnityEngine;

namespace PongRoyale.Gameplay
{
    /// <summary>
    /// Ponte de vetores entre o Core e a engine. O Core usa System.Numerics.Vector2 porque
    /// nao pode referenciar UnityEngine (ADR-001/ADR-006); a conversao mora aqui, num unico
    /// lugar, em vez de espalhar "new Vector3(v.X, v.Y, 0f)" por toda a camada de views.
    /// </summary>
    public static class NumericsExtensions
    {
        public static Vector3 ToWorldPosition(this System.Numerics.Vector2 value, float z = 0f)
        {
            return new Vector3(value.X, value.Y, z);
        }

        public static Vector2 ToUnity(this System.Numerics.Vector2 value)
        {
            return new Vector2(value.X, value.Y);
        }

        public static System.Numerics.Vector2 ToNumerics(this Vector2 value)
        {
            return new System.Numerics.Vector2(value.x, value.y);
        }
    }
}
