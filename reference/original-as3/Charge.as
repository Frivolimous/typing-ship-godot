package{
	import flash.display.Sprite;
	import flash.events.Event;
	import flash.filters.GlowFilter;
	
	public class Charge extends Sprite{
		var count:int;
		var maxCount:int;
		var origin:GameSprite;
		var target:GameSprite;
		var type:int;
		var color:uint;
		
		public function Charge(_x:int,_y:int,i:int,_color:uint=0x0000ff,t:int=ActionControl.LASER){
			type=t;
			maxCount=i;
			x=_x;
			y=_y;
			color=_color;
		}
		
		public function add(_origin:GameSprite,_target:GameSprite){
			origin=_origin;
			target=_target;
			count=0;
			addEventListener(Event.ENTER_FRAME,grow,false,0,true);
			origin.addChildAt(this,0);
		}
		
		public function cancel(){
			try{
				removeEventListener(Event.ENTER_FRAME,grow,false);
				parent.removeChild(this);
			}catch(e:Error){}
		}
		
		public function grow(e:Event){
			if (Facade.gameM.running){
				count+=1;
				if(count>=maxCount){
					removeEventListener(Event.ENTER_FRAME,grow,false);
					parent.removeChild(this);;
					if (type==ActionControl.LASER){
						Facade.actionC.shootLaser(origin,target,true);
					}else if (type==ActionControl.EMP){
						Facade.actionC.shootEMP(origin,target,true);
					}
				}else{
					if (count==maxCount/2){
						origin.priority=3;
						Facade.soundC.sound(SoundControl.CHARGE);
					}
					graphics.clear();
					graphics.beginFill(0x0000ff+Math.floor(Math.random()*0x40)*0x100-Math.floor(Math.random()*0x40));
					graphics.drawCircle(0,0,10*count/maxCount);
				}
			}
		}
	}
}