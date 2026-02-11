using DnDBattle.Models.Spells;

namespace DnDBattle.Tests.Models.Spells
{
    public class SpellSlotsTests
    {
        [Fact]
        public void DefaultValues_NoSlots()
        {
            var slots = new SpellSlots();
            Assert.False(slots.HasSpellSlots);
            for (int i = 1; i <= 9; i++)
            {
                Assert.Equal(0, slots.GetMaxSlots(i));
                Assert.Equal(0, slots.GetCurrentSlots(i));
            }
        }

        [Fact]
        public void SetMaxSlots_SetsCorrectly()
        {
            var slots = new SpellSlots();
            slots.SetMaxSlots(1, 4);
            Assert.Equal(4, slots.GetMaxSlots(1));
            Assert.True(slots.HasSpellSlots);
        }

        [Fact]
        public void SetMaxSlots_OutOfRange_Ignored()
        {
            var slots = new SpellSlots();
            slots.SetMaxSlots(0, 5);
            slots.SetMaxSlots(10, 5);
            Assert.Equal(0, slots.GetMaxSlots(0));
            Assert.Equal(0, slots.GetMaxSlots(10));
        }

        [Fact]
        public void SetCurrentSlots_ClampedToMax()
        {
            var slots = new SpellSlots();
            slots.SetMaxSlots(1, 3);
            slots.SetCurrentSlots(1, 5);
            Assert.Equal(3, slots.GetCurrentSlots(1));
        }

        [Fact]
        public void SetCurrentSlots_ClampedToZero()
        {
            var slots = new SpellSlots();
            slots.SetMaxSlots(1, 3);
            slots.SetCurrentSlots(1, -1);
            Assert.Equal(0, slots.GetCurrentSlots(1));
        }

        [Fact]
        public void UseSlot_DecreasesCount()
        {
            var slots = new SpellSlots();
            slots.SetMaxSlots(1, 4);
            slots.SetCurrentSlots(1, 4);

            Assert.True(slots.UseSlot(1));
            Assert.Equal(3, slots.GetCurrentSlots(1));
        }

        [Fact]
        public void UseSlot_NoSlotsAvailable_ReturnsFalse()
        {
            var slots = new SpellSlots();
            slots.SetMaxSlots(1, 4);
            slots.SetCurrentSlots(1, 0);

            Assert.False(slots.UseSlot(1));
        }

        [Fact]
        public void UseSlot_OutOfRange_ReturnsFalse()
        {
            var slots = new SpellSlots();
            Assert.False(slots.UseSlot(0));
            Assert.False(slots.UseSlot(10));
        }

        [Fact]
        public void RestoreSlot_IncreasesCount()
        {
            var slots = new SpellSlots();
            slots.SetMaxSlots(1, 4);
            slots.SetCurrentSlots(1, 2);

            slots.RestoreSlot(1);
            Assert.Equal(3, slots.GetCurrentSlots(1));
        }

        [Fact]
        public void RestoreSlot_AtMax_DoesNotExceed()
        {
            var slots = new SpellSlots();
            slots.SetMaxSlots(1, 4);
            slots.SetCurrentSlots(1, 4);

            slots.RestoreSlot(1);
            Assert.Equal(4, slots.GetCurrentSlots(1));
        }

        [Fact]
        public void RestoreAllSlots_RestoresAll()
        {
            var slots = new SpellSlots();
            slots.SetMaxSlots(1, 4);
            slots.SetMaxSlots(2, 3);
            slots.SetCurrentSlots(1, 1);
            slots.SetCurrentSlots(2, 0);

            slots.RestoreAllSlots();
            Assert.Equal(4, slots.GetCurrentSlots(1));
            Assert.Equal(3, slots.GetCurrentSlots(2));
        }

        [Fact]
        public void LongRest_RestoresAllSlots()
        {
            var slots = new SpellSlots();
            slots.SetMaxSlots(1, 4);
            slots.SetCurrentSlots(1, 0);

            slots.LongRest();
            Assert.Equal(4, slots.GetCurrentSlots(1));
        }

        [Fact]
        public void GetDisplayForLevel_HasSlots_ShowsDisplay()
        {
            var slots = new SpellSlots();
            slots.SetMaxSlots(1, 4);
            slots.SetCurrentSlots(1, 3);

            Assert.Equal("3/4", slots.GetDisplayForLevel(1));
        }

        [Fact]
        public void GetDisplayForLevel_NoSlots_ReturnsNull()
        {
            var slots = new SpellSlots();
            Assert.Null(slots.GetDisplayForLevel(1));
        }

        [Fact]
        public void GetDisplayForLevel_OutOfRange_ReturnsNull()
        {
            var slots = new SpellSlots();
            Assert.Null(slots.GetDisplayForLevel(0));
            Assert.Null(slots.GetDisplayForLevel(10));
        }

        [Fact]
        public void DisplayProperties_MapCorrectly()
        {
            var slots = new SpellSlots();
            slots.SetMaxSlots(1, 4);
            slots.SetCurrentSlots(1, 3);
            Assert.Equal("3/4", slots.Level1Display);
        }

        #region Caster Level Progression

        [Fact]
        public void GetForCasterLevel_Level1FullCaster_Has2Level1Slots()
        {
            var slots = SpellSlots.GetForCasterLevel(1, CasterType.Full);
            Assert.Equal(2, slots.GetMaxSlots(1));
            Assert.Equal(2, slots.GetCurrentSlots(1));
            Assert.Equal(0, slots.GetMaxSlots(2));
        }

        [Fact]
        public void GetForCasterLevel_Level5FullCaster_HasLevel3Slots()
        {
            var slots = SpellSlots.GetForCasterLevel(5, CasterType.Full);
            Assert.Equal(4, slots.GetMaxSlots(1));
            Assert.Equal(3, slots.GetMaxSlots(2));
            Assert.Equal(2, slots.GetMaxSlots(3));
            Assert.Equal(0, slots.GetMaxSlots(4));
        }

        [Fact]
        public void GetForCasterLevel_Level20FullCaster_HasLevel9Slots()
        {
            var slots = SpellSlots.GetForCasterLevel(20, CasterType.Full);
            Assert.Equal(1, slots.GetMaxSlots(9));
        }

        [Fact]
        public void GetForCasterLevel_HalfCaster_UsesHalfLevel()
        {
            // Half caster at level 4 => effective level 2 (4/2)
            var halfSlots = SpellSlots.GetForCasterLevel(4, CasterType.Half);
            var fullSlotsLevel2 = SpellSlots.GetForCasterLevel(2, CasterType.Full);
            Assert.Equal(fullSlotsLevel2.GetMaxSlots(1), halfSlots.GetMaxSlots(1));
        }

        [Fact]
        public void GetForCasterLevel_ThirdCaster_UsesThirdLevel()
        {
            // Third caster at level 9 => effective level 3 (9/3)
            var thirdSlots = SpellSlots.GetForCasterLevel(9, CasterType.Third);
            var fullSlotsLevel3 = SpellSlots.GetForCasterLevel(3, CasterType.Full);
            Assert.Equal(fullSlotsLevel3.GetMaxSlots(1), thirdSlots.GetMaxSlots(1));
        }

        [Fact]
        public void GetForCasterLevel_ClampedTo20()
        {
            // Should not throw for levels above 20
            var slots = SpellSlots.GetForCasterLevel(25, CasterType.Full);
            Assert.True(slots.HasSpellSlots);
        }

        [Fact]
        public void GetForCasterLevel_ClampedTo1()
        {
            var slots = SpellSlots.GetForCasterLevel(0, CasterType.Full);
            Assert.True(slots.HasSpellSlots);
        }

        #endregion

        [Fact]
        public void SetMaxSlots_ReducingMax_ClampsCurrent()
        {
            var slots = new SpellSlots();
            slots.SetMaxSlots(1, 4);
            slots.SetCurrentSlots(1, 4);
            slots.SetMaxSlots(1, 2);
            Assert.Equal(2, slots.GetCurrentSlots(1));
        }
    }
}
