using DnDBattle.Models.Creatures;
using DnDBattle.Services.Combat;

namespace DnDBattle.Tests.Services.Combat
{
    public class InitiativeManagerTests
    {
        private Token CreateToken(string name, int initiativeModifier = 0)
        {
            return new Token { Name = name, InitiativeModifier = initiativeModifier };
        }

        [Fact]
        public void Constructor_InitializesCorrectly()
        {
            var participants = new List<Token> { CreateToken("A"), CreateToken("B") };
            var manager = new InitiativeManager(participants);
            Assert.Equal(-1, manager.CurrentIndex);
            Assert.Empty(manager.LastRolls);
        }

        [Fact]
        public void RollInitiativeSingle_SetsTokenInitiative()
        {
            var token = CreateToken("Fighter", initiativeModifier: 3);
            var participants = new List<Token> { token };
            var manager = new InitiativeManager(participants);

            var result = manager.RollInitiativeSingle(token);
            Assert.Equal(token, result.Token);
            Assert.Equal(3, result.Modifier);
            Assert.InRange(result.Roll, 1, 20);
            Assert.Equal(result.Roll + 3, result.Total);
            Assert.Equal(result.Total, token.Initiative);
        }

        [Fact]
        public void RollAll_SetsAllInitiatives()
        {
            var participants = new List<Token>
            {
                CreateToken("A", 2),
                CreateToken("B", 0),
                CreateToken("C", -1)
            };
            var manager = new InitiativeManager(participants);

            var results = manager.RollAll();
            Assert.Equal(3, results.Count);
            foreach (var result in results)
            {
                Assert.InRange(result.Roll, 1, 20);
                Assert.Equal(result.Roll + result.Modifier, result.Total);
            }
        }

        [Fact]
        public void RollAll_SetsCurrentIndexToZero()
        {
            var participants = new List<Token> { CreateToken("A"), CreateToken("B") };
            var manager = new InitiativeManager(participants);
            manager.RollAll();
            Assert.Equal(0, manager.CurrentIndex);
        }

        [Fact]
        public void RollAll_EmptyList_CurrentIndexNegative()
        {
            var participants = new List<Token>();
            var manager = new InitiativeManager(participants);
            manager.RollAll();
            Assert.Equal(-1, manager.CurrentIndex);
        }

        [Fact]
        public void RollAll_WithCustomRoller_UsesCustomValues()
        {
            var participants = new List<Token>
            {
                CreateToken("A", 5),
                CreateToken("B", 3)
            };
            var manager = new InitiativeManager(participants);

            var results = manager.RollAll((token) =>
            {
                return token.Name == "A" ? (15, 5) : (10, 3);
            });

            Assert.Equal(2, results.Count);
            var resultA = results.First(r => r.Token.Name == "A");
            var resultB = results.First(r => r.Token.Name == "B");
            Assert.Equal(15, resultA.Roll);
            Assert.Equal(10, resultB.Roll);
        }

        [Fact]
        public void Order_SortsByInitiativeDescending()
        {
            var participants = new List<Token>
            {
                CreateToken("Low"),
                CreateToken("High"),
                CreateToken("Mid")
            };
            participants[0].Initiative = 5;
            participants[1].Initiative = 20;
            participants[2].Initiative = 12;

            var manager = new InitiativeManager(participants);
            var order = manager.Order;

            Assert.Equal("High", order[0].Name);
            Assert.Equal("Mid", order[1].Name);
            Assert.Equal("Low", order[2].Name);
        }

        [Fact]
        public void NextTurn_CyclesThroughParticipants()
        {
            var participants = new List<Token>
            {
                CreateToken("A"),
                CreateToken("B"),
                CreateToken("C")
            };
            var manager = new InitiativeManager(participants);
            manager.RollAll();

            Assert.Equal(0, manager.CurrentIndex);
            manager.NextTurn();
            Assert.Equal(1, manager.CurrentIndex);
            manager.NextTurn();
            Assert.Equal(2, manager.CurrentIndex);
            manager.NextTurn();
            Assert.Equal(0, manager.CurrentIndex); // Wraps around
        }

        [Fact]
        public void NextTurn_EmptyList_StaysNegative()
        {
            var participants = new List<Token>();
            var manager = new InitiativeManager(participants);
            manager.NextTurn();
            Assert.Equal(-1, manager.CurrentIndex);
        }

        [Fact]
        public void CurrentToken_BeforeRoll_ReturnsNull()
        {
            var participants = new List<Token> { CreateToken("A") };
            var manager = new InitiativeManager(participants);
            Assert.Null(manager.CurrentToken);
        }

        [Fact]
        public void Reset_ClearsAllInitiatives()
        {
            var participants = new List<Token>
            {
                CreateToken("A", 5),
                CreateToken("B", 3)
            };
            var manager = new InitiativeManager(participants);
            manager.RollAll();
            manager.Reset();

            Assert.Equal(-1, manager.CurrentIndex);
            Assert.Empty(manager.LastRolls);
            foreach (var t in participants)
            {
                Assert.Equal(0, t.Initiative);
            }
        }

        [Fact]
        public void InitiativeResult_TotalIsRollPlusModifier()
        {
            var result = new InitiativeResult { Roll = 15, Modifier = 3 };
            Assert.Equal(18, result.Total);
        }
    }
}
