package{
	import flash.display.Sprite;
	import flash.events.Event;
	import flash.filters.GlowFilter;
	
	public class LaserEffect extends Sprite{
		
		public function LaserEffect (origin:Sprite,target:*){
			x=origin.x;
			y=origin.y;
			if (origin==Facade.gameM.pShip){
				graphics.lineStyle(1,0xff0000);
				graphics.lineTo(target.x-origin.x,target.y-origin.y);
				this.filters=[new GlowFilter(0xff7777,0.5)];
			}else{
				graphics.lineStyle(1,0x0000ff);
				graphics.lineTo(target.x-origin.x,target.y-origin.y);
				this.filters=[new GlowFilter(0x7777ff,0.5)];
			}
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