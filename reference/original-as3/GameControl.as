package{
	import flash.display.Stage;
	import flash.display.Sprite;
	import flash.events.Event;
	import flash.geom.Point;
	
	public class GameControl{
		public var gameM:GameModel,
				gameUI:GameUI,
				spriteC:SpriteSheets,
				actionC:ActionControl,
				delay:int,
				finalDelay:Boolean=false,
				toRun:Function;
		
		public function init(){
			gameM=Facade.gameM;
			gameUI=Facade.gameUI;
			actionC=Facade.actionC;
			spriteC=Facade.spriteC;
			Facade.stage.addEventListener(Event.ENTER_FRAME,onFrame);
		}
		
		public function newGame(){
			while(gameM.gObjects.length>0){
				gameM.gObjects[0].remove();
			}
			if (gameM.boss!=null){
				gameM.boss.remove();
			}
			actionC.mRate=gameM.wpm/150;
			if (actionC.mRate>1) actionC.mRate=1;
			if (actionC.mRate<0.3) actionC.mRate=0.3;
			
			gameM.tutReset();
			gameM.score=0;
			nextLevel();
		}
		
		public function nextLevel(){
			gameM.pShip.removeShield();
			gameM.level+=1;
			gameM.distance=0;
			gameM.totalShips=gameM.missedShips=0;
			gameM.perfect=true;
			gameM.pShip.injured=false;
			gameM.scoreC=gameM.score;
			gameM.starfield.setBack(gameM.level);
			gameUI.resetLetter();
			actionC.heal(100);
			gameM.running=true;
			gameM.canType=true;
			gameM.bossMode=false;
			gameM.levelData=LevelData.loadData(gameM.level);
			gameM.maxDistance=gameM.levelData[gameM.levelData.length-1].distance;
			gameUI.gameV.addChild(gameM.pShip);
			delay=0;
			finalDelay=false;
			toRun=null;
			Facade.soundC.play(1+(gameM.level%4));
		}
		
		public function onFrame(e:Event){

			if (gameM.running){
				gameM.starfield.update();
				if (delay>0){
					delay-=1;
					if (delay==1){
						toRun();
					}else if (toRun==finalBoss){
						gameM.boss.alpha=delay/100;
						if (gameM.boss.over!=null){
							gameM.boss.over.alpha=delay/100;
						}
						if (Math.random()>0.6){
							var _point:Point=new Point();
							do{
								_point.x=Math.random()*gameM.boss.view.bitmapData.width;
								_point.y=Math.random()*gameM.boss.view.bitmapData.height;
							}while (gameM.boss.view.bitmapData.getPixel32(_point.x,_point.y)<0x01000000);
							gameM.starfield.explosion((_point.x*gameM.boss.scale)+gameM.boss.x-gameM.boss.view.width/2,(_point.y*gameM.boss.scale)+gameM.boss.y-gameM.boss.view.height/2);
						}
					}
				}else if (gameM.pShip.health<=0){
					playerDeath();
					return;
				}
				gameM.distance+=gameM.wpm/Facade.stage.frameRate/3600;
				gameUI.onTick();
				spriteC.onTick();
				if (gameM.levelData.length>0){
					while ((gameM.levelData.length>0)&&(gameM.distance>gameM.levelData[0].distance)){
						gameM.levelData.shift().run();
					}
				}else{
					if (gameM.bossMode){
						gameM.levelData=LevelData.bossLoop();
					}else{
						if ((gameM.boss==null)&&(gameM.gObjects.length==0)){
							if ((delay==0)&&(finalDelay)){
								endLevel();
							}else{
								gameM.levelData.push(new LevelEvent(gameM.distance+0.05,LevelEvent.WAIT,0.05));
								finalDelay=true;
							}
						}
					}
				}
				var i:int=0;
				while (i<gameM.gObjects.length){
					if (gameM.gObjects[i].update()){
						/*if ((gameM.tutFlags[0]==false)&&(inBounds(gameM.gObjects[i])&&(gameM.gObjects[i] is EnemyShip))){
							new PopBox(PopBox.tutText(0),gameM.gObjects[i]);
							gameM.tutFlags[0]=true;
						}else if ((gameM.tutFlags[1]==false&&(inBounds(gameM.gObjects[i])))&&(gameM.gObjects[i].fires==ActionControl.LASER)){
							new PopBox(PopBox.tutText(1),gameM.gObjects[i]);
							gameM.tutFlags[1]=true;
						}else if ((gameM.tutFlags[2]==false)&&(inBounds(gameM.gObjects[i]))&&(gameM.gObjects[i].shieldOn)){
							new PopBox(PopBox.tutText(2),gameM.gObjects[i]);
							gameM.tutFlags[2]=true;
						}else if ((gameM.tutFlags[3]==false)&&(inBounds(gameM.gObjects[i]))&&(gameM.gObjects[i].fires==ActionControl.SUICIDE)){
							new PopBox(PopBox.tutText(3),gameM.gObjects[i]);
							gameM.tutFlags[3]=true;
						}else if ((gameM.tutFlags[4]==false)&&(inBounds(gameM.gObjects[i]))&&(gameM.gObjects[i].viewSource==SpriteSheets.enemy[4])){
							new PopBox(PopBox.tutText(4),gameM.gObjects[i]);
							gameM.tutFlags[4]=true;
						}else if ((gameM.tutFlags[5]==false)&&(inBounds(gameM.gObjects[i]))&&(gameM.gObjects[i].turret!=null)){
							new PopBox(PopBox.tutText(5),gameM.gObjects[i]);
							gameM.tutFlags[5]=true;
						}else if ((gameM.tutFlags[6]==false)&&(inBounds(gameM.gObjects[i]))&&(gameM.gObjects[i].viewSource==SpriteSheets.enemy[2])){
							new PopBox(PopBox.tutText(6),gameM.gObjects[i]);
							gameM.tutFlags[6]=true;
						}*/
						i+=1;
					}
				}
				
				i=0;
				while (i<gameM.gOverflow.length){
					if (gameM.gOverflow[i].update()){
						i+=1;
					}
				}
				if (gameM.boss!=null){
					gameM.boss.update();
				}
				gameUI.popWarning.update();
			}
		}
				
		public function endLevel(){
			while(gameM.gObjects.length>0){
				gameM.gObjects[0].remove();
			}
			if (gameM.boss!=null){
				gameM.boss.remove();
			}
			gameM.running=false;
			Facade.transition.add(0xffffff,Facade.scoreUI,Facade.gameUI);
		}
		
		public function killBoss(){
			gameM.bossMode=false;
			gameM.boss.commands=null;
			gameM.levelData=new Array();
			delay=200;
			toRun=finalBoss;
			switch(gameM.achieve[1]){
				case 0: if (gameM.level==3){
					gameM.achieve[1]=1;
					new PopAchieve(StringData.CONQUEROR,1);
				}break;
				case 1: if (gameM.level==7){
					gameM.achieve[1]=2;
					new PopAchieve(StringData.CONQUEROR,2);
				}break;
				case 2: if (gameM.level==11){
					gameM.achieve[1]=3;
					new PopAchieve(StringData.CONQUEROR,3);
				}
			}
		}
		
		public function finalBoss(){
			endLevel();
		}
		
		public function playerDeath(){
			gameM.canType=false;
			delay=100;
			gameUI.gameV.removeChild(gameM.pShip);
			gameM.starfield.explosion(gameM.pShip.x,gameM.pShip.y,100);
			toRun=finalDeath;
			gameM.tutOff();
		}
		
		function finalDeath(){
			gameM.running=false;
			if (gameM.score>gameM.getLevelScore(gameM.level)[0]){
				gameM.getLevelScore(gameM.level)[0]=gameM.score;
			}
			Facade.goUI.addLose();
			Facade.transition.add(0,Facade.goUI,Facade.gameUI,3);
		}
		
		public function pauseGame(b:Boolean){
			gameM.running=!b;
			showAll(!b);
			if (b){
				Facade.pauseB.gotoAndStop(2);
				if (gameUI.tutBox==null){
					Facade.stage.addChildAt(gameUI.pauseUI,Facade.stage.numChildren-2);
				}
			}else{
				Facade.pauseB.gotoAndStop(1);
				if (gameUI.tutBox!=null){
					gameUI.tutBox.remove();
				}
				if (Facade.stage.contains(gameUI.pauseUI)){
					Facade.stage.removeChild(gameUI.pauseUI);
					if (gameUI.pauseUI.contains(gameUI.pauseUI.confirmM)){
						gameUI.pauseUI.removeChild(gameUI.pauseUI.confirmM);
					}
				}
			}
		}
		
		public function inBounds(_v:GameSprite):Boolean{
			if ((_v!=null)&&(_v.y>_v.height/2)&&(_v.x>_v.width/2)&&(_v.y<GameUI.VIEW_HEIGHT-_v.height/2)&&(_v.x<GameUI.VIEW_WIDTH-_v.width/2)){
				return true;
			}else{
				return false;
			}
		}
		
		public function showAll(b:Boolean){
			for (var i:int=0;i<gameM.gObjects.length;i+=1){
				gameM.gObjects[i].show(b);
				if (gameM.gObjects[i].turret!=null){
					gameM.gObjects[i].turret.show(b);
				}
			}
			gameM.hull.show(b);
			if ((gameM.boss!=null)&&(gameM.boss.scanner!=null)){
				gameM.boss.scanner.show(b);
			}
		}
	}
}