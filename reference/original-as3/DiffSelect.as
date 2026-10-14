package  {
	import flash.display.Sprite;
	import flash.text.TextField;
	import flash.text.TextFormat;
	import flash.filters.GlowFilter;
	
	public class DiffSelect extends Sprite{
		var highscore:TextField=new TextField();
		var level:int=0;
		
		
		public function DiffSelect(){
			graphics.lineStyle(2,Facade.BORDER_BLUE);
			graphics.beginFill(0);
			graphics.drawRect(0,0,70,135);
			highscore.defaultTextFormat=new TextFormat(Facade.SPACE_FONT,16,Facade.GOLD,true);
			highscore.y=3;
			highscore.width=65;
			highscore.height=30;
			highscore.autoSize="right";
			highscore.selectable=false;
			addChild(highscore);
			var _button:BoxButton=new BoxButton(StringData.EASY,Facade.EASY,0x003355,0x88aaff);
			_button.x=5;
			_button.y=30;
			addChild(_button);
			_button=new BoxButton(StringData.NORMAL,Facade.NORMAL,0x005500,0x88ff88);
			_button.x=5;
			_button.y=50;
			addChild(_button);
			_button=new BoxButton(StringData.HARD,Facade.HARD,0x555500,0xffff88);
			_button.x=5;
			_button.y=70;
			addChild(_button);
			_button=new BoxButton(StringData.EXTREME,Facade.EXTREME,0x553300,0xffdd88);
			_button.x=5;
			_button.y=90;
			addChild(_button);
			_button=new BoxButton(StringData.INSANE,Facade.INSANE,0x550000,0xff8888);
			_button.x=5;
			_button.y=110;
			addChild(_button);
		}
		
		public function update(_level:int,_x:Number,_y:Number){
			level=_level;
			highscore.text=String(Facade.gameM.getLevelScore(_level)[0]);
			x=_x+20;
			y=_y;
		}
		
		function startGame(i:int){
			cancel();
			Facade.gameM.wpm=i;
			Facade.gameM.starfield.wpm=Facade.gameM.wpm;
			Facade.gameM.level=level-1;
			Facade.gameC.newGame();
			Facade.transition.add(0xffffff,Facade.gameUI,Facade.menuUI);
		}
		
		function cancel(){
			parent.removeChild(this);
		}
	}
}