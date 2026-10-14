package  {
	import flash.display.Sprite;
	import flash.text.TextField;
	import flash.text.TextFormat;
	import flash.filters.GlowFilter;
	
	public class MenuBox extends Sprite{
		var display:TextField=new TextField;
		
		public function MenuBox(_text:String,_x:int,_y:int,_width:int,_height:int,_size:int=20) {
			name=KeyboardControl.REMOVE;
			mouseChildren=false;
			buttonMode=true;
			graphics.beginFill(0,0.01);
			graphics.drawRect(0,0,Facade.stage.stageWidth,Facade.stage.stageHeight);
			graphics.lineStyle(2,Facade.BORDER_BLUE);
			graphics.beginFill(Facade.MENU_BLUE,0.9);
			graphics.drawRect(_x,_y,_width,_height);
			display.defaultTextFormat=new TextFormat(Facade.SPACE_FONT,_size,Facade.BORDER_BLUE,true);
			display.wordWrap=true;
			display.selectable=false;
			display.width=_width-6;
			display.height=_height-6;
			display.x=_x;
			display.y=_y;
			display.text=_text;
			addChild(display);
			display.filters=[new GlowFilter(Facade.BLUE,0.6,4,4,2,1,true)];
		}
		
		public function set text(s:String){
			display.text=s;
		}
		
		public function get text():String{
			return display.text;
		}

	}
	
}
