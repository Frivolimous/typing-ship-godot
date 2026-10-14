package{
	import flash.display.Sprite;
	import flash.text.TextField;
	import flash.text.TextFormat;
	import flash.display.Bitmap;
	import flash.display.BitmapData;
	import flash.filters.GlowFilter;
	import flash.filters.BevelFilter;
	import flash.geom.Point;

	public class UIButton extends Sprite{
		public var label1:TextField=new TextField();
		public var label2:TextField=new TextField();
		public var output:Function;
		
		public function UIButton(t:String,f:Function,c1:uint=0,c2:uint=0,simple:Boolean=false){
			if (c1==0){
				c1=Facade.BLUE;
				c2=Facade.BLUE_OVER;
			}
			name=KeyboardControl.BUTTON;
			output=f;
			buttonMode=true;
			mouseChildren=false;
			
			if (simple){
				label1.defaultTextFormat=new TextFormat(Facade.TITLE_FONT,16,0xffffff,true);
				label2.defaultTextFormat=new TextFormat(Facade.TITLE_FONT,16,0xffffff,true);
				label1.text=label2.text=t;
				label1.width=label2.width=label1.textWidth+20;
				label1.height=label2.height=30;
				label1.filters=[new GlowFilter(c1,1,3,3,2,1,true)];
				label2.filters=[new GlowFilter(c2,1,3,3,2,1,true),new GlowFilter(c2,1,3,3,2,1)];
			}else{
				label1.defaultTextFormat=new TextFormat(Facade.ROSE_FONT,20,c1);
				label2.defaultTextFormat=new TextFormat(Facade.ROSE_FONT,20,c1);
				label1.text=label2.text=t;
				label1.width=label2.width=label1.textWidth+10;
				label1.height=label2.height=30;
				label1.filters=[new BevelFilter(5,30,0xffffff,.75,0,.75),new GlowFilter(c1,1,1,1,1,1,false),new GlowFilter(0,1,6,6,2,1,false)];
				label2.filters=[new BevelFilter(5,30,0xffffff,.75,0,.75),new GlowFilter(c2,1,3,3)];
			}
			addChild(label1);
		}
		
		public function over(b:Boolean){
			if (b){
				if (contains(label1)){
					removeChild(label1);
					addChild(label2);
				}
			}else{
				if (contains(label2)){
					removeChild(label2);
					addChild(label1);
				}
			}
		}
	}
}