package{
	import flash.display.Sprite;
	import flash.events.Event;
	import flash.text.TextField;
	import flash.text.TextFormat;
	import flash.filters.GlowFilter;
	
	public class PopScore extends Sprite{
		var frontText:TextField=new TextField;
		var rate:Number;
		
		public function PopScore (origin:*,s:int){
			if(s>0){
				frontText.defaultTextFormat=new TextFormat(Facade.GAME_FONT,14+s/20,0x00ff00);
			}else{
				frontText.defaultTextFormat=new TextFormat(Facade.GAME_FONT,14+s/20,0xff0000);
			}
			rate=1-s/120;
			frontText.filters=[new GlowFilter(0,1,8,8,4)];

			frontText.text=(s>0?"+":"")+String(s);
			Facade.gameM.score+=s;
			addChild(frontText);
			
			x=origin.x;
			y=origin.y;
			Facade.gameUI.addEventListener(Event.ENTER_FRAME,update,false,0,true);
			try{
				origin.name;
				Facade.gameUI.gameV.addChild(this);
			}catch(e:Error){
				Facade.gameUI.addChild(this);
			}
		}
		
		public function update(e:Event){
			if (alpha>0){
				alpha-=0.02*rate;
				y-=0.6*rate;
			}else{
				Facade.gameUI.removeEventListener(Event.ENTER_FRAME,update,false);
				parent.removeChild(this);
			}
		}
	}
}