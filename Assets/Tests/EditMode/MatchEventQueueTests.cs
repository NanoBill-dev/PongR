using System.Numerics;
using NUnit.Framework;
using PongRoyale.Core.Events;
using PongRoyale.Core.Simulation;

namespace PongRoyale.Tests.EditMode
{
    public sealed class MatchEventQueueTests
    {
        [Test]
        public void EventsComeOutInTheOrderTheyHappened()
        {
            // A ordem importa: num mesmo tick a bola bate na torre, causa dano e destroi.
            // Feedback fora de ordem viraria explosao antes do impacto.
            var queue = new MatchEventQueue();

            queue.Enqueue(MatchEvent.BallHitPaddle(1, PlayerSlot.Bottom, 0, 8f, Vector2.Zero));
            queue.Enqueue(MatchEvent.TowerDamaged(1, PlayerSlot.Top, 0, 250f, Vector2.Zero));
            queue.Enqueue(MatchEvent.TowerDestroyed(1, PlayerSlot.Top, 0, Vector2.Zero));

            Assert.AreEqual(3, queue.Count);
            Assert.AreEqual(MatchEventType.BallHitPaddle, queue[0].Type);
            Assert.AreEqual(MatchEventType.TowerDamaged, queue[1].Type);
            Assert.AreEqual(MatchEventType.TowerDestroyed, queue[2].Type);
        }

        [Test]
        public void QueueGrowsBeyondCapacityWithoutLosingEvents()
        {
            var queue = new MatchEventQueue(capacity: 2);

            for (int i = 0; i < 10; i++)
            {
                queue.Enqueue(MatchEvent.BallHitWall(i, (byte)i, Vector2.Zero));
            }

            Assert.AreEqual(10, queue.Count);
            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(i, queue[i].Tick);
            }
        }

        [Test]
        public void ClearKeepsTheBufferSoDrainingDoesNotAllocate()
        {
            // O contrato de zero GC em regime depende disto: esvaziar nao devolve memoria.
            var queue = new MatchEventQueue(capacity: 2);

            for (int i = 0; i < 8; i++)
            {
                queue.Enqueue(MatchEvent.BallHitWall(i, 0, Vector2.Zero));
            }

            int capacityAfterGrowth = queue.Capacity;
            queue.Clear();

            Assert.AreEqual(0, queue.Count);
            Assert.AreEqual(capacityAfterGrowth, queue.Capacity);
        }

        [Test]
        public void ReadingPastTheEndThrows()
        {
            var queue = new MatchEventQueue();
            queue.Enqueue(MatchEvent.BallHitWall(0, 0, Vector2.Zero));

            Assert.Throws<System.ArgumentOutOfRangeException>(() => _ = queue[1]);
        }

        [Test]
        public void PayloadSurvivesTheRoundTrip()
        {
            var queue = new MatchEventQueue();
            var position = new Vector2(1.5f, -2.5f);

            queue.Enqueue(MatchEvent.TowerDamaged(42, PlayerSlot.Top, 2, 250f, position));

            MatchEvent stored = queue[0];
            Assert.AreEqual(42, stored.Tick);
            Assert.AreEqual(PlayerSlot.Top, stored.Slot);
            Assert.AreEqual(2, stored.EntityIndex);
            Assert.AreEqual(250f, stored.Value, 1e-4f);
            Assert.AreEqual(position, stored.Position);
        }
    }
}
