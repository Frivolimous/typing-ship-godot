package  {
	import flash.display.Sprite;
	import flash.display.Bitmap;
	import flash.display.BitmapData;
	import flash.geom.ColorTransform;
	import flash.events.Event;
	import flash.geom.Matrix;
	
	public class StealthEffect extends Sprite{
		var toSpawn:EnemyShip;
		var red:Bitmap=new Bitmap;
		var blue:Bitmap=new Bitmap;
		var counter:Number=0;
		var countUp:Number;
		var centerX:Number;
		var centerY:Number;
		
		public function StealthEffect(_v:EnemyShip) {
			countUp=1+Facade.gameM.wpm/30;
			toSpawn=_v;
			_v.parent.addChild(this);
			_v.alpha=0;
			x=_v.x;
			y=_v.y;
			var _matrix:Matrix=new Matrix(_v.scale,0,0,_v.scale);
			red.bitmapData=new BitmapData(_v.width,_v.height,true,0);
			blue.bitmapData=new BitmapData(_v.width,_v.height,true,0);
			red.bitmapData.draw(_v.view,_matrix,new ColorTransform(2,0.4,0.4,1));
			blue.bitmapData.draw(_v.view,_matrix,new ColorTransform(0.4,0.4,2,1));
			red.rotation=blue.rotation=90;
			centerX=_v.view.x;
			centerY=_v.view.y;
			
			addChild(red);
			addChild(blue);
			
			alpha=0;
			addEventListener(Event.ENTER_FRAME,onTick);
			
		}
		
		function onTick(e:Event){
			rotation=toSpawn.rotation;
			if (Facade.gameM.running){
				counter+=countUp;
				var a:Number=counter/100*Math.PI*2;
				var r:Number=20*(1-counter/100);
				red.x=centerX+r*Math.sin(a);
				red.y=centerY+r*Math.cos(a);
				blue.x=centerX-r*Math.sin(a);
				blue.y=centerY-r*Math.cos(a);

				alpha=(0.1+counter/150);
				toSpawn.alpha=(counter-70)/30;
				if (toSpawn.alpha<0) toSpawn.alpha=0;
				if (counter>=100){
					toSpawn.alpha=1;
					parent.removeChild(this);
					removeEventListener(Event.ENTER_FRAME,onTick);
				}
			}
		}
	}
	
}
