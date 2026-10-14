package{
	import flash.display.Sprite;
	import flash.events.Event;
	
	public class ShieldView extends Sprite{
		public function ShieldView(){
			graphics.clear();
			graphics.beginGradientFill("radial",[0x00aaff,0x00aaff,0x00aaff],[0.5,0.7,1],[0,15,25]);
			graphics.drawCircle(0,0,10);
		}
		
		public function add(_target:*){
			alpha=1;
			graphics.clear();
			graphics.beginGradientFill("radial",[0x00aaff,0x00aaff,0x00aaff],[0.5,0.7,1],[0,15,25]);
			graphics.drawCircle(0,0,10);
			_target.addChild(this);
			try{
				removeEventListener(Event.ENTER_FRAME,fade);				
			}catch(e:Error){}
		}
		
		public function remove(){
			addEventListener(Event.ENTER_FRAME,fade,false,0,true);
			graphics.clear();
			graphics.beginGradientFill("radial",[0xffffff,0xffaaaa,0xffaaaa],[1,0.5,0],[15,25,30]);
			graphics.drawCircle(0,0,13);
		}
		
		public function fade(e:Event){
			alpha-=0.1;
			if (alpha<=0){
				removeEventListener(Event.ENTER_FRAME,fade);
				parent.removeChild(this);
			}
		}
	}
}