/**********************************
TO DO:
	GAME ENGINE
 - Dodging Left/Right (Arrows which flash on the map when needed; only for bosses)
 - Tractor Beam to collect things that fall
 - Mega Bomb which kills everything
 
	DESIGN
 - Organized Levels
 - Boss AI
 - space-mines
 
 	INTERFACE
 - Type to navigate the menu
 - Difficulty adjusted based on Typing Test
 - Advanced final score calculating algorythm
 
 	GRAPHICS
 - all graphics stuff is needed
***********************************/

package{
	import flash.display.Stage;
	import net.jpauclair.FlashPreloadProfiler;
	
	public class Facade{
		public static const WORD_FONT:String="Consolas",
							SPACE_FONT:String="Quixley LET",
							TITLE_FONT:String="Rocket Script",
							TINY_FONT:String="Lily UPC",
							ROSE_FONT:String="RosewoodStd-Regular",
							GAME_FONT:String="Arial",
							GOLD:uint=0xffcc33,
							GOLD_OVER:uint=0xffee55,
							DASH_GOLD:uint=0xe1e55b,
							BLUE:uint=0x1b4e92,//0x0099ff,
							BLUE_OVER:uint=0x55ff99,
							MENU_BLUE:uint=0x000022,
							BORDER_BLUE:uint=0xccccff,
							RED:uint=0xff5555,
							GREEN:uint=0x55ff55,
							
							BRONZE:uint=0x774422,
							SILVER:uint=0xccccdd,
							
							EASY:int=25,
							NORMAL:int=40,
							HARD:int=65,
							EXTREME:int=75,
							INSANE:int=90;
							
		static var wordList:WordList=new WordList(),
				gameM:GameModel,
				gameUI:GameUI,
				menuUI:MenuUI,
				goUI:GameOverUI,
				vicUI:VictoryUI,
				scoreUI:ScoreUI,
				typingTest:TypingTest,
				gameC:GameControl=new GameControl(),
				actionC:ActionControl=new ActionControl(),
				keyboardC:KeyboardControl=new KeyboardControl(),
				spriteC:SpriteSheets=new SpriteSheets(),
				soundC:SoundControl=new SoundControl(),
				gameStrings:GameStrings=new GameStrings(),
				transition:Transition=new Transition(),
				menuB:MenuBt,
				muteB:MuteB,
				pauseB:PauseB,
				stage:Stage;
		
		public static var tempBox:MenuBox;

		public static function initialize(s:Stage){
			stage=s;
			//stage.addChild(new FlashPreloadProfiler());
			stage.stageFocusRect=false;
			s.focus=s;
			gameUI=new GameUI;
			menuUI=new MenuUI;
			goUI=new GameOverUI;
			scoreUI=new ScoreUI();
			vicUI=new VictoryUI();
			gameM=new GameModel();
			typingTest=new TypingTest;
			gameM.init();
			gameUI.init();
			gameC.init();
			actionC.init();
			spriteC.init();
			keyboardC.init(s);
			soundC.init();
			tempBox=new MenuBox("",50,50,450,450,40);
			muteB=new MuteB;
			pauseB=new PauseB;
			s.addChild(gameM.starfield);
			s.addChild(menuUI);
			menuUI.navIn();
			pauseB.scaleX=pauseB.scaleY=muteB.scaleX=muteB.scaleY=0.8;
			pauseB.x=435;
			muteB.x=505;
			pauseB.y=muteB.y=522;
			s.addChild(muteB);
		}
				
		public static function highScoreSubmit(){
			tempBox.text="This will submit the high score.";
			stage.addChild(tempBox);
		}
		
		public static function highScoreDisplay(){
			tempBox.text="This will display the high score.";
			stage.addChild(tempBox);
		}
		
		public static function goSponsor(){
			if (gameM.achieve[5]==0){
				gameM.achieve[5]=1;
				new PopAchieve(StringData.EXPLORER,1);
			}
			tempBox.text="This will lead to the sponsor's website.";
			stage.addChild(tempBox);
		}
	}
}