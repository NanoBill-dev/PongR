using System;
using System.Numerics;
using NUnit.Framework;
using PongRoyale.Core.Ball;

namespace PongRoyale.Tests.EditMode
{
    public sealed class CollisionMathTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void HeadOnSweepFindsTheExactContactMoment()
        {
            // Bola de raio 0.5 em (0,0), caixa de meia-extensao 0.5 centrada em (3,0).
            // As superficies se tocam quando os centros distam 0.5 + 0.5 = 1, ou seja com
            // a bola em x = 2. Sobre um deslocamento de 4 unidades, isso e metade do passo.
            bool hit = CollisionMath.SweepCircleVsBox(
                origin: Vector2.Zero,
                delta: new Vector2(4f, 0f),
                radius: 0.5f,
                boxCenter: new Vector2(3f, 0f),
                boxHalfSize: new Vector2(0.5f, 0.5f),
                out float time,
                out Vector2 normal,
                out _);

            Assert.IsTrue(hit);
            Assert.AreEqual(0.5f, time, Tolerance);
            Assert.AreEqual(new Vector2(-1f, 0f), normal);
        }

        [Test]
        public void SweepMissesWhenTheBoxIsOffTheTrajectory()
        {
            bool hit = CollisionMath.SweepCircleVsBox(
                origin: Vector2.Zero,
                delta: new Vector2(0f, 4f),
                radius: 0.5f,
                boxCenter: new Vector2(3f, 0f),
                boxHalfSize: new Vector2(0.5f, 0.5f),
                out _,
                out _,
                out _);

            Assert.IsFalse(hit);
        }

        [Test]
        public void SweepStopsShortWhenTheBoxIsBeyondTheStep()
        {
            // A caixa esta na trajetoria, mas longe demais para ser alcancada neste passo.
            bool hit = CollisionMath.SweepCircleVsBox(
                origin: Vector2.Zero,
                delta: new Vector2(0.5f, 0f),
                radius: 0.5f,
                boxCenter: new Vector2(10f, 0f),
                boxHalfSize: new Vector2(0.5f, 0.5f),
                out _,
                out _,
                out _);

            Assert.IsFalse(hit);
        }

        [Test]
        public void OverlappingStartResolvesAtTimeZeroInsteadOfPassingThrough()
        {
            // Pode acontecer quando a raquete se move para dentro da bola. Ignorar seria
            // deixar a bola atravessar; resolver em t=0 empurra para fora no mesmo tick.
            bool hit = CollisionMath.SweepCircleVsBox(
                origin: new Vector2(0.1f, 0f),
                delta: new Vector2(1f, 0f),
                radius: 0.5f,
                boxCenter: Vector2.Zero,
                boxHalfSize: new Vector2(0.5f, 0.5f),
                out float time,
                out _,
                out float separation);

            Assert.IsTrue(hit);
            Assert.AreEqual(0f, time, Tolerance);
            Assert.Greater(
                separation,
                0f,
                "Comecando sobreposta, a varredura precisa dizer quanto empurrar para " +
                "desencaixar. Sem isso a bola fica presa colidindo a cada tick.");
        }

        [Test]
        public void BoundsSweepAccountsForTheBallRadius()
        {
            // Parede em x = 5, bola de raio 0.25: o contato ocorre em x = 4.75.
            bool hit = CollisionMath.SweepCircleInsideBounds(
                origin: Vector2.Zero,
                delta: new Vector2(10f, 0f),
                radius: 0.25f,
                halfExtents: new Vector2(5f, 9f),
                out float time,
                out Vector2 normal,
                out _);

            Assert.IsTrue(hit);
            Assert.AreEqual(0.475f, time, Tolerance);
            Assert.AreEqual(new Vector2(-1f, 0f), normal);
        }

        [Test]
        public void BoundsSweepReportsNoHitWhenTheStepStaysInside()
        {
            bool hit = CollisionMath.SweepCircleInsideBounds(
                origin: Vector2.Zero,
                delta: new Vector2(0.1f, 0.1f),
                radius: 0.25f,
                halfExtents: new Vector2(5f, 9f),
                out _,
                out _,
                out _);

            Assert.IsFalse(hit);
        }

        [Test]
        public void ReflectionFlipsOnlyTheNormalComponent()
        {
            Vector2 reflected = CollisionMath.Reflect(
                new Vector2(1f, 1f),
                new Vector2(-1f, 0f));

            Assert.AreEqual(-1f, reflected.X, Tolerance);
            Assert.AreEqual(1f, reflected.Y, Tolerance);
        }

        [Test]
        public void CenterHitSendsTheBallStraightBack()
        {
            Vector2 direction = CollisionMath.PaddleDeflection(
                normalizedOffset: 0f,
                maxDeflectionDegrees: 60f,
                inwardNormalSign: 1f);

            Assert.AreEqual(0f, direction.X, Tolerance);
            Assert.AreEqual(1f, direction.Y, Tolerance);
        }

        [Test]
        public void EdgeHitSendsTheBallAtTheMaximumAngle()
        {
            Vector2 right = CollisionMath.PaddleDeflection(1f, 60f, 1f);
            Vector2 left = CollisionMath.PaddleDeflection(-1f, 60f, 1f);

            Assert.AreEqual((float)Math.Sin(Math.PI / 3.0), right.X, Tolerance);
            Assert.AreEqual((float)Math.Cos(Math.PI / 3.0), right.Y, Tolerance);
            Assert.AreEqual(-right.X, left.X, Tolerance);
            Assert.AreEqual(right.Y, left.Y, Tolerance);
        }

        [Test]
        public void TopPaddleDeflectsDownward()
        {
            Vector2 direction = CollisionMath.PaddleDeflection(0f, 60f, -1f);

            Assert.AreEqual(-1f, direction.Y, Tolerance);
        }

        [Test]
        public void DeflectionSaturatesBeyondTheEdgeOfThePaddle()
        {
            Vector2 atEdge = CollisionMath.PaddleDeflection(1f, 60f, 1f);
            Vector2 beyondEdge = CollisionMath.PaddleDeflection(5f, 60f, 1f);

            Assert.AreEqual(atEdge.X, beyondEdge.X, Tolerance);
            Assert.AreEqual(atEdge.Y, beyondEdge.Y, Tolerance);
        }

        [Test]
        public void ShallowDirectionIsLiftedToTheMinimumAngle()
        {
            Vector2 corrected = CollisionMath.EnforceMinAngleFromHorizontal(
                new Vector2(1f, 0.001f),
                minAngleDegrees: 20f);

            float expectedVertical = (float)Math.Sin(20.0 * Math.PI / 180.0);

            Assert.AreEqual(expectedVertical, corrected.Y, Tolerance);
            Assert.AreEqual(1f, corrected.Length(), Tolerance, "A direcao corrigida precisa continuar unitaria.");
        }

        [Test]
        public void SteepDirectionIsLeftUntouched()
        {
            var steep = new Vector2(0f, 1f);
            Vector2 result = CollisionMath.EnforceMinAngleFromHorizontal(steep, 20f);

            Assert.AreEqual(steep, result);
        }

        [Test]
        public void MinimumAnglePreservesTheDirectionOfTravel()
        {
            Vector2 corrected = CollisionMath.EnforceMinAngleFromHorizontal(
                new Vector2(-1f, -0.001f),
                minAngleDegrees: 20f);

            Assert.Less(corrected.X, 0f, "A bola continuava indo para a esquerda.");
            Assert.Less(corrected.Y, 0f, "E continuava descendo.");
        }

        [Test]
        public void PaddleOffsetIsClampedToTheEdges()
        {
            Assert.AreEqual(0f, CollisionMath.NormalizedPaddleOffset(2f, 2f, 1.2f), Tolerance);
            Assert.AreEqual(1f, CollisionMath.NormalizedPaddleOffset(9f, 2f, 1.2f), Tolerance);
            Assert.AreEqual(-1f, CollisionMath.NormalizedPaddleOffset(-9f, 2f, 1.2f), Tolerance);
        }
    }
}
