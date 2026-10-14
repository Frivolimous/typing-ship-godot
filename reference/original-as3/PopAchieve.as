package  {
	import flash.utils.Timer;
	import flash.events.TimerEvent;
	import flash.display.Sprite;
	import flash.text.TextField;
	import flash.text.TextFormat;
	import flash.display.Bitmap;
	import flash.display.BitmapData;
	import flash.geom.Matrix;
	
	public class PopAchieve extends Sprite{
		var timer:Timer=new Timer(50,80);
		var bitmap:Bitmap;
		public function PopAchieve(s:String,i:int) {
			alpha=0;
			timer.addEventListener(TimerEvent.TIMER_COMPLETE,timerC);
			timer.addEventListener(TimerEvent.TIMER,timerT);
			timer.start();
			graphics.lineStyle(2,Facade.BORDER_BLUE);
			graphics.beginFill(0,0.5);
			graphics.drawRect(0,0,250,100);

			bitmap=new Bitmap(new BitmapData(250,200,true,0));
			//bitmap.bitmapData.draw(SpriteSheets.achieve[4]);
			bitmap.bitmapData.draw(SpriteSheets.achieve[i],new Matrix(1,0,0,1,-10,-10));
			bitmap.x=bitmap.y=5;
			addChild(bitmap);
			mouseChildren=false;
			var _textField:TextField=new TextField;
			_textField.defaultTextFormat=new TextFormat(Facade.SPACE_FONT,15,0xffffff,true);
			_textField.wordWrap=true;
			_textField.width=150;
			_textField.height=100;
			_textField.text=StringData.NEW_ACHIEVE+"\n\n"
			switch(i){
				case 1: _textField.appendText(StringData.BRONZE+" "); break;
				case 2: _textField.appendText(StringData.SILVER+" "); break;
				case 3: _textField.appendText(StringData.GOLD+" "); break;
			}
			_textField.appendText(s);
			_textField.x=100;
			bitmap.bitmapData.draw(_textField,new Matrix(1,0,0,1,100));
			x=175;
			y=20;
			Facade.stage.addChild(this);
		}
		function timerT(e:TimerEvent){
			if (timer.currentCount<=20){
				alpha=timer.currentCount/20;
			}
			if (timer.currentCount>=60){
				alpha=(80-timer.currentCount)/20;
			}
		}
		
		function timerC(e:TimerEvent){
			bitmap.bitmapData.dispose();
			timer.removeEventListener(TimerEvent.TIMER_COMPLETE,timerC);
			timer.removeEventListener(TimerEvent.TIMER,timerT);
			parent.removeChild(this);
		}

	}
	
}
