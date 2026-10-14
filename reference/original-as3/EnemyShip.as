package{
	import flash.display.Sprite;
	
	public class EnemyShip extends GameSprite{
		var shieldV:ShieldView=new ShieldView(),
			commands:Array=new Array(),
			charge:Charge;
			
		public function EnemyShip(_index:int,_x:Number,_y:Number,_commands:Array,_a:Number,_wS:int,_f:int,_kB:int,_v:int,_s:Boolean,_t:Boolean){
			commands=_commands;
			wordSize=_wS;
			fires=_f;
			killBy=_kB;
			value=_v;
			
			viewSource=SpriteSheets.enemy[_index];
			switch(_index){
				case 0: scale=0.5; shieldV.width=45; shieldV.height=45; health=1; break;
				case 1: scale=0.5; shieldV.width=55; shieldV.height=55; shieldV.x=-5; health=1; break;
				case 2: scale=0.13; shieldV.width=30; shieldV.height=30; health=1; break;
				case 3: scale=0.65; shieldV.width=60; shieldV.height=60; shieldV.y=2; health=2; break;
				case 4: scale=0.65; shieldV.width=70; shieldV.height=70; shieldV.x=-2; health=2; break;
				case 5: scale=0.25; shieldV.width=35; shieldV.height=35; health=1; break;
				case 6: scale=1; shieldV.width=116; shieldV.height=82; shieldV.x=-7; shieldV.y=1; health=4; break;
				case 7: scale=1; shieldV.width=110; shieldV.height=65; shieldV.x=-8; health=4; break;
				case 8: scale=0.5; shieldV.width=50; shieldV.height=35; break;
				case 9: scale=1; shieldV.width=50; shieldV.height=35; turnRate=0; break;
			}
			
			if (fires==ActionControl.LASER){
				switch(_index){
					case 1: charge=new Charge(18,0,6000/Facade.gameM.wpm,0x0000ff); break;
					case 4: charge=new Charge(28,1,6000/Facade.gameM.wpm,0x0000ff); break;
					case 7: charge=new Charge(35,3,6000/Facade.gameM.wpm,0x0000ff); break;
					case 9: charge=new Charge(10,0,6000/Facade.gameM.wpm,0x0000ff); break;
					default: throw(new Error("shouldn't have Laser: "+_index));
				}
			}
			
			init(_x,_y,Math.atan2(commands[0].y-_y,commands[0].x-_x),_a,1);
			
			if (_t){
				turret=new Turret();
				turret.x-=20;
				addChild(turret);
			}
			
			if (commands[0].move==false){
				halt=true;
			}
			
			if (_s){
				addShield();
			}
			if (_index==8){
				new StealthEffect(this);
			}
		}

		override public function moreUpdate():Boolean{
			/*if ((x<0)||(y<0)||(x>GameUI.VIEW_WIDTH)||(y>GameUI.VIEW_HEIGHT)){
				Facade.gameM.missed+=1;
				remove();
				return false;
			}*/
			
			homeTarget(commands[0]);
			
			if (turret!=null){
				turret.update();
			}
			
			if (commands[0].condition(this)){
				commands.shift();
				if (commands.length>0){
					if (commands[0].fire==true){
						switch(fires){
							case ActionControl.MISSILE: Facade.actionC.shootMissile(this,Facade.gameM.pShip); break;
							case ActionControl.AUTO_MISSILE:Facade.actionC.shootMissile(this,Facade.gameM.pShip,true); break;
							case ActionControl.LASER: Facade.actionC.shootLaser(this,Facade.gameM.pShip); break;
							case ActionControl.SUICIDE: Facade.actionC.shootSuicide(this,Facade.gameM.pShip); break;
						}
					}
					if (commands[0].move){
						halt=false;
					}else{
						halt=true;
					}
				}else{
					if (fires==ActionControl.SUICIDE){
						Facade.actionC.hitTarget(Facade.gameM.pShip,ActionControl.SUICIDE);
						Facade.gameM.starfield.explosion(x,y,10);
					}
					remove();
					Facade.gameM.missedShips+=1;
					return false;
				}
			}
			return true;
		}
		
		override public function remove(){
			if (textObject!=null){
				new Overflow(textObject.text);
			}
			
			//try{
				Facade.gameM.gObjects.splice(Facade.gameM.gObjects.indexOf(this),1);
				if (charge!=null){
					charge.cancel();
				}
				parent.removeChild(this);
			//}catch(e:Error){}
		}
		
		public function addShield(){
			shieldV.add(this);
			shieldOn=true;
			recycleWord();
			if (turret!=null){
				turret.recycleWord();
			}
			addWord(3);
		}
		
		public function removeShield(){
			if (shieldOn){
				shieldV.remove();
				shieldOn=false;
				recycleWord();
				addWord();
				if (turret!=null){
					turret.addWord();
					addChild(turret);
				}
			}
		}
	}
}