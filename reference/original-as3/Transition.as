package  {
	import flash.display.Sprite;
	import flash.events.Event;
	
	public class Transition extends Sprite{
		var up:Boolean=false;
		var toAdd:*;
		var toRemove:*;
		var speed:Number;

		public function Transition() {
		}
		
		public function add(_color:uint,_add:Sprite,_remove:Sprite,_speed:int=1){
			toAdd=_add;
			toRemove=_remove;
			Facade.stage.addChild(this);
			up=true;
			alpha=0;
			graphics.clear();
			graphics.beginFill(_color);
			graphics.drawRect(0,0,GameUI.UI_WIDTH,GameUI.UI_WIDTH);
			addEventListener(Event.ENTER_FRAME,update);
			speed=0.1/_speed;
		}
		
		public function update(e:Event){
			if (up){
				alpha+=speed;
				if (alpha>=1){
					up=false;
					toRemove.parent.removeChild(toRemove);
					Facade.stage.addChildAt(toAdd,Facade.stage.numChildren-1);
					Facade.stage.addChildAt(Facade.muteB,Facade.stage.numChildren-1);
					if (toAdd==Facade.gameUI){
						Facade.stage.addChildAt(Facade.pauseB,Facade.stage.numChildren-1);
					}else if (toAdd==Facade.menuUI){
						Facade.soundC.play(0);
					}
					if (toRemove==Facade.gameUI){
						Facade.stage.removeChild(Facade.pauseB);
					}
					try{
						toAdd.navIn();
					}catch(e:Error){}
				}
			}else{
				alpha-=speed;
				if (alpha<=0){
					Facade.stage.focus=toAdd;
					removeEventListener(Event.ENTER_FRAME,update);
					parent.removeChild(this);
				}
			}
		}
	}
}