namespace TypingShip;

// Level scripts. The helpers keep the original lowercase names so the scripts read exactly as they did in AS3.
// Positions are on a 12x12 grid over the game view; distances are in "distance units" (the ship travels
// wpm/3600 of them per second), and steps of 0.1 or more shrink at higher typing speeds.
public static class LevelData
{
    static double dist;
    static int level;
    static List<ILevelEvent> array = new();
    static double mult;

    const float GridX = G.ViewWidth / 12, GridY = G.ViewHeight / 12;

    public static List<ILevelEvent> LoadData(int lvl)
    {
        mult = Math.Max(1 - G.Model.Wpm / 300.0, 0.5);
        array = new List<ILevelEvent>();
        dist = 0;
        level = lvl;
        switch (level)
        {
            case 0:
                missile("sn", 0.05, 5, 0, 5, 6, 4, 12);
                mirror();

                missile("sn", 0.15, 0, 3, 7, 3, 12, 3);
                missile("sn", 0.01, 12, 6, 5, 6, 0, 6);
                repeat(0.01, 3, 2);

                laser("sn", 0.1, 6, 0);

                missile("sn", 0.1, 3, 0, 7, 5, 4, 5, 7, 0);
                repeat(0.01, 5);
                laser("sn", 0, 12, 5);

                missile("sn", 0.13, 2, 0, 2, 4, 12, 9);
                repeat(0.01, 7, 1, true);
                laser("sn", 0, 0, 6);
                insert(-4);
                laser("sn", 0, 12, 6);
                insert(-4);

                laser("sn", 0.15, 0, 3);
                laser("sn", 0.005, 12, 3);
                laser("sn", 0.005, 0, 6);
                laser("sn", 0.005, 12, 6);

                missile("sn", 0.13, 3, 0, 4, 4, 7, 0);
                missile("sn", 0.01, 12, 5, 7, 6, 0, 5);
                repeat(0.02, 4, 2);
                laser("sn", 0.04, 0, 2);
                laser("sn", 0.01, 12, 2);
                laser("sn", 0.02, 0, 4);
                laser("sn", 0.01, 12, 4);
                missile("sn", 0.07, 9, 0, 8, 4, 5, 0);
                missile("sn", 0.01, 0, 5, 5, 6, 12, 5);
                repeat(0.02, 4, 2);
                break;

            case 1:
                missile("sn",0.1, 4,0, 4,7, 0,12);
                missile("sn",0,2,0, 2,6, 0,9);
                repeat(0,1,2,true);

                missile("sn",0.12, 0,3, 7,3, 12,3);
                offset(0,0,1);
                repeat(0.015,1,2);
                missile("sn",0.02, 12,5, 5,5, 0,5);
                offset(0,0,1);
                repeat(0.015,1,2);
                laser("ss",0,6,0);

                suicide("mn",0.14, 3,0, 3,3);
                repeat(0.008,3,1,true);

                suicide("mn",0.1, 0,2, 8,2);
                mirror(0.005);
                suicide("mn",0.03, 3,0, 3,3);
                mirror(0.001);
                repeat(0.01,5,4);

                missile("sn",0.15, 0,3, 7,4, 12,7);
                repeat(0.02,9,1,true);
                laser("ss",0,4,0);
                insert(-6);
                laser("ss",0,8,0);
                insert(-2);
                suicide("mn",0.05, 1,0, 4,4);
                repeat(0.01,3,1,true);

                laser("ss",0.12,0,3);
                laser("ss",0.005,12,3);
                laser("ss",0.01,0,6);
                laser("ss",0.005,12,6);

                missile("sn",0.18, 2,0, 2,6, 0,7);
                missile("sn",0, 3,0, 3,6, 12,9);
                repeat(0.03,4,2,true);
                suicide("mn",0.04, 0,3, 4,5);
                repeat(0.005,3,1,true);

                laser("sn",0.13,0,3);
                laser("sn",0.005,12,3);
                laser("sn",0.01,0,6);
                laser("sn",0.005,12,6);
                suicide("mn",0.025, 2,12, 3,4);
                repeat(0.005,7,1,true);

                suicide("mn",0.2, 0,7, 3,6);
                mirror();
                suicide("mn",0.01,0,2, 6,3);
                mirror();
                suicide("mn",0.01,0,4, 4,3);
                mirror();
                suicide("mn",0.01,5,0, 5,1);
                mirror();

                missile("sn",0.12, 3,0, 6,7, 12,8);
                repeat(0.02,11,1,true);
                laser("ss",0,0,6);
                insert(-7);
                laser("ss",0,12,6);
                insert(-3);
                laser("mn",0,6,0);
                suicide("mn",0.03,0,2, 6,3);
                repeat(0.01,3,1,true);

                break;

            case 2:
                missile("sn",0.1, 6,0, 6,5, 0,9);
                repeat(0.01,5,1,true);

                missile("sn",0.1, 0,1, 5,4, 0,8);
                mirror();
                repeat(0.03, 3,2,true);
                laser("sn", 0,0,6);
                insert(-6);
                laser("sn",0,12,6);
                insert(-4);
                laser("mn",0,6,0);

                missile("sn",0.17, 3,0, 3,4, 12,11);
                missile("sn",0, 4,0, 5,4, 8,0);
                repeat(0.025,3,2);
                laser("sn",0.01, 2,0);
                mirror();
                missile("sn",0.09, 12,3, 8,3, 1,12);
                missile("sn",0, 12,4, 8,5, 12,8);
                repeat(0.025,3,2);
                laser("sn",0.01,0,6);
                mirror();
                suicide("mn",0.09, 0,3, 3,3);
                repeat(0.01,3);

                laser("sn",0.13, 0,3);
                mirror();
                laser("mn",0, 6,0);
                laser("sn",0.01, 0,6);
                mirror();

                missile("sn",0.18, 2,0, 2,7, 2,12);
                offset(0,1,0);
                repeat(0.015,1,2);
                repeat(0.05,1,4,true);
                laser("mn",0,0,4);
                missile("sn",0.05, 0,2, 7,2, 12,2);
                offset(0,0,1);
                repeat(0.015,1,2);
                laser("mn",0,12,4);
                missile("sn",0.05, 12,5, 5,5, 0,5);
                offset(0,0,1);
                repeat(0.015,1,2);

                missile("sn",0.2, 0,12, 4,4, 12,9);
                repeat(0.006,3,1,true);
                laser("mn",0.01,4,0);
                mirror(0.01);
                suicide("mn",0.05, 0,3, 3,3);
                repeat(0.015,7,1,true);

                laser("mn",0.15, 0,7);
                mirror();
                laser("sn",0.015, 0,4);
                mirror();
                laser("mn",0.02, 3,0);
                mirror();
                laser("sn",0.015, 5,0);
                mirror();

                missile("sn",0.2, 6,0, 6,4, 0,0);
                mirror(0.027);
                missile("sn",0.027, 6,0, 6,6, 0,9);
                mirror(0.028);
                repeat(0.028,4,4);
                laser("sn",0,0,3);
                insert(-18);
                laser("sn",0,12,3);
                insert(-15);
                laser("sn",0,0,6);
                insert(-12);
                laser("sn",0,12,6);
                insert(-9);
                laser("mn",0,3,0);
                insert(-6);
                laser("mn",0,9,0);
                insert(-3);
                suicide("ms",0.08, 0,12, 3,4);
                repeat(0.015,7,1,true);

                break;
            case 3:
                missile("sn",0.1, 0,3, 3,5, 8,0);
                repeat(0.02,5,1,true);
                laser("ss",0.02,5,0);
                mirror(0.02);
                suicide("mn",0.02, 0,2, 6,3);
                mirror(0.02);
                suicide("mn",0.02, 0,4, 6,5);
                mirror(0.02);
                repeat(0.02,1,4);
                laser("ms",0.02,0,2);
                mirror(0.02);

                boss(0.2,0);
                break;
            case 4:
                missile("sn",0.05, 5,0, 5,6, 4,12);
                mirror();

                missile("sn",0.15, 0,3, 7,3, 12,3);
                missile("sn",0.01, 12,6, 5,6, 0,6);
                repeat(0.01,3,2);
                laser("ss",0.03,3,0);
                mirror();
                laser("ss",0.02,0,9);
                mirror();

                missile("sn",0.25,6,0,6,5,0,10);
                repeat(0.015,7,1,true);
                laser("ss",0.015,0,3);
                mirror();
                missile("sn",0.015,1,0,5,4,12,5);
                repeat(0.015,5,1,true);
                laser("ss",0.02,3,0);
                mirror();

                missile("sn",0.2,5,0,5,6,1,12);
                mirror();
                repeat(0.03,3,2);
                laser("ms",0.03,0,9);
                mirror();

                laser("ms",0.25,6,0);
                laser("ss",0.01,0,3);
                mirror(0.01);
                offset(0.02,0,3);
                mirror(0.02);

                suicide("sn",0.25,1.5,0,1.5,6);
                offset(0,3);
                offset(0,3);
                offset(0,3);
                suicide("sn",0.028,3,0,3,6);
                offset(0,3);
                offset(0,3);
                repeat(0.028,3,7);

                missile("sn",0.25,0,4,3,6,6,0);
                repeat(0.03,11,1,true);
                laser("ss",0,2,0);
                insert(-9);
                laser("ss",0,4,0);
                insert(-6);
                laser("ss",0,10,0);
                insert(-3);
                laser("ss",0,8,0);

                missile("sn",0.25,12,2,7,3,0,2);
                missile("sn",0.015,1,12,5,6,12,8);
                repeat(0.03,5,2);
                laser("ms",0,0,6);
                insert(-8);
                laser("ms",0,12,6);
                insert(-6);
                laser("ms",0,6,0);
                insert(-4);
                break;

            case 5:
                missile("ss",0.05,5,0,5,4,2,0);
                mirror();
                repeat(0.03,1,2);
                missile("sn",0.03,5,0,5,4,2,0);
                mirror();
                repeat(0.03,1,2);

                missile("ss",0.1,6,0,6,5,0,10);
                repeat(0.06,5,1,true);
                missile("sn",0,1,0,2,4,3,0);
                missile("sn",0,2,0,3,4,4,0);
                repeat(0,1,2);
                insert(-10);
                insert(-9);
                insert(-7);
                insert(-6);
                missile("sn",0,12,1,8,2,12,3);
                missile("sn",0,12,2,8,3,12,4);
                repeat(0,1,2);
                insert(-6);
                insert(-5);
                insert(-3);
                insert(-2);

                missile("ss",0.15,9,0,9,6,9,12);
                offset(0,1);
                missile("mn",0.02,2,0,2,6,2,12);
                offset(0,2);
                missile("sn",0.04,0,2,5,4,12,7);
                repeat(0.01,3,1,true);

                missile("sn",0.15,1,0,1,6,1,12);
                offset(0,1);
                repeat(0.04,1,2);
                missile("mn",0.02,10,0,10,6,10,12);
                repeat(0.03,3,3);
                laser("ms",0,5,0);
                mirror();
                insert(-4);

                suicide("sn",0.32,1.5,0,1.5,6);
                offset(0,3);
                offset(0,3);
                offset(0,3);
                repeat(0.035,7,4,true);

                missile("ss",0.3,6,0,6,5,0,9);
                missile("mn",0.01,0,8,4,5,5,0);
                mirror();
                repeat(0.07,3,3,true);
                laser("ms",0,2,0);
                insert(-9);
                laser("ms",0,10,0);
                insert(-6);

                missile("mn",0.2,2,0,2,5,2,12);
                missile("ss",0,3,0,3.5,5,7,0);
                repeat(0.05,3,2);
                missile("mn",0.03,12,2,5,5,2,12);
                missile("ss",0,12,3.5,7,5,12,7);
                repeat(0.05,3,2);
                suicide("ms",0.06,0,12,3,4);
                repeat(0.015,3,1,true);
                break;

            case 6:
                missile("mn",0.05,2,0,2,7,2,12);
                mirror();
                missile("mn",0,4,0,4,6,2,12);
                mirror();

                missile("ms",0.15,1,0,5,4,11,12);
                mirror();
                missile("mn",0.01,0,10,3,6,0,3);
                repeat(0.01,3,1,true);

                missile("ms",0.2,0,4,7,4,12,4);
                offset(0.02,0,2);
                repeat(0.03,3,2);
                laser("ms",0.04,0,2);
                laser("ms",0.04,0,8);

                missile("ms",0.25,2,0,2,7,2,12);
                missile("ms",0.01,3,0,5,5,12,7);
                repeat(0.06,1,2);
                missile("mn",0.05,2,0,2,7,2,12);
                missile("mn",0.01,3,0,5,5,12,7);
                repeat(0.06,3,2);
                laser("ss",0,12,3);
                insert(-7);
                laser("ss",0,8,0);
                insert(-4);
                laser("ss",0,6,0);
                insert(-1);

                missile("ms",0.2,5,0,5,6,0,10);
                mirror();
                missile("mn",0.02,3,0,3,4,0,7);
                repeat(0.035,11,1,true);
                laser("ms",0,0,3);
                insert(-8);
                laser("ms",0,12,3);
                insert(-6);

                missile("ms",0.2,3,0,3,7,2,12);
                mirror();
                missile("ms",0.04,4,0,4,6,2,12);
                mirror();
                missile("ms",0.04,5,0,5,5,2,12);
                mirror();
                missile("mn",0.07,3,0,3,7,2,12);
                mirror();
                missile("mn",0.04,4,0,4,6,2,12);
                mirror();
                missile("mn",0.04,5,0,5,5,2,12);
                mirror();

                laser("mn",0.2,0,3);
                mirror();
                laser("sn",0.01,0,6);
                mirror();
                laser("ln",0,6,0);

                suicide("ss",0.35, 1.4,0, 1.4,6);
                offset(0,3.8);
                offset(0,3.8);
                repeat(0.05,4,3,true);
                break;

            case 7:
                suicide("sn",0.1,1.5,0,1.5,6);
                offset(0,3);
                offset(0,3);
                offset(0,3);
                laser("ms",0.04,3,0);
                mirror();

                suicide("sn",0.09,1.5,0,1.5,6);
                offset(0,3);
                offset(0,3);
                offset(0,3);
                missile("mn",0.02,0,2,6,2,12,2);
                missile("mn",0.02,12,4,6,4,0,4);

                suicide("sn",0.09,1.5,0,1.5,6);
                offset(0,3);
                offset(0,3);
                offset(0,3);
                missile("ss",0.02,3,0,3,6,2,12);
                mirror(0.02);

                suicide("ss",0.09,1.5,0,1.5,6);
                offset(0,3);
                offset(0,3);
                offset(0,3);
                missile("ms",0.02,0,2,3,6,0,9);
                mirror(0.02);

                boss(0.2,1);
                break;

            case 8:
                missile("sn",0.07, 0,2, 4,2, 0,7);
                repeat(0.008,5,1,true);

                missile("sn",0.05, 0,3, 5,4, 12,8);
                repeat(0.015,9,1,true);
                laser("ln",0,3,0);
                insert(-7);
                laser("ln",0,9,0);
                insert(-5);
                laser("ln",0,6,0);
                missile("sn",0.02, 1,12, 4,6, 12,2);
                repeat(0.02,5,1,true);

                missile("sn",0.17, 12,12, 8,3, 2,6, 12,7);
                repeat(0.01,12,1);
                laser("ln",0,3,0);
                insert(-10);
                laser("ln",0,9,0);
                insert(-5);
                suicide("mn",0.05,0,0, 3,4);
                repeat(0.01,3);

                laser("ms",0.23, 4,0);
                mirror(0.01);
                laser("ls",0.01, 6,0);
                laser("ss",0.04, 0,4);
                mirror(0.022);
                offset(0.022,0,3);
                mirror(0.022);

                missile("ss",0.22, 0,12, 4,3, 10,6, 0,6);
                repeat(0.03,7,1);
                laser("ls",0,5,0);
                insert(-5);
                laser("ls",0,7,0);
                insert(-1);
                suicide("mn",0.13,12,0, 9,4);
                repeat(0.01,3);

                laser("ls",0.3,2,0);
                offset(0.01,2);
                offset(0.01,2);
                offset(0.01,2);
                offset(0.01,2);
                suicide("mn",0.13,0,12, 2,5);
                repeat(0.01,7,1,true);
                break;
            case 9:
                missile("ln",0.05, -1,-1, 5,6, 13,13);
                mirror(0.01);

                missile("mn",0.1, 0,1, 9,3, 0,6);
                missile("mn",0.013, 12,8, 3,6, 12,3);
                repeat(0.013,3,2);
                missile("ln",0.03, -1,3, 6,5, 13,3);
                missile("ln",0, 13,5, 6,3, -1,4);
                laser("lb",0.06, 4.5,0);
                laser("lb",0.01, 7.5,0);

                missile("ln",0.3, 3,-1, 3,5, 3,13);
                mirror();
                missile("ln",0.04, -1,1, 6,3, 9,13);
                mirror();
                laser("lt",0.04, 0,5);
                mirror();
                missile("mn",0.14, 0,1, 5,5, 12,1);
                missile("mn",0, 0,9, 6,6, 0,3);
                missile("mn",0, 12,9, 7,5, 0,9);
                missile("mn",0, 12,1, 6,4, 12,10);

                missile("ss",0.14, 2,12, 2,3, 9,2.5, 10,12);
                repeat(0.01,7);
                missile("ls",0.07, -1,6, 8,6, 13,6);
                offset(0,0,2);
                laser("lb",0.05, 0,7);

                ship("lmt", 0.1, 3,0,[move(3,3),halt(15),halt(10,true),move(0,3)]);
                mirror();
                ship("lmt", 0.05, 5,0,[move(5,3),halt(15),halt(10,true),move(0,3)]);
                mirror();
                missile("mn",0.06, 0,12, 4,7, 4,0);
                mirror();
                missile("mn",0.03, 6,0, 6,5, 1,12);
                mirror();

                missile("ms",0.23, 2,0, 2,6, 6,4, 10,12);
                mirror(0.018);
                missile("mn",0.018, 2,0, 2,6, 6,4, 10,12);
                repeat(0.018,5,1,true);
                missile("ln",0.04, -1,-1, 2,3, -1,13);
                mirror();
                laser("lb",0.05,6,0);

                laser("lb",0.35, 4,0);
                mirror();
                stealth(0.05, 3,5, 0,12);
                mirror(0.01);
                stealth(0.01, 6,4, 6,0);
                break;
            case 10:
                missile("lb",0.03, 2,-1, 2,13);
                mirror();
                missile("lb",0.05,-1,2,13,2);
                mirror();
                laser("lb",0.05,0,5);
                mirror();
                missile("lb",0.05, 3,-1, 3,6, 1,13);
                mirror();
                missile("ss",0.03, 6,0, 6,5, 1,0);
                repeat(0.03,3,1,true);

                missile("sn",0.1, 2,12, 2,3, 8,4, 10,12);
                repeat(0.01,13,1,true);
                laser("ss",0.02,5,0);
                mirror();
                missile("sn",0.05, 2,0, 2,7, 2,12);
                repeat(0.01,5,1,true);
                stealth(0.05, 6,7,6,0);

                missile("sn",0.1, 6,0, 6,5, 3,2, 12,7);
                repeat(0.014,9);
                missile("lb",0.03, 2,-1, 2,7, 2,13);
                mirror(0.02);
                laser("ss",0.04, 4,0);
                mirror(0.02);
                stealth(0.07, 5,3, 0,6);
                stealth(0.02, 8,5, 12,7);

                missile("lb",0.2, 2,-1, 5,4, 2,13);
                mirror();
                laser("sn",0.02, 0,7);
                mirror(0.015);
                offset(0.015,0,-2);
                mirror(0.015);
                offset(0.015,0,-2);
                mirror(0.015);
                laser("sn",0.015, 3,0);
                mirror(0.015);
                laser("sn",0.015, 7,0);
                mirror(0.015);

                missile("mn",0.2, 0,2, 6,4, 12,2);
                repeat(0.02,3);
                missile("sn",0.01, 12,3, 6,5, 0,3);
                clone(0.01,-2);
                repeat(0.01,4,2);
                clone(0.01,-2);
                repeat(0.02,3);
                laser("lb",0.01,0,6);
                mirror(0.01);
                stealth(0.05,5,5, 5,0);
                mirror(0.03);

                suicide("sn",0.3, 1.4,0, 1.4,6);
                offset(0,3.8);
                offset(0,3.8);
                repeat(0.03,2,3,true);
                missile("sn",0, 0,6, 6,7, 12,10);
                repeat(0.01,7);
                missile("mn",0, 2,0, 2,5, 1,12);
                mirror();
                missile("mn",0.06, 4,0, 4,5, 1,12);
                mirror();
                missile("ln",0, 13,5, 6,7, -1,10);

                laser("ms",0.18, 4,0);
                mirror(0.01);
                laser("ls",0.01, 6,0);
                laser("ss",0.06, 0,4);
                mirror(0.02);
                offset(0.02,0,3);
                mirror(0.02);
                stealth(0.05, 4,5, 0,12);
                mirror(0.02);

                suicide("mn",0.2, 3,0, 5,2);
                mirror();
                repeat(0.06,1,2);
                suicide("mn",0, 0,0, 2,2);
                mirror();
                repeat(0.06,1,4);
                suicide("mn",0, 0,7, 3,6);
                mirror();
                repeat(0.06,1,6);
                suicide("ms",0, 5,0, 5,3);
                mirror();
                break;
            case 11:
                missile("ln",0.1, 1,-1, 4,6, -1,13);
                mirror();
                laser("ln",0.06, 0,5);
                mirror();

                missile("ls",0.09, -1,2, 6,3.5, 13,5);
                missile("ls",0, 13,8, 6,6.5, -1,5);
                laser("ls",0.06, 3,0);
                mirror();

                missile("lt",0.09, 5,-1, 5,4, -1,8);
                mirror();
                laser("lt",0.09, 0,3);
                mirror();

                missile("lb",0.09, -1,1, 4,2, -1,12);
                mirror();
                laser("lb",0.09, 5,0);
                mirror();

                stealth(0.2, 3,4, 0,5);
                mirror(0.024);
                stealth(0.024, 6,3, 6,0, true);

                boss(0.02,2);
                break;

            default: throw new ArgumentOutOfRangeException(nameof(lvl), $"Level number OOB: {lvl}");
        }
        return array;
    }

    /// <summary>The six suicide ships boss 0 launches from its hangar, relative to the boss's top-left corner.</summary>
    public static List<SpawnEvent> boss0Suicides()
    {
        array = new List<ILevelEvent>();
        dist = 0;
        ship("msn", 0, 1.6, 3, [halt(16), fire()]);
        ship("msn", 0, 2.2, 3.4, [halt(13), fire()]);
        ship("msn", 0, 2.8, 3.7, [halt(10), fire()]);
        ship("msn", 0, 6.8, 3.7, [halt(10), fire()]);
        ship("msn", 0, 7.4, 3.4, [halt(13), fire()]);
        ship("msn", 0, 8, 3, [halt(16), fire()]);
        return array.Cast<SpawnEvent>().ToList();
    }

    /// <summary>Escorts that keep coming while a boss fight is on, starting from the current distance.</summary>
    public static List<ILevelEvent> bossLoop()
    {
        array = new List<ILevelEvent>();
        dist = G.Model.Distance;

        switch (G.Model.Boss!.Index)
        {
            case 0:
                missile("sn",0.3, 0,4, 7,6, 12,6);
                missile("sn",0.015, 12,3, 5,5, 0,5);
                repeat(0.015,2,2);

                laser("ss",0.2, 0,9);
                mirror(0.01);
                break;
            case 1:
                missile("mn",0.25, 0,3+G.Rand()*3, 5,6, 12,7);
                mirror();
                repeat(0.03,1,2);

                laser("ms",0.35,0,8);
                mirror(0.01);
                break;
            case 2:
                laser("ms",0.3,0,6+G.Rand()*3);
                mirror();
                missile("mn",0.3, 0,0, 2,4, 5,6, 12,10);
                repeat(0.02,5,1,true);
                stealth(0.3, 2+G.Rand()*8,5+G.Rand()*2, (G.Rand()>0.5?0:12),5);
                break;
            default: throw new InvalidOperationException($"Level out of bounds: {level}");
        }
        return array;
    }

    static void setDist(double d)
    {
        if (d >= 0.1) d *= mult;
        dist += d;
    }

    static void loopBack(int j = 0)
    {
        if (j < 0) j = array.Count + j;
        array.Add(new LevelEvent(dist, LevelEvent.Kind.Loop, j));
    }

    static LevelEvent wait(double d = 0)
    {
        setDist(d);
        var m = new LevelEvent(dist, LevelEvent.Kind.Wait, d);
        array.Add(m);
        return m;
    }

    static void jump(double j = 0) => array.Add(new LevelEvent(dist, LevelEvent.Kind.Jump, j));

    // Grid positions are doubles so scripts can use 1.5 or 3+G.Rand()*3 as they did in AS3.
    static void laser(string type, double d, double x, double y, int speed = 0)
    {
        ObjectCommand[] commands;
        if (x == 0) commands = [move(2, y), halt(10 - speed), halt(20 - speed, true), move(-2, y)];
        else if (x == 12) commands = [move(10, y), halt(10 - speed), halt(20 - speed, true), move(14, y)];
        else if (y == 0) commands = [move(x, 2), halt(10 - speed), halt(20 - speed, true), move(x, -2)];
        else if (y == 12) commands = [move(x, 10), halt(10 - speed), halt(20 - speed, true), move(x, 14)];
        else throw new ArgumentException($"Invalid spawn location: {x} {y}");
        ship($"{type[0]}l{type[1]}", d, x, y, commands);
    }

    static void suicide(string type, double d, double x, double y, double xTo, double yTo) =>
        ship($"{type[0]}s{type[1]}", d, x, y, [move(xTo, yTo), fire()]);

    static void stealth(double d, double x, double y, double xTo, double yTo, bool shield = false) =>
        ship(shield ? "xms" : "xmn", d, x, y,
            [halt(8), halt(10, true), halt(10, true), halt(10, true), halt(10, true), halt(5, true), move(xTo, yTo)]);

    static void missile(string type, double d, double x, double y, double xTo1, double yTo1,
        double xTo2 = -1, double yTo2 = -1, double xTo3 = -1, double yTo3 = -1) =>
        ship($"{type[0]}m{type[1]}", d, x, y,
        [
            move(xTo1, yTo1),
            move(xTo2, yTo2, xTo3 == -1),
            xTo3 != -1 ? move(xTo3, yTo3, true) : halt(1),
        ]);

    /// <summary>Move the last event back to position j, at that event's distance.</summary>
    static void insert(int j)
    {
        if (j < 0) j = array.Count + j;
        var last = array[^1];
        last.Distance = array[j].Distance; // before removing it: insert(-1) leaves it where it is
        array.RemoveAt(array.Count - 1);
        array.Insert(j, last);
    }

    static void ship(string type, double d, double x, double y, ObjectCommand[]? commands = null)
    {
        setDist(d);
        array.Add(new SpawnEvent(dist, type, (float)(x * GridX), (float)(y * GridY), commands));
    }

    static void boss(double d, int type)
    {
        setDist(d);
        array.Add(new BossEvent(dist, type));
    }

    static SpawnEvent At(int j) => (SpawnEvent)array[j < 0 ? array.Count + j : j];

    static void clone(double d = 0, int j = -1)
    {
        var src = At(j);
        setDist(d);
        array.Add(new SpawnEvent(dist, src.Type, src.X, src.Y, src.Commands.Select(c => c.Clone()).ToArray()));
    }

    static void mirror(double d = 0, int j = -1)
    {
        var src = At(j);
        setDist(d);
        array.Add(new SpawnEvent(dist, src.Type, G.ViewWidth - src.X, src.Y, src.Commands.Select(c => c.Mirror()).ToArray()));
    }

    static void offset(double d = 0, double xOff = 0, double yOff = 0, int j = -1)
    {
        var src = At(j);
        setDist(d);
        float x = (float)(xOff * GridX), y = (float)(yOff * GridY);
        array.Add(new SpawnEvent(dist, src.Type, src.X + x, src.Y + y,
            src.Commands.Select(c => c.Offset(x, y)).ToArray()));
    }

    /// <summary>Repeat the last repeatC spawns repeatN times (mirrored if reverse), keeping their spacing.</summary>
    static void repeat(double d, int repeatN = 1, int repeatC = 1, bool reverse = false)
    {
        for (int k = 0; k < repeatN * repeatC; k++)
        {
            double step = k % repeatC == 0
                ? d
                : array[^repeatC].Distance - array[^(repeatC + 1)].Distance;
            if (reverse) mirror(step, -repeatC);
            else clone(step, -repeatC);
        }
    }

    static ObjectCommand move(double x, double y, bool fire = false) =>
        new((float)(x * GridX), (float)(y * GridY), 0, true, fire);

    static ObjectCommand halt(int timer, bool fire = false) => new(G.ViewWidth / 2, G.ViewHeight - 100, timer, false, fire);

    static ObjectCommand fire() => new(0, 0, 1, false, true);
}
