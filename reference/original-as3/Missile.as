package{
	import flash.display.Sprite;
	import flash.geom.Point;
	
	public class Missile extends GameSprite{
		const homing:Boolean=true;
		var target:*;
		var delay:int;
		
		public function Missile(origin:*,_target:*,_n:Number=NaN,_d:int=0){
			target=_target;
			
			if (origin==Facade.gameM.pShip){
				viewSource=SpriteSheets.pMissile;
				var tSpeed:Number=0.1*Facade.gameM.wpm;
				wordSize=-1;
				if (isNaN(_n)){
					var tValue:Number=origin.rotation;
				}else{
					tValue=_n;
				}
			}else{
				killBy=ActionControl.LASER;
				if (Facade.gameM.bossMode){
					value=0;
				}else{
					value=1;
				}
				viewSource=SpriteSheets.eMissile;
				tSpeed=0.008*Facade.gameM.wpm;
				wordSize=3;
				tValue=origin.rotation*Math.PI/180;
			}
			scale=0.1;
			init(origin.x,origin.y,tValue,tSpeed,3);
			turnRate=tSpeed/20;
			delay=_d;
		}
		
		override public function moreUpdate():Boolean{
			if (delay>0){
				delay-=1;
				if (delay>0){
					halt=true;
				}else{
					halt=false;
				}
				return true;
			}
			turnRate+=a/300;
			if (target.turret!=null){
				target.turret.testTarget(this);
			}
			
			homeTarget(target);
			if (((Math.abs(x-target.x)<=Math.abs(vX*1.5))&&(Math.abs(y-target.y)<=Math.abs(vY*1.5)))||
				((Math.abs(x-target.x)<=30)&&(Math.abs(y-target.y)<=30))){
				Facade.actionC.hitTarget(target,ActionControl.MISSILE);
				if (target==Facade.gameM.pShip) {
					Facade.gameM.starfield.explosion(x,y,10);
				}
				remove();
				return false;
			}
			return true;
		}
	}
}