using DnDBattle.Models.Encounters;
using DnDBattle.Models.Creatures;

namespace DnDBattle.Tests.Models.Encounters
{
    public class InitiativeEntryTests
    {
        [Fact]
        public void DefaultConstructor_InitializesCorrectly()
        {
            var entry = new InitiativeEntry();
            Assert.Null(entry.Token);
            Assert.Equal(0, entry.InitiativeRoll);
            Assert.Equal(0, entry.InitiativeTotal);
            Assert.False(entry.IsCurrentTurn);
            Assert.False(entry.IsDelaying);
            Assert.False(entry.IsReadying);
            Assert.False(entry.HasActed);
        }

        [Fact]
        public void TokenConstructor_SetsTokenAndInitiative()
        {
            var token = new Token { Name = "Fighter", Initiative = 15 };
            var entry = new InitiativeEntry(token);
            Assert.Equal(token, entry.Token);
            Assert.Equal(15, entry.InitiativeTotal);
        }

        [Fact]
        public void RollConstructor_CalculatesTotal()
        {
            var token = new Token { Name = "Fighter", InitiativeModifier = 3 };
            var entry = new InitiativeEntry(token, 15);
            Assert.Equal(15, entry.InitiativeRoll);
            Assert.Equal(18, entry.InitiativeTotal); // 15 + 3
            Assert.Equal(18, token.Initiative); // Should update token too
        }

        [Fact]
        public void DisplayName_ReturnsTokenName()
        {
            var token = new Token { Name = "Goblin" };
            var entry = new InitiativeEntry(token);
            Assert.Equal("Goblin", entry.DisplayName);
        }

        [Fact]
        public void DisplayName_NullToken_ReturnsUnknown()
        {
            var entry = new InitiativeEntry();
            Assert.Equal("Unknown", entry.DisplayName);
        }

        [Fact]
        public void IsPlayer_ReflectsTokenIsPlayer()
        {
            var player = new Token { IsPlayer = true };
            var monster = new Token { IsPlayer = false };
            Assert.True(new InitiativeEntry(player).IsPlayer);
            Assert.False(new InitiativeEntry(monster).IsPlayer);
        }

        [Fact]
        public void StatusText_Delaying_ShowsDelaying()
        {
            var entry = new InitiativeEntry { IsDelaying = true };
            Assert.Equal("Delaying", entry.StatusText);
        }

        [Fact]
        public void StatusText_Readying_ShowsReadiedAction()
        {
            var entry = new InitiativeEntry
            {
                IsReadying = true,
                ReadiedAction = "Cast Shield"
            };
            Assert.Contains("Cast Shield", entry.StatusText);
        }

        [Fact]
        public void StatusText_HasActed_ShowsDone()
        {
            var entry = new InitiativeEntry { HasActed = true };
            Assert.NotEmpty(entry.StatusText);
        }

        [Fact]
        public void StatusText_Default_ReturnsEmpty()
        {
            var entry = new InitiativeEntry();
            Assert.Equal("", entry.StatusText);
        }

        [Fact]
        public void ResetForNewRound_ClearsFlags()
        {
            var entry = new InitiativeEntry
            {
                HasActed = true,
                IsDelaying = true
            };
            entry.ResetForNewRound();
            Assert.False(entry.HasActed);
            Assert.False(entry.IsDelaying);
        }
    }
}
