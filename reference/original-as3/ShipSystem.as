package{
	import flash.text.TextField;
	import flash.text.TextFormat;
	import flash.display.Bitmap;
	import flash.display.Sprite;
	
	public class ShipSystem extends GameObject{
		var color:uint;
		var dName:TextField=new TextField();
		var bar:Bitmap=new Bitmap;
		var barMask:Sprite=new Sprite;
		//var state:Boolean;
		
		public function ShipSystem(){
			dName.defaultTextFormat=new TextFormat("Arial",10,0xffffff);
			dName.y=-15;
			//dName.text="Hull";
			addChild(dName);
			color=Facade.DASH_GOLD;
			drawBar(100);
			wordX=180;
			wordSize=WordList.healSize();
			addChild(new Bitmap(SpriteSheets.healthBase));
			bar.bitmapData=SpriteSheets.healthFull;
			bar.mask=barMask;
			addChild(bar);
			addChild(barMask);
		}
		
		public function drawBar(i:int){
			/*graphics.clear();
			graphics.beginFill(0xff0000);
			graphics.drawRect(0,0,200,10);
			graphics.beginFill(color);
			graphics.drawRect(0,0,200*i/PlayerShip.MAX_HEALTH,10);*/
			barMask.graphics.clear();
			barMask.graphics.beginFill(0);
			//barMask.graphics.drawRect(0,0,100,100);
			barMask.graphics.drawRect(0,0,bar.width*i/PlayerShip.MAX_HEALTH,bar.height);
		}
		
		public function drawCircle(_state:Boolean){
			//state=_state;
			graphics.clear();
			graphics.beginFill(color);
			graphics.drawCircle(5,5,5);
			if (_state==false){
				graphics.beginFill(0,0.5);
				graphics.drawCircle(5,5,5);
			}
		}
	}
}