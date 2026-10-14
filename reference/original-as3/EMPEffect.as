package{
	import flash.display.Sprite;
	import flash.events.Event;
	import flash.filters.GlowFilter;
	
	public class EMPEffect extends Sprite{
		
		public function EMPEffect (origin:Sprite,target:Sprite){
			x=origin.x;
			y=origin.y;
			graphics.lineStyle(2,0xffffff);
			graphics.lineTo(target.x-origin.x,target.y-origin.y);
			this.filters=[new GlowFilter(0xffaaaa,1,8,8,3)];
			Facade.gameUI.addEventListener(Event.ENTER_FRAME,update,false,0,true);
			Facade.gameUI.gameV.addChildAt(this,0);
		}
		
		public function update(e:Event){
			if (alpha>0){
				alpha-=0.08;
			}else{
				Facade.gameUI.removeEventListener(Event.ENTER_FRAME,update,false);
				parent.removeChild(this);
			}
		}
	}
}