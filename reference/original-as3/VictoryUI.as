package  {
	import flash.text.TextField;
	import flash.display.Sprite;
	import flash.filters.GlowFilter;
	import flash.events.Event;
	import flash.display.Bitmap;
	import flash.text.TextFormat;
	
	public class VictoryUI extends Sprite{
		var back:Bitmap=new Bitmap(new FinalB(550,550));
		var skipB:UIButton=new UIButton(StringData.SKIP,skip,Facade.GOLD,Facade.GOLD_OVER);
		var scrollT:TextField=new TextField;
		public function VictoryUI() {
			addChild(back);
			skipB.scaleX=skipB.scaleY=0.5;
			skipB.x=550-skipB.width-10;
			skipB.y=10;
			addChild(skipB);
			scrollT.defaultTextFormat=new TextFormat(Facade.SPACE_FONT,25,0xffffff,true,null,null,null,null,"center");
			scrollT.wordWrap=true;
			scrollT.x=50;
			scrollT.y=550;
			scrollT.width=450;
			scrollT.height=3000;
			scrollT.selectable=false;
			scrollT.filters=[new GlowFilter(Facade.GOLD,1,2,2,1,1,true),new GlowFilter(Facade.GOLD,1,2,2,1,1)];
			scrollT.text=StringData.FINAL_CREDITS;
			//scrollT.autoSize="center";
			scrollT.height=scrollT.textHeight;
			addChild(scrollT);

		}
		
		public function run(){
			Facade.soundC.play(0);
			scrollT.y=550;
			addEventListener(Event.ENTER_FRAME,onTick);
		}
		
		public function onTick(e:Event){
			scrollT.y-=1;
			if (scrollT.y<0-scrollT.height){
				done();
			}
		}
		
		public function skip(){
			done();
		}
		
		public function done(){
			removeEventListener(Event.ENTER_FRAME,onTick);
			Facade.goUI.addWin();
			Facade.transition.add(0,Facade.goUI,this,5);

		}
	}
	
}
