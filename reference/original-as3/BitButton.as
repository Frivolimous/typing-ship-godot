package{
	import flash.display.Sprite;
	import flash.display.Bitmap;
	import flash.display.BitmapData;
	import flash.filters.ColorMatrixFilter;
	import flash.geom.Rectangle;
	import flash.display.DisplayObjectContainer;

	public class BitButton extends Sprite{
		public var output:Function;
		public var bitmap:Bitmap;
		public var filter:Array;
		
		public function BitButton(b:BitmapData,f:Function,p:DisplayObjectContainer,r:Rectangle){
			name=KeyboardControl.BUTTON;
			output=f;
			buttonMode=true;
			mouseChildren=false;
			bitmap=new Bitmap(b);
			addChild(bitmap);
			
			filter=[new ColorMatrixFilter([2,0,0,0,0,
										   0,2,0,0,0,
										   0,0,2,0,0,
										   0,0,0,1,0])];
			var hA:Sprite=new Sprite();
			hA.graphics.beginFill(0,0);
			hA.graphics.drawRect(r.x,r.y,r.width,r.height);
			hitArea=hA;
			p.addChild(hA);
			p.addChild(this);
		}
		
		public function over(b:Boolean){
			if (b){
				bitmap.filters=filter;
			}else{
				bitmap.filters=[];
			}
		}
		
		public function updateHA(){
			hitArea.x=x;
			hitArea.y=y;
			hitArea.scaleX=scaleX;
			hitArea.scaleY=scaleY;
		}
	}
}