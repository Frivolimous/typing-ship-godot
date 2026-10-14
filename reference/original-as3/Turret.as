package{
	import flash.display.Sprite;
	
	public class Turret extends GameSprite{
		var target:*=null;
		
		public function Turret(){
			name="Turret";
			a=0;turnRate=1;
			wordSize=4;
			killBy=ActionControl.LASER;
			addWord();
			//rotation=90;
			graphics.beginFill(0x666666);
			graphics.lineStyle(1,0xaaaaaa);
			graphics.drawCircle(0,0,3);
			graphics.drawRect(-3,0,6,-15);
			priority=1;
		}
				
		override public function update():Boolean{
			if (textObject!=null){
				textObject.rotation=0-rotation-parent.rotation;
			}
			return true;
		}
		
		public function testTarget(t:Missile){
			rotation=Math.atan2(t.y-(y+parent.y),t.x-(x+parent.x))*180/Math.PI;
		
			if (Math.sqrt((t.y-parent.y)*(t.y-parent.y)+(t.x-parent.x)*(t.x-parent.x))<100){
				Facade.actionC.shootLaser(parent,t,true);
				(parent as GameObject).addWord();
			}
			
			if (textObject!=null){
				textObject.rotation=0-rotation-parent.rotation;
			}

			return true;
		}
	}
}