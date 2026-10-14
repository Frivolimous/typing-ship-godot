package{
	public class GameStrings{
		public static var END_GAME:String="End Game",
						NEW_GAME:String="New Game",
						MENU_TEXT:String="Menu Text";
						
		public function display(s:String):String{
			switch (s){
				case END_GAME:return "Your ship was destroyed!\n\n You travelled "+String(Facade.gameM.distance).substring(0,5)+" U.\n\nYou ended the game with "+Facade.gameM.score+" points.\n Your high score is "+Facade.gameM.highscore+".\n\n Hit the <Spacebar> to begin again.";
				case MENU_TEXT:return "Welcome to TYPING SPACESHIP!\n\nINSTRUCTIONS:\nType in all the words that you see on the screen.\n\nHit the <spacebar> to BEGIN.";
				default:return "";
			}
		}
	}
}