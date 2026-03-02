namespace DnDBattle.GameLogic.Rules;

public static class DndConstants
{
    public const int DefaultCellSizeFeet = 5;
    public const int MaxAbilityScore = 30;
    public const int MinAbilityScore = 1;
    public const int DeathSaveSuccessesRequired = 3;
    public const int DeathSaveFailuresRequired = 3;
    public const int MaxSpellLevel = 9;
    public const double DiagonalMovementCost = 1.5;
    public const int NormalVisionRange = 60;
    public const int DarkvisionDefault = 60;

    public static int CalculateProficiencyBonus(int challengeRating) => challengeRating switch
    {
        <= 4 => 2,
        <= 8 => 3,
        <= 12 => 4,
        <= 16 => 5,
        _ => 6
    };

    public static int AbilityModifier(int score) => (score - 10) / 2;

    public static string ChallengeRatingToString(double cr) => cr switch
    {
        0 => "0",
        0.125 => "1/8",
        0.25 => "1/4",
        0.5 => "1/2",
        _ => ((int)cr).ToString()
    };
}
