package{
	import flash.display.Sprite;
	import flash.display.Bitmap;
	import flash.geom.Point;
	
	public class BossShip extends GameSprite{
		var commands:Array=new Array(),
			charge:Charge,
			scanner:Scanner,
			index:int,
			objects:Array=new Array(),
			over:Bitmap=new Bitmap,
			delay:int=-1,
			count:int,
			five:int=5;
			
		public function BossShip(i:int){
			health=3;
			five=300/Facade.gameM.wpm;
			index=i;
			wordSize=-1;
			commands.push(new ObjectCommand(GameUI.VIEW_WIDTH/2,100));
			viewSource=SpriteSheets.boss[i];
			switch(index){
				case 0: commands.push(new ObjectCommand(GameUI.VIEW_WIDTH/2,100)); scale=0.5; break;
				case 1: commands.push(new ObjectCommand(GameUI.VIEW_WIDTH/2,170)); scale=0.7; break;
				case 2: commands.push(new ObjectCommand(GameUI.VIEW_WIDTH/2,170)); scale=0.5; break;
			}
			commands.push(new ObjectCommand(GameUI.VIEW_WIDTH/2,500,6,false,true));

			
			init(GameUI.VIEW_WIDTH/2,-50,Math.PI/2,0.1,0,true);

			Facade.gameUI.bossV.addChild(this);
			switch(index){
				case 0:
					over.bitmapData=SpriteSheets.bRes[0][0];
					over.scaleX=over.scaleY=scale;
					Facade.gameUI.gameV.addChild(over);
					break;
				case 1:
					over.bitmapData=SpriteSheets.bRes[1];
					over.scaleX=over.scaleY=scale;
					over.x=view.x;
					over.y=view.y;
					break;
				case 2: 
					over.bitmapData=SpriteSheets.bRes[2][1][0];
					over.x=43;
					over.y=160;
					view.x+=12;
					break;
				default: throw(new Error("Boss int is out of bounds: "+i));
			}
		}
		
		override public function moreUpdate():Boolean{
			if (scanner==null){
				if ((vT==0)&&(Facade.gameM.bossMode==false)&&(health>0)){
					scanner=new Scanner(this);
					Facade.gameM.bossMode=true;
				}
			}else{
				scanner.update();
			}
			if (health==0) return false;
			
			switch(index){
				case 0:
					over.x=x-view.width/2;
					over.y=y-view.height/2;
					
					if (delay>0){
						if (delay==10){
							if (objects.length==0){
								over.bitmapData=SpriteSheets.bRes[0][1];
							}
						}else if (delay==1){
							if (objects.length>0){
								over.bitmapData=SpriteSheets.bRes[0][2];
							}else{
								over.bitmapData=SpriteSheets.bRes[0][0];
							}
						}
						delay-=1;
						
					}else{
						if (objects.length>0){
							while (true){
								if ((objects[0].commands.length==1)||(objects[0].parent==null)){
									objects.shift();
									if (objects.length==0) break;
								}else{
									break;
								}
							}
							if (objects.length==0){
								delay=20;
							}
						}
					}
					break;
				case 1:
					if (delay<=0){
						delay=36;
					}else{
						delay-=1;
						if (delay==35){
							addChild(over);
						}else if (delay==20){
							removeChild(over);
						}
					}
					break;
				case 2:
					if (delay>=0){
						switch(delay){
							case five*43: makeCounter(30,20); break;
							case five*42: makeCounter(-75,20); break;
							case five*41: makeCounter(30,-20); break;
							case five*40: makeCounter(-75,-20); break;
							case five*36:
								view.bitmapData=SpriteSheets.bRes[2][0][1];
								Facade.soundC.sound(SoundControl.BOSS_CHARGE);
								break;
							case five*28:
								view.bitmapData=SpriteSheets.bRes[2][0][2];
								Facade.soundC.sound(SoundControl.BOSS_CHARGE);
								break;
							case five*20:
								Facade.gameUI.bossV.addChildAt(over,0);
								if (count>0){
									over.bitmapData=SpriteSheets.bRes[index][1][1];
								}
								Facade.soundC.sound(SoundControl.BOSS_FIRE);
								break;
							default:
								if (delay<five*15){
									if (delay%5==0){
										if (count==0){
											Facade.gameM.starfield.explosion(Facade.gameM.pShip.x-Facade.gameM.pShip.view.width/2+(Math.random()*Facade.gameM.pShip.view.width),Facade.gameM.pShip.y-Facade.gameM.pShip.view.height/2+(Math.random()*Facade.gameM.pShip.view.height));
										}else{
											over.bitmapData=SpriteSheets.bRes[index][1][(Math.floor(delay/5)%3)+1];
										}
									}
									if (delay%(five*4)==0){
										if (delay>0) Facade.soundC.sound(SoundControl.BOSS_FIRE);
										if (count>0){
											count-=1;
											Facade.gameM.pShip.shieldV.alpha-=0.2;
											if (count==0){
												over.bitmapData=SpriteSheets.bRes[index][1][0];
												Facade.gameM.pShip.removeShield();
											}
										}else{
											Facade.actionC.hitTarget(Facade.gameM.pShip,ActionControl.SUICIDE);
										}
									}
									if (delay==0){
										Facade.gameM.pShip.removeShield();
										over.parent.removeChild(over);
										view.bitmapData=SpriteSheets.boss[2];
										while(objects.length>0){
											if (objects[0].parent!=null){
												objects[0].remove();
											}
											objects.shift();
										}
									} 
								}
								break;
						}
						delay-=1;
					}
					break;
			}
			if (commands==null){
				return true;
			}
			if (commands[0].condition(this)){
				if (commands[0].fire==true){
					switch(index){
						case 0: 
							var array:Array=LevelData.boss0Suicides();
							while (array.length>0){
								Facade.gameM.gObjects.unshift(array.shift().run(true));
								objects.push(Facade.gameM.gObjects[0]);
								Facade.gameM.gObjects[0].x+=x-view.width/2;
								Facade.gameM.gObjects[0].y+=y-view.height/2;
								Facade.gameUI.gameV.setChildIndex(Facade.gameM.gObjects[0],0);
							}
							over.bitmapData=SpriteSheets.bRes[0][1];
							delay=20;
							break;
						case 1:
							var _point:Point;
							switch(Math.floor(Math.random()*6)){
								case 0: _point=new Point(96,115); break;
								case 1: _point=new Point(205,235); break;
								case 2: _point=new Point(250,185); break;
								case 3: _point=new Point(390,185); break;
								case 4: _point=new Point(430,235); break;
								case 5: _point=new Point(540,115); break;
								defaut: break;
							}
							_point.x=x-view.width/2+_point.x*scale;
							_point.y=y-view.height/2+_point.y*scale;
							new SpawnEvent(0,(health>1)?"nln":"nls",_point.x,_point.y,[new ObjectCommand(_point.x,500,(health>1)?1:3,false,false),new ObjectCommand(_point.x,500,20,false,true)]).run();
							break;
						case 2: view.bitmapData=SpriteSheets.bRes[2][0][0];
							Facade.soundC.sound(SoundControl.BOSS_CHARGE);
							count=0;
							delay=five*44;
							break;
						default: throw (new Error("Boss index out of bounds: "+index));
					}
				}
				commands.shift();
				if (commands.length==0){
					switch(index){
						case 0: commands.push(new ObjectCommand(GameUI.VIEW_WIDTH/2,500,48,false,true)); break;
						case 1: 
						commands.push(new ObjectCommand(GameUI.VIEW_WIDTH/2,500,23+(health*3),false,true)); break;
						case 2: commands.push(new ObjectCommand(GameUI.VIEW_WIDTH/2,500,70,false,true)); break;
						default: throw (new Error("Boss index out of bounds: "+index));
					}
				}
				if (commands[0].move){
					halt=false;
				}else{
					halt=true;
				}
			}return true;
		}
		
		function makeCounter(_x:Number,_y:Number){
			objects.unshift(new GameSprite());
			objects[0].init(Facade.gameM.pShip.x+_x-5,Facade.gameM.pShip.y+_y,0,0,2);
			objects[0].wordSize=0;
			objects[0].killBy=ActionControl.INSTANT;
			objects[0].addWord(11-health);
			Facade.gameM.gObjects.push(objects[0]);
			Facade.gameUI.gameV.addChild(objects[0]);
		}
		
		override public function remove(){
			if (over.parent!=null){
				over.parent.removeChild(over);
			}
			if (scanner!=null){
				scanner.remove();
			}
			try{
				parent.removeChild(this);
				recycleWord();
				Facade.gameM.boss=null;
				
				if (charge!=null){
					charge.cancel();
				}
			}catch(e:Error){}
		}
		
		
		public function injure(){
			if (Facade.gameM.tutFlags[10]==false){
				new PopBox(PopBox.tutText(10),Facade.gameM.boss);
				Facade.gameM.tutFlags[10]=true;
			}
			health-=1;
			if (health>0){
				scanner=new Scanner(this);
			}else{
				new PopScore(this,100);
				Facade.gameC.killBoss();
				if (index==0){
					Facade.gameUI.bossV.addChild(over);
				}
			}
		}
	}
}