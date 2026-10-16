namespace TypingShip;

// StringData from the original: every piece of menu and screen text.
public static class Strings
{
    // Achievements, in save-slot order (GameModel.achieve[0..5]).
    public static readonly string[] AchieveNames = { "SOLDIER", "CONQUEROR", "RIDDLER", "DEFENDER", "PERFECTION", "EXPLORER" };

    /// <summary>getAchieveArray: what earns the bronze, silver and gold tier.</summary>
    public static readonly string[][] AchieveText =
    {
        new[] { "DESTROY 10 ENEMY SHIPS", "DESTROY 1,000 ENEMY SHIPS", "DESTROY 20,000 ENEMY SHIPS" },
        new[] { "DEFEAT THE FIRST BOSS", "DEFEAT THE SECOND BOSS", "FINISH THE GAME" },
        new[] { "IT'S A BIRD, IT'S A PLANE, IT'S...", "JAPANESE DEATH PILOT'S SCREAM", "NATURE'S MASTER OF DISGUISE" },
        new[] { "COMPLETE ANY LEVEL WITH PERFECT HEALTH", "ACHIEVE 5 GOLD HEALTH MEDALS", "FINISH EVERY LEVEL WITH PERFECT HEALTH" },
        new[] { "COMPLETE ANY LEVEL KILLING EVERY SHIP", "ACHIEVE 5 GOLD ACCURACY MEDALS", "FINISH EVERY LEVEL WITH PERFECT ACCURACY" },
        new[] { "VIEW THE CREDITS", "PAUSE THE GAME WITHOUT CLICKING", "KILL SUPERMAN" },
    };

    public static readonly string[] Tiers = { "", "BRONZE", "SILVER", "GOLD" };
    public const string Done = "DONE", NewAchieve = "New Achievement!";

    // Score screen
    public const string LevelScore = "LEVEL SCORE: ", DiffBonus = "DIFFICULTY BONUS x", HealthBonus = "HEALTH BONUS x",
        HealthPerfect = "PERFECT HEALTH x", AccBonus = "ACCURACY BONUS x", AccPerfect = "PERFECT ACCURACY x",
        FinalScore = "FINAL SCORE", Spacebar = "PRESS SPACE TO CONTINUE";

    // In game
    public const string Paused = "-PAUSED-", Yes = "YES", No = "NO";

    // Difficulty select, easiest first
    public static readonly string[] Diffs = { "EASY", "NORMAL", "HARD", "EXTREME", "INSANE" };

    // Game over screen
    public const string Menu = "MENU", WonGame = "VICTORY", LoseGame = "SHIP DESTROYED";

    // Map and menus
    public const string Health = "HEALTH", Accuracy = "ACCURACY", Skip = "SKIP", TypingTest = "TYPING TEST";

    // Typing test (the passages are in data/typing-test.txt)
    public const string TestHint = "Type the text. The clock starts with your first key.";
    public static string TestResult(int wpm, int accuracy, string difficulty) =>
        $"Net speed: {wpm} WPM\nAccuracy: {accuracy}%\nSuggested: {difficulty}";

    /// <summary>The credits screen: each role and who did it (from the original's credits picture).</summary>
    public static readonly (string Heading, string Names)[] Credits =
    {
        ("PROGRAMMING AND DESIGN", "YERMIYAH HORNICK"),
        ("ART", "AVI KENTRIDGE"),
        ("MUSIC", "BINYAMIN HORNICK"),
        ("DESIGN CONSULTANT", "XELU"),
        ("FONTS", "ORBITRON BY THE ORBITRON PROJECT AUTHORS, JETBRAINS MONO BY JETBRAINS (SIL OPEN FONT LICENSE)"),
        ("SPECIAL THANKS", "GOD, MOM, A. WALKER, DAVEY KOFSKY, DAMON KENTRIDGE, MICHELLE KENTRIDGE, GABRIEL MENRIX, MAX JACKEL, " +
            "EDWARD GEIN, KUROSAI, AND YOU!"),
    };

    public const string FinalCredits =
        "As the debris settles in the depths of space a heavy sigh of relief washes across all the people of earth.  " +
        "The evil emperor has been defeated and the planet freed... an age of peace and prosperity is sure to follow." +
        "\n\n\nConcept and Design by:\nYermiyah Hornick\n\n\nProgramming by:\nYermiyah Hornick\n\n\nArt by:\nAvi Kentridge" +
        "\n\n\nMusic By:\nBinyamin Hornick\n\n\nSound Effects by:\nYermiyah Hornick\nAvi Kentridge\n\n\nThank you for playing!";

    /// <summary>PopBox.tutText. Only 7-10 are used; the original commented out the triggers for 0-6.</summary>
    public static readonly string[] Tutorial =
    {
        "This is an Enemy Ship.  Type in the word shown below it to target it with your missiles.",
        "This is a Laser Ship.  It will shoot an unblockable laser at you if not killed quickly.",
        "This ship is Shielded.  You must type in two words to destroy it.",
        "This is a Suicide Ship.  It will gain speed and attempt to ram you.",
        "Larger ships have larger words to type in order to destroy them.",
        "This ship has a Defense Turret.  If you dont destroy it then it will shoot down any missiles you launch at this ship.",
        "This is a Space Mine.  If it gets too close it will damage you.",
        "Some ships shoot missiles.  Shoot them down before they hit you.",
        "You have been damaged!  Type in the word next to your Hull Bar to repair your ship.",
        "A boss is approaching!  Type in the small words to hone into its weak spot, then type the long word to damage it.",
        "Good job!  Two more shots to defeat the boss.",
    };

    public const string TutorialKeys = "hit SPACEBAR to continue\nhit ESCAPE to disable tutorial";
}
