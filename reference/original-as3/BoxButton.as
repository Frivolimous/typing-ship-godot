package  {
	import flash.display.Sprite;
	import flash.text.TextField;
	import flash.text.TextFormat;
	import flash.display.Bitmap;
	import flash.display.BitmapData;
	import flash.filters.GlowFilter;
	import flash.geom.Point;

	public class BoxButton extends Sprite{
		public var label:TextField=new TextField();
		public var index:int;

		public function BoxButton(t:String,i:int,c1:uint=0,c2:uint=0){
			name=KeyboardControl.DIFF_BUTTON;
			index=i;
			buttonMode=true;
			mouseChildren=false;
			label.defaultTextFormat=new TextFormat(Facade.SPACE_FONT,12,c2,true);
			label.x=0;
			label.text=t;
			label.width=60;
			label.height=20;
			label.background=true;
			label.backgroundColor=c1;
			label.border=true;
			label.borderColor=2;
			
			addChild(label);
		}

	}
	
}
