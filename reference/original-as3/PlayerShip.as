package{
	import flash.display.Sprite;
	import flash.display.Bitmap;
		
	public class PlayerShip extends GameSprite{
		public static const MAX_HEALTH:int=5;
		
		public var injured:Boolean;
		public var shieldSize:Number;
		var shieldV:ShieldView=new ShieldView(),
		charge:Charge;
		
		public function PlayerShip(){
			scale=1;
			charge=new Charge(0,-20,5,0xffffff,ActionControl.EMP);
			viewSource=SpriteSheets.player;
			Facade.spriteC.update(this,0);
			addChild(view);
			view.x=0-view.width/2;
			view.y=0-view.height/2;
			init(GameUI.VIEW_WIDTH/2,GameUI.VIEW_HEIGHT-100,-Math.PI/2,0,0);
			shieldV.width=100;
			shieldV.height=100;
		}
		
		public function addShield(){
			shieldV.add(this);
			shieldOn=true;
		}
		
		public function removeShield(){
			if (shieldOn){
				shieldV.remove();
				shieldOn=false;
			}
		}

	}
}