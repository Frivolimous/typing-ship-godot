package{
	import flash.display.Sprite;
	import flash.text.TextField;
	import flash.text.TextFormat;
	import flash.filters.GlowFilter;
	
	public class TextObject extends Sprite{
		public var frontText:TextField=new TextField();
		public var wordSize:int;

		public function TextObject(i:int,p:int){
			if (i>0){
				wordSize=i;
				text=Facade.wordList.shift(i);
				frontText.filters=[new GlowFilter(0,1,8,8,4)];
				addChild(frontText);
				priority(p);
			}
		}
		
		public function set text(s:String){
			frontText.text=s;
			frontText.x=0-frontText.textWidth/2;
		}
		
		public function get text():String{
			return frontText.text;
		}
		
		public function priority(i:int){
			var _color:uint;
			switch(i){
				case 0: _color=0xffffff; break;
				case 1: _color=0xffff99; break;
				case 2: _color=0xffbb99; break;
				case 3: _color=0xff8877; break;
			}
			frontText.defaultTextFormat=new TextFormat(Facade.WORD_FONT,14,_color);
			frontText.setTextFormat(new TextFormat(Facade.WORD_FONT,14,_color));
		}
		
		public function show(b:Boolean){
			if (b){
				addChild(frontText);
			}else{
				if (contains(frontText)){
					removeChild(frontText);
				}
			}
		}
	}
}