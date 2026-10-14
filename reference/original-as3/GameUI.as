package{
	import flash.display.Sprite;
	import flash.text.TextField;
	import flash.text.TextFormat;
	import flash.geom.Point;
	import flash.display.Bitmap;
	import flash.filters.GlowFilter;
	
	public class GameUI extends Sprite{
		static const VIEW_WIDTH:int=600;
		static const VIEW_HEIGHT:int=550;
		static const UI_WIDTH:int=550;
		static const UI_HEIGHT:int=500;
		var dash:Bitmap=new Bitmap(SpriteSheets.dash[0]);
		var dashC:int=0;
		var diffText:TextField=new TextField();
		var scoreText:TextField=new TextField();
		var typeText:TextField=new TextField();
		var gameV:Sprite=new Sprite();
		var bossV:Sprite=new Sprite();
		var pauseUI:PauseUI=new PauseUI();
		var popWarning:PopWarning=new PopWarning();
		var tutBox:PopBox;
		
		public function init(){
			mouseChildren=false;
			gameV.x=bossV.x=-25;
			gameV.y=bossV.y=-25;
			dash.width=550;
			dash.scaleY=dash.scaleX;
			popWarning.x=UI_WIDTH/2;
			popWarning.y=UI_HEIGHT-20;
			
			diffText.defaultTextFormat=new TextFormat(Facade.GAME_FONT,15,0xffffff,true);
			scoreText.defaultTextFormat=new TextFormat(Facade.GAME_FONT,18,0xffffff,true);
			typeText.defaultTextFormat=new TextFormat(Facade.GAME_FONT,15,0xffffff,true);
			diffText.filters=[new GlowFilter(Facade.GREEN,1,4,4,2,1,true)];
			scoreText.filters=[new GlowFilter(Facade.GREEN,1,4,4,2,1,true)];
			typeText.filters=[new GlowFilter(Facade.RED,1,4,4,2,1,true)];
			diffText.width=50;
			scoreText.width=50;
			typeText.y=UI_HEIGHT+21;
			diffText.x=UI_WIDTH-61;
			diffText.y=UI_HEIGHT-17;
			scoreText.y=UI_HEIGHT-12;
			scoreText.x=10;
			var _scoreT:TextField=new TextField;
			_scoreT.defaultTextFormat=new TextFormat(Facade.TINY_FONT,10,0xffffff,true);
			_scoreT.x=17;
			_scoreT.y=UI_HEIGHT-22;
			_scoreT.text=StringData.SCORE;
			_scoreT.filters=[new GlowFilter(Facade.GREEN,1,4,4,2,1,true)];
			
			diffText.autoSize=scoreText.autoSize="right";
			typeText.autoSize="left";
			Facade.gameM.hull.x=169;
			Facade.gameM.hull.y=UI_HEIGHT+25;
			
			addChild(bossV);
			addChild(gameV);
			addChild(dash);
			addChild(Facade.gameM.hull);
			addChild(diffText);
			addChild(typeText);
			addChild(scoreText);
			addChild(_scoreT);
		}
		
		public function onTick(){
			var _per:int=Facade.gameM.distance/Facade.gameM.maxDistance*100;
			if (_per>100){
				diffText.text="100%";
			}else{
				diffText.text=String(_per)+"%";
			}
			if (dashC==0){
				if (Math.random()<0.01){
					dashC=Math.ceil(Math.random()*5)*5;
				}
			}else{
				dashC-=1;
				if (dashC==0){
					dash.bitmapData=SpriteSheets.dash[0];
				}else if (dashC%3==0){
					dash.bitmapData=SpriteSheets.dash[Math.ceil(Math.random()*5)];
				}
			}
		}
		
		public function showScore(i:int){
			scoreText.text=String(i);
		}
		
		public function resetLetter(){
			typeText.text="";
		}
		
		public function addLetter(s:String){
			typeText.appendText(s);
			if (typeText.text.length>20){
				new PopScore(new Point(typeText.x+5,typeText.y-5),-1);
				typeText.text=typeText.text.substring(1);
			}
		}
		
		public function removeLetter(i:int=1){
			if (typeText.text.length>0){
				Facade.gameM.perfect=false;
				typeText.text=typeText.text.substring(0,typeText.length-1);
				new PopScore(new Point(typeText.x+5,typeText.y-5),-1);
			}
		}
				
		public function warning(i:int){
			popWarning.add(i);
		}
	}
}