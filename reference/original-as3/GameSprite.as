package{
	import flash.display.Sprite;
	import flash.display.Bitmap;
	import flash.display.BitmapData;
	
	public class GameSprite extends GameObject{
		public var vX:Number=0,
				vY:Number=0,
				vT:Number=0,
				n:Number,
				a:Number,
				value:int,
				killBy:int,
				turnRate:Number,
				shieldOn:Boolean,
				turret:Turret,
				halt:Boolean,
				view:Bitmap=new Bitmap,
				viewSource:*,
				scale:Number=0.2,
				fires:int,
				frame:int,
				health:int=1;
		
		public function init(_x:Number,_y:Number,_n:Number,_a:Number,_p:int,_isBoss:Boolean=false){
			if (_isBoss){
				vT=0.01;
				view.bitmapData=viewSource.clone();
				view.scaleX=view.scaleY=scale;
				addChild(view);
				view.x=-view.width/2;
				view.y=-view.height/2;
				n=Math.PI/2;
				rotation=0;
			}else{
				Facade.spriteC.update(this,0);
				addChild(view);
				
				view.rotation=90;
				view.y=-0.5*view.height;
				view.x=0.5*view.width;
				n=_n;
				rotation=n*180/Math.PI;
			}
			
			x=_x;
			y=_y;
			a=_a;
			turnRate=a/7;
			addWord();
			priority=_p;
			Facade.gameUI.gameV.addChild(this);
		}
		
		public function update():Boolean{
			if (moreUpdate()){
				vT+=((halt)?0:a)-vT*0.1;
				vX=vT*Math.cos(n);
				vY=vT*Math.sin(n);

				if (Math.abs(vT)<Math.abs(a)){
					vX=vY=vT=0;
				}else{
					x+=vX;
					y+=vY;
				}
				return true;
			}				
			return false;
		}
		
		public function moreUpdate():Boolean{
			return true;
		}
				
		public function remove(){
			if (textObject!=null){
				new Overflow(textObject.text);
			}
			//try{
				var i:int=Facade.gameM.gObjects.indexOf(this);
				if (i>=0){
					Facade.gameM.gObjects.splice(i,1);
				}else if (this is Turret){
					(parent as EnemyShip).turret=null;
				}
				parent.removeChild(this);
			//}catch(e:Error){}
		}
		
		public function homeTarget(_target:*){
			var tDiff:Number=Math.atan2(_target.y-y,_target.x-x)-n;
			while (tDiff<(0-Math.PI)){
				tDiff+=2*Math.PI;
			}
			while (tDiff>Math.PI){
				tDiff-=2*Math.PI;
			}
			
			if (tDiff>0){
				n+=(tDiff>turnRate)?turnRate:tDiff;
			}else if (tDiff<0){
				n+=(tDiff<-turnRate)?(0-turnRate):tDiff;
			}else{
				return;
			}
			rotation=n*180/Math.PI;
			if (textObject!=null){
				textObject.rotation=0-rotation;
			}
		}
	}
}