package{
	import flash.display.Sprite;
	import flash.geom.Point;
	
	public class ActionControl{
		public static const LASER:int=0,
							MISSILE:int=1,
							AUTO_MISSILE:int=2,
							EMP:int=3,
							SUICIDE:int=4,
							INSTANT:int=5;
							
							
		public var gameM:GameModel,
				gameUI:GameUI,
				gameC:GameControl,
				soundC:SoundControl,
				mCount:Number=0,
				mRate:Number=0.4;
		
		public function init(){
			gameM=Facade.gameM;
			gameUI=Facade.gameUI;
			gameC=Facade.gameC;
			soundC=Facade.soundC;
		}

		public function shootMissile(o:GameSprite,t:GameSprite,_auto:Boolean=false){
			if (o==gameM.pShip){
				switch (t.health){
					case 4:
						gameM.gObjects.unshift(new Missile(o,t,-Math.PI*5/6));
						gameM.gObjects[0].parent.setChildIndex(gameM.gObjects[0],0);
						gameM.gObjects.unshift(new Missile(o,t,-Math.PI/6,Math.ceil(100/gameM.wpm)));
						gameM.gObjects[0].parent.setChildIndex(gameM.gObjects[0],0);
						gameM.gObjects.unshift(new Missile(o,t,Math.PI*3/4,Math.ceil(200/gameM.wpm)));
						gameM.gObjects[0].parent.setChildIndex(gameM.gObjects[0],0);
						gameM.gObjects.unshift(new Missile(o,t,Math.PI/4,Math.ceil(300/gameM.wpm)));
						gameM.gObjects[0].parent.setChildIndex(gameM.gObjects[0],0);
						break;
					case 3:
						gameM.gObjects.unshift(new Missile(o,t,-Math.PI/2));
						gameM.gObjects[0].parent.setChildIndex(gameM.gObjects[0],0);
						gameM.gObjects.unshift(new Missile(o,t,Math.PI*3/4,Math.ceil(200/gameM.wpm)));
						gameM.gObjects[0].parent.setChildIndex(gameM.gObjects[0],0);
						gameM.gObjects.unshift(new Missile(o,t,Math.PI/4,Math.ceil(300/gameM.wpm)));
						gameM.gObjects[0].parent.setChildIndex(gameM.gObjects[0],0);
						break;
					case 2:
						gameM.gObjects.unshift(new Missile(o,t,-Math.PI,Math.ceil(100/gameM.wpm)));
						gameM.gObjects[0].parent.setChildIndex(gameM.gObjects[0],0);
						gameM.gObjects.unshift(new Missile(o,t,0));
						gameM.gObjects[0].parent.setChildIndex(gameM.gObjects[0],0);
						break;
					case 1:
						gameM.gObjects.unshift(new Missile(o,t));
						gameM.gObjects[0].parent.setChildIndex(gameM.gObjects[0],0);
						break;
				}
			}else{
				if (_auto){
					mCount+=1;
				}else{
					switch(o.health){
						case 2: mCount+=mRate*1.5; break;
						case 4: case 3: mCount+=mRate*2; break;
						default: mCount+=mRate; break;
					}
				}
				
				if (mCount>1){
					o.priority=0;
					(o as EnemyShip).value-=1;
					mCount-=1;
					gameM.gObjects.push(new Missile(o,t));
					if (gameM.tutFlags[7]==false){
						new PopBox(PopBox.tutText(7),gameM.gObjects[gameM.gObjects.length-1]);
						gameM.tutFlags[7]=true;
					}
				}
			}
		}
		
		public function shootLaser(o:*,t:GameSprite,b:Boolean=false){
			if (o==gameM.pShip){
				if (t.name=="Turret"){
					new LaserEffect(o,t.parent);
				}else{
					new LaserEffect(o,t);
				}
				hitTarget(t,LASER);
				soundC.sound(SoundControl.LASER);
			}else{
				if (b){
					new LaserEffect(o,t);
					hitTarget(t,LASER);
					gameM.starfield.explosion(t.x,t.y,10);
					soundC.sound(SoundControl.LASER);
					o.priority=0;
				}else{
					o.charge.add(o,t);
					o.priority=2;
					soundC.sound(SoundControl.CHARGE);
				}
			}
		}
		
		public function shootInstant(o:PlayerShip,t:GameSprite){
			gameM.boss.count+=1;
			if (o.contains(o.shieldV)){
				o.shieldV.alpha+=0.2;
			}else{
				o.addShield();
				o.shieldV.alpha=0.2;
			}
			hitTarget(t,INSTANT);
		}
		
		public function shootEMP(o:*,t:GameSprite,b:Boolean=false){
			if (b){
				new EMPEffect(o,t);
				hitTarget(t,EMP);
				soundC.sound(SoundControl.EMP);
			}else{
				o.charge.add(o,t);
			}
		}
		
		public function shootSuicide(o:*,t:GameSprite){
			o.commands=[new ObjectCommand(t.x,t.y)]
			o.a*=2;
			o.priority=3;
		}
		
		public function heal (i:int=1){
			if (i==10){
				soundC.sound(SoundControl.HEAL);
			}
			gameM.pShip.health+=i;
			if (gameM.pShip.health>PlayerShip.MAX_HEALTH){
				gameM.pShip.health=PlayerShip.MAX_HEALTH;
			}else if (gameM.pShip.health<0){
				gameM.pShip.health=0;
			}
			gameM.hull.drawBar(gameM.pShip.health);
			if (gameM.pShip.health<PlayerShip.MAX_HEALTH){
				if (gameM.hull.textObject==null){
					gameM.hull.addWord();
				}
				if (gameM.pShip.health<=1){
					gameM.hull.priority=3;
				}else if (gameM.pShip.health<=2){
					gameM.hull.priority=2;
				}else if (gameM.pShip.health<=3){
					gameM.hull.priority=1
				}else{
					gameM.hull.priority=0;
				}
			}else{
				gameM.hull.recycleWord();
			}
		}
		
		public function damagePlayer(i:int=1){
			if (gameC.delay>0) return;
			if (gameM.invincible) return;
			heal(-i);
			gameM.pShip.injured=true;
		}
		
		public function shieldPlayer(){
			if (gameM.pShip.shieldOn){
				gameM.pShip.removeShield();
			}else{
				gameM.pShip.addShield();
			}
		}
		
		public function hitTarget (t:*,s:int){
			if (t==gameM.pShip){
				damagePlayer();
				if (gameM.tutFlags[8]==false){
					new PopBox(PopBox.tutText(8),new Point(420,530),PopBox.RIGHT);
					gameM.tutFlags[8]=true;
					gameM.hull.show(true);
				}
			}else if ((t.shieldOn)&&(s==EMP)){
				t.removeShield();
			}else{
				if ((s==MISSILE)&&(t.health>1)){
					t.health-=1;
					gameM.starfield.explosion(t.x,t.y,20);
				}else{
					if ((t.value!=0)&&(gameM.bossMode==false)) new PopScore(t,t.value);
					if (t.wordSize==3){
						gameM.starfield.explosion(t.x,t.y,20);
					}else if (t.wordSize>3){
						if (s==MISSILE){
							gameM.achieveC[0]+=1;
							switch(gameM.achieve[0]){
								case 0: if (gameM.achieveC[0]>=10){
									gameM.achieve[0]=1;
									new PopAchieve(StringData.SOLDIER,1);
								}break;
								case 1: if (gameM.achieveC[0]>=1000){
									gameM.achieve[0]=2;
									new PopAchieve(StringData.SOLDIER,2);
								}break;
								case 2: if (gameM.achieveC[0]>=20000){
									gameM.achieve[0]=3;
									new PopAchieve(StringData.SOLDIER,3);
								}break;
								default: null;
							}
						}
						gameM.starfield.explosion(t.x,t.y);
					}else if (t.viewSource==SpriteSheets.superman){
						gameM.starfield.explosion(t.x,t.y,300);
						new PopScore(t,99);
						if (gameM.achieve[5]==2){
							gameM.achieve[5]=3;
							new PopAchieve(StringData.EXPLORER,3);
						}
					}
					if (t.parent==null){
						gameM.missedShips-=1;
					}else{
						t.remove();
					}
				}
			}
		}
	}
}