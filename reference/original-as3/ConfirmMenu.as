package  {
	import flash.display.Sprite;
	import flash.text.TextField;
	import flash.text.TextFormat;
	import flash.filters.GlowFilter;
	
	public class ConfirmMenu extends MenuBox{
		var wpm:TextField=new TextField();
		var levels:Array=new Array();
		var diff:Array=new Array();
		var selectL:Sprite=new Sprite();
		var selectD:Sprite=new Sprite();
		
		public function ConfirmMenu(){
			super("  Difficulty:\n\n\n                               WPM:",100,100,300,300,23);
			wpm.defaultTextFormat=new TextFormat(Facade.SPACE_FONT,50,Facade.BLUE,true);
			wpm.width=100;
			wpm.height=60;
			wpm.type="input";
			wpm.maxChars=3;
			wpm.restrict="0-9";
			wpm.x=280;
			wpm.y=204;
			wpm.border=true;
			wpm.borderColor=Facade.BORDER_BLUE;
			addChild(wpm);
			var _text:TextField=new TextField;
			_text.defaultTextFormat=new TextFormat(Facade.SPACE_FONT,23,Facade.BORDER_BLUE,true);
			_text.filters=[new GlowFilter(Facade.BLUE,0.6,4,4,2,1,true)];
			_text.text="Level Select:";
			_text.x=120;
			_text.y=283;
			_text.width=200;
			_text.selectable=false;
			addChild(_text);
			var _button:UIButton=new UIButton("Start Game",startGame);
			_button.x=100;
			_button.y=310;
			addChild(_button);
			_button=new UIButton("Cancel",cancel);
			_button.x=100;
			_button.y=350;
			addChild(_button);
			selectL.graphics.lineStyle(3,Facade.BORDER_BLUE);
			selectL.graphics.drawCircle(35,25,20);
			selectD.graphics.lineStyle(3,Facade.BORDER_BLUE);
			selectD.graphics.drawCircle(35,25,20);
			levels.push(new UIButton("1",level0));
			levels.push(new UIButton("2",level1));
			levels.push(new UIButton("3",level2));
			levels[0].y=levels[1].y=levels[2].y=270;
			levels[0].x=240;
			levels[1].x=275;
			levels[2].x=310;
			diff.push(new UIButton("Easy",diff0));
			diff.push(new UIButton("Medium",diff1));
			diff.push(new UIButton("Hard",diff2));
			diff.push(new UIButton("Custom",diff3));
			diff[0].scaleX=diff[1].scaleX=diff[2].scaleX=diff[3].scaleX=0.7;
			diff[0].scaleY=diff[1].scaleY=diff[2].scaleY=diff[3].scaleY=0.7;
			diff[0].x=diff[1].x=diff[2].x=diff[3].x=110;
			diff[0].y=130; addChild(diff[0]);
			diff[1].y=165; addChild(diff[1]);
			diff[2].y=200; addChild(diff[2]);
			diff[3].y=235; addChild(diff[3]);
			selectL.y=levels[0].y;
			selectD.x=diff[0].x+15;
			addChild(levels[0]);
			addChild(selectL);
			addChild(selectD);
		}
		
		function diff0(){
			wpm.text="25";
			selectD.y=diff[0].y;
			diffSize(diff[0]);
		}
		
		function diffSize(b:UIButton){
			selectD.graphics.clear();
			selectD.graphics.lineStyle(3,Facade.BORDER_BLUE);
			selectD.graphics.drawEllipse(-10,0,b.width+20,b.height);
		}
		
		function diff1(){
			wpm.text="40";
			selectD.y=diff[1].y;
			diffSize(diff[1]);

		}
		
		function diff2(){
			wpm.text="55";
			selectD.y=diff[2].y;
			diffSize(diff[2]);
		}
		
		function diff3(){
			selectD.y=diff[3].y;
			diffSize(diff[3]);
		}
		
		function level0(){
			selectL.x=levels[0].x;
			Facade.gameM.level=-1;
		}
		function level1(){
			selectL.x=levels[1].x;
			Facade.gameM.level=0;
		}
		function level2(){
			selectL.x=levels[2].x;
			Facade.gameM.level=1;
		}
		
		function update(){
			level0();
			if (Facade.gameM.achieve[1]>0){
				addChild(levels[1]);
			}
			if (Facade.gameM.achieve[1]>1){
				addChild(levels[2]);
			}
			switch(Facade.gameM.wpm){
				case 25: diff0(); break;
				case 40: diff1(); break;
				case 55: diff2(); break;
				default: diff3(); wpm.text=String(Facade.gameM.wpm); break;
			}
		}
		
		function startGame(){
			cancel();
			Facade.gameM.wpm=int(wpm.text);
			if (Facade.gameM.wpm<20) Facade.gameM.wpm=20;
			if (Facade.gameM.wpm>100) Facade.gameM.wpm=100;
			Facade.gameM.starfield.wpm=Facade.gameM.wpm;
			Facade.gameC.newGame();
			Facade.transition.add(0xffffff,Facade.gameUI,Facade.menuUI);
		}
		
		function cancel(){
			parent.removeChild(this);
		}
	}
}