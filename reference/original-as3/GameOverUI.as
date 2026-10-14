package  {
	import flash.display.Sprite;
	import flash.text.TextField;
	import flash.text.TextFormat;
	import flash.display.Bitmap;
	import flash.filters.GlowFilter;

	public class GameOverUI extends Sprite{
		var loseBack:Bitmap=new Bitmap(new LoseB(510,425));
		var winBack:Bitmap=new Bitmap(new WinB(510,425));
		var title:TextField=new TextField();
		var score:TextField=new TextField();
		var scroll:TextField=new TextField();
		var menuB:UIButton=new UIButton(StringData.MENU,goMenu,Facade.GOLD,Facade.GOLD_OVER,true);
		var submitB:UIButton=new UIButton(StringData.HS_SUBMIT,goSubmit,Facade.GOLD,Facade.GOLD_OVER,true);
		var moreB:UIButton=new UIButton(StringData.MORE_GAMES,goMore,Facade.GOLD,Facade.GOLD_OVER,true);
		
		public function GameOverUI() {
			winBack.height=loseBack.height=550;
			winBack.scaleX=winBack.scaleY;
			loseBack.scaleX=loseBack.scaleY;
			title.defaultTextFormat=new TextFormat(Facade.SPACE_FONT,50,0xffffff,true);
			title.x=30;
			title.y=5;
			title.width=500;
			title.height=200;
			title.selectable=false;
			title.wordWrap=true;
			title.filters=[new GlowFilter(Facade.GOLD,1,5,5,2,1,true),new GlowFilter(Facade.GOLD,1,5,5,2,1)];
			addChild(title);
			score.defaultTextFormat=new TextFormat(Facade.SPACE_FONT,50,0xffffff,true);
			score.multiline=true;
			score.x=60;
			score.y=200;
			score.width=500;
			score.height=200;
			score.selectable=false;
			score.filters=[new GlowFilter(Facade.GOLD,1,4,4,2,1,true),new GlowFilter(Facade.GOLD,1,4,4,2,1)];
			addChild(score);
			scroll.defaultTextFormat=new TextFormat(Facade.SPACE_FONT,22,0xffffff,true);
			scroll.wordWrap=true;
			scroll.selectable=false;
			scroll.filters=[new GlowFilter(Facade.GOLD,1,4,4,2,1,true)];
			scroll.width=500;
			scroll.height=300;
			scroll.y=200;
			addChild(scroll);
			menuB.scaleX=menuB.scaleY=submitB.scaleX=submitB.scaleY=moreB.scaleX=moreB.scaleY=1.5;
			menuB.x=70;
			submitB.x=moreB.x=270;
			menuB.y=420;
			submitB.y=420;
			moreB.y=480;
			addChild(menuB);
			addChild(submitB);
			addChild(moreB);
		}
		
		public function scrollEnd(){
			score.x=5;
			score.y=150;
			scroll.width=290;
			scroll.x=200;
			scroll.y=250;
		}
		
		public function addWin(){
			if (contains(loseBack)){
				removeChild(loseBack);
			}
			addChildAt(winBack,0);
			title.text=StringData.WON_GAME;
			score.text=StringData.FINAL_SCORE+"\n        "+Facade.gameM.highscore;
			scrollEnd();
		}
		
		public function addLose(){
			if (contains(winBack)){
				removeChild(winBack);
			}
			addChildAt(loseBack,0);
			title.text=StringData.LOSE_GAME;
			score.text=StringData.LEVEL_SCORE+"\n        "+Facade.gameM.score;
			scrollEnd();			
		}
		
		public function goMenu(){
			Facade.transition.add(0,Facade.menuUI,this);
		}
		
		public function goSubmit(){
			Facade.highScoreSubmit();
		}
		
		public function goMore(){
			Facade.goSponsor();
		}
	}
}
