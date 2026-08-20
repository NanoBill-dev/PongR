using System.Numerics;
using NUnit.Framework;
using PongRoyale.Core.Simulation;

namespace PongRoyale.Tests.EditMode
{
    public sealed class MatchCommandTests
    {
        [Test]
        public void PaddleMoveCarriesOnlyTheTargetX()
        {
            MatchCommand command = MatchCommand.PaddleMove(tick: 7, slot: PlayerSlot.Bottom, targetX: -3.5f);

            Assert.AreEqual(CommandType.PaddleMove, command.Type);
            Assert.AreEqual(7, command.Tick);
            Assert.AreEqual(PlayerSlot.Bottom, command.Slot);
            Assert.AreEqual(-3.5f, command.PaddleTargetX, 1e-4f);
            Assert.AreEqual(Vector2.Zero, command.Target);
        }

        [Test]
        public void PlayCardCarriesHandSlotAndTarget()
        {
            var target = new Vector2(2f, -4f);
            MatchCommand command = MatchCommand.PlayCard(tick: 12, slot: PlayerSlot.Top, handSlot: 3, target: target);

            Assert.AreEqual(CommandType.PlayCard, command.Type);
            Assert.AreEqual(3, command.HandSlot);
            Assert.AreEqual(target, command.Target);
            Assert.AreEqual(0f, command.PaddleTargetX, 1e-4f);
        }

        [Test]
        public void EmoteCarriesOnlyTheEmoteId()
        {
            MatchCommand command = MatchCommand.Emote(tick: 1, slot: PlayerSlot.Bottom, emoteId: 5);

            Assert.AreEqual(CommandType.Emote, command.Type);
            Assert.AreEqual(5, command.EmoteId);
        }

        [Test]
        public void HandSlotFitsInsideTheHand()
        {
            // A mao tem HandSize cartas; o comando usa byte, entao o limite real e a regra,
            // nao o tipo. Este teste documenta a expectativa antes de existir validacao.
            MatchCommand command = MatchCommand.PlayCard(0, PlayerSlot.Bottom, (byte)(MatchConstants.HandSize - 1), Vector2.Zero);

            Assert.Less(command.HandSlot, MatchConstants.HandSize);
        }
    }
}
