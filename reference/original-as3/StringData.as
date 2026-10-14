package  {
	
	public class StringData {
							//achievement
		public static const SOLDIER:String="SOLDIER",
							CONQUEROR:String="CONQUEROR",
							PERFECTIONIST:String="PERFECTION",
							RIDDLER:String="RIDDLER",
							EXPLORER:String="EXPLORER",
							DEFENDER:String="DEFENDER",
							
							BRONZE:String="BRONZE",
							SILVER:String="SILVER",
							GOLD:String="GOLD",
							DONE:String="DONE",
							
							//score menu
							LEVEL_SCORE:String="LEVEL SCORE: ",
							DIFF_BONUS:String="DIFFICULTY BONUS x",
							HEALTH_BONUS:String="HEALTH BONUS x",
							HEALTH_PERFECT:String="PERFECT HEALTH x",
							ACC_BONUS:String="ACCURACY BONUS x",
							ACC_PERFECT:String="PERFECT ACCURACY x",
							FINAL_SCORE:String="FINAL SCORE: ",
							SPACEBAR:String="HIT SPACEBAR TO CONTINUE",
							
							//in game
							SCORE:String="SCORE:",
							PAUSED:String="-PAUSED-",
							YES:String="YES",
							NO:String="NO",
							LOW_HEALTH:String="Low Health!",
							
							//diff
							EASY:String="EASY",
							NORMAL:String="NORMAL",
							HARD:String="HARD",
							EXTREME:String="EXTREME",
							INSANE:String="INSANE",
							
							//game over screen
							MENU:String="MENU",
							HS_SUBMIT:String="SUBMIT SCORE",
							MORE_GAMES:String="MORE GAMES",
							WON_GAME:String="Congratulations!  You won the game!",
							LOSE_GAME:String="You have been destroyed!",
							
							//misc. words for testing
							MUTE:String="mute",
							UNMUTE:String="unmute",
							SUPERMAN:String="superman",
							KAMIKAZE:String="kamikaze",
							CHAMELEON:String="chameleon",
							PAUSE:String="pause",
							UNPAUSE:String="unpause",
							
							//other words
							HEALTH:String="HEALTH",
							ACCURACY:String="ACCURACY",
							START_GAME:String="START GAME",
							HIGHSCORE:String="HIGH SCORE",
							ACHIEVE:String="ACHIEVEMENTS",
							CREDITS:String="CREDITS",
							TITLE0:String="MILLENIUM",
							TITLE1:String="TYPER",
							NEW_ACHIEVE:String="New Achievement!",
							SKIP:String="SKIP",
							FINAL_CREDITS:String="As the debris settles in the depths of space a heavy sigh of relief washes across all the people of earth.  The evil emperor has been defeated and the planet freed... an age of peace and prosperity is sure to follow.\n\n\nConcept and Design by:\nYermiyah Hornick\n\n\nProgramming by:\nYermiyah Hornick\n\n\nArt by:\nAvi Kentridge\n\n\nMusic By:\nBinyamin Hornick\n\n\nSound Effects by:\nYermiyah Hornick\nAvi Kentridge\n\n\nThank you for playing!";
		
		public static function getAchieveArray(s:String):Array{
			switch (s){
				case SOLDIER: return ["DESTROY 10 ENEMY SHIPS","DESTROY 1,000 ENEMY SHIPS","DESTROY 20,000 ENEMY SHIPS"];
				case CONQUEROR: return ["DEFEAT THE FIRST BOSS","DEFEAT THE SECOND BOSS","FINISH THE GAME"];
				case PERFECTIONIST: return ["COMPLETE ANY LEVEL KILLING EVERY SHIP","ACHIEVE 5 GOLD ACCURACY MEDALS","FINISH EVERY LEVEL WITH PERFECT ACCURACY"];
				case RIDDLER: return ["IT'S A BIRD, IT'S A PLANE, IT'S...","JAPANESE DEATH PILOT'S SCREAM","NATURE'S MASTER OF DISGUISE"];
				case EXPLORER: return ["VISIT OUR SPONSORS","PAUSE THE GAME WITHOUT CLICKING","KILL SUPERMAN"];
				case DEFENDER: return["COMPLETE ANY LEVEL WITH PERFECT HEALTH","ACHIEVE 5 GOLD HEALTH MEDALS","FINISH EVERY LEVEL WITH PERFECT HEALTH"];
				default: return null;
			}
			/*switch (s){
				case SOLDIER: return ["Destroy 10 Enemy Ships","Destroy 1,000 Enemy Ships","Destroy 20,000 Enemy Ships"];
				case CONQUEROR: return ["Defeat the first boss","Defeat the second boss","Finish the game"];
				case PERFECTIONIST: return ["Complete a level killing every ship","Complete a level killing every ship and with no wrong letters","Achieve 6 Gold Target Medals"];
				case RIDDLER: return ["It's a bird, It's a plane, It's...","Japanese Death Pilot's Scream","Nature's master of disguise"];
				case EXPLORER: return ["Visit our sponsors","Pause the game without clicking","Kill Superman"];
				case DEFENDER: return["Complete any level with Perfect Health","Achieve 5 Gold Health Medals","Finish every level with Perfect Health"];
				default: return null;
			}*/
		}
		
		public static function levelComplete(i:int):String{
			return "Level "+String(i)+" Complete!";
		}
	}
}
