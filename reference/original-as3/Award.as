package  {
	import flash.display.Sprite;
	import flash.display.Bitmap;
	import flash.text.TextField;
	import flash.text.TextFormat;
	import flash.filters.GlowFilter;
	
	public class Award extends Sprite{
		var trophy:Bitmap=new Bitmap(SpriteSheets.achieve[0]);
		var label:TextField=new TextField;
		
		public function Award() {
			mouseChildren=false;
			buttonMode=true;
			name=KeyboardControl.AWARD;
			addChild(trophy);
			label.defaultTextFormat=new TextFormat(Facade.TITLE_FONT,17,0xffffff,true);
			label.filters=[new GlowFilter(Facade.GOLD,1,2,2,1,1,true),new GlowFilter(Facade.GOLD,1,2,2,1,1)];
			label.y=92;
			label.x=10;
			label.width=140;
			label.height=50;
			addChild(label);
		}
	}
}
