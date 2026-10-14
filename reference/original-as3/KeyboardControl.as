package{
	import flash.events.KeyboardEvent;
	import flash.events.MouseEvent;
	import flash.ui.Keyboard;
	import flash.display.Sprite;
	import flash.geom.Point;
	import flash.text.TextField;
	
	public class KeyboardControl{
		public static const BUTTON:String="button",
							DIFF_BUTTON:String="difficulty button",
							LEVEL_BUTTON:String="level button",
							TEST:String="test",
							AWARD:String="award",
							MUTE:String="mute",
							REMOVE:String="remove";
							
		public static const SPECIAL:Boolean=true;
		var gameM:GameModel;
		var gameUI:GameUI;
		var gameC:GameControl;
		var actionC:ActionControl;
		
		public function init(s:*){
			gameM=Facade.gameM;
			gameUI=Facade.gameUI;
			gameC=Facade.gameC;
			actionC=Facade.actionC;
			s.addEventListener(MouseEvent.MOUSE_DOWN,click);
			s.addEventListener(KeyboardEvent.KEY_DOWN,keyDown);
			s.addEventListener(MouseEvent.MOUSE_OVER,mouseOver);
			s.addEventListener(MouseEvent.MOUSE_OUT,mouseOut);
		}
		
		public function click(e:MouseEvent){
			if ((Facade.menuUI.contains(Facade.menuUI.mapUI))&&(Facade.menuUI.mapUI.contains(Facade.menuUI.mapUI.diffSelect))
				&&(e.target!=Facade.menuUI.mapUI.diffSelect)&&(e.target.parent!=Facade.menuUI.mapUI.diffSelect)){
				Facade.menuUI.mapUI.removeChild(Facade.menuUI.mapUI.diffSelect);
			}
			if (e.target.name==BUTTON){
				e.target.output();
				e.target.over(false);
				Facade.soundC.sound(SoundControl.CLICK);
			}else if (e.target.name==LEVEL_BUTTON){
				(e.target as LevelButton).click();
			}else if (e.target.name==DIFF_BUTTON){
				(e.target.parent as DiffSelect).startGame((e.target as BoxButton).index);
				Facade.soundC.sound(SoundControl.CLICK);
			}else if (e.target.name==MUTE){
				e.target.output(e.localX);
			}else if (e.target.name==REMOVE){
				e.target.parent.removeChild(e.target);
			}else if (Facade.stage.contains(Facade.typingTest)){
				Facade.stage.focus=Facade.typingTest.top;
			}else if (Facade.stage.contains(Facade.menuUI.credits)){
				Facade.transition.add(0xffffff,Facade.menuUI,Facade.menuUI.credits);
			}else if ((e.target.constructor!=TextField)||(e.target.type!="input")){
				Facade.stage.focus=Facade.stage;
			}
		}
		
		public function mouseOver(e:MouseEvent){
			switch(e.target.name){
				case BUTTON: case LEVEL_BUTTON: e.target.over(true); break;
				case AWARD: Facade.menuUI.achieveUI.popText(e.target); break;
			}
		}

		public function mouseOut(e:MouseEvent){
			switch(e.target.name){
				case BUTTON: case LEVEL_BUTTON: e.target.over(false); break;
				case AWARD: Facade.menuUI.achieveUI.popText(); break;
			}
		}
		
		public function keyDown(e:KeyboardEvent){
			if (gameM.running){
				if (gameM.canType){
					if ((e.keyCode>=65)&&(e.keyCode<=90)){
						gameUI.addLetter(KeyCon.fromCode(e.keyCode));
						Facade.soundC.sound(SoundControl.TYPE);
						for (var i:int=0;i<gameM.gObjects.length;i+=1){
							if (testWord(gameM.gObjects[i])){
								if (gameM.gObjects[i].shieldOn){
									actionC.shootEMP(gameM.pShip,gameM.gObjects[i]);
								}else{
									switch(gameM.gObjects[i].killBy){
										case ActionControl.MISSILE: actionC.shootMissile(gameM.pShip,gameM.gObjects[i]); break;
										case ActionControl.LASER: actionC.shootLaser(gameM.pShip,gameM.gObjects[i]); break;
										case ActionControl.INSTANT: actionC.shootInstant(gameM.pShip,gameM.gObjects[i]); break;
										default: throw new Error("Invalid Kill type: "+gameM.gObjects[i].killBy);
									}
								}
								return;
							}
							try{
								if (gameM.gObjects[i].turret!=null){
									if (testWord(gameM.gObjects[i].turret)){
										actionC.shootLaser(gameM.pShip,gameM.gObjects[i].turret);
										return;
									}
								}
							}catch(e:Error){
							}
						}
						for (i=0;i<gameM.gOverflow.length;i+=1){
							if (testWord2(gameM.gOverflow[i].word)){
								gameM.gOverflow[i].remove();
								return;
							}
						}
						if (testWord(gameM.hull)){
							actionC.heal();
							return;
						}
						if ((gameM.boss!=null)&&(gameM.boss.scanner!=null)){
							if (testWord(gameM.boss.scanner)){
								gameM.boss.scanner.scan();
								return;
							}
						}
						
						if(testWord2(StringData.UNMUTE)){
							Facade.soundC.mute=false;
						}else if (testWord2(StringData.MUTE)){
							Facade.soundC.mute=true;
						}else if (testWord2(StringData.SUPERMAN)){
							makeSuperman();
							if (Facade.gameM.achieve[2]==0){
								Facade.gameM.achieve[2]=1;
								new PopAchieve(StringData.RIDDLER,1);
							}
						}else if (testWord2(StringData.KAMIKAZE)){
							if (Facade.gameM.achieve[2]==1){
								Facade.gameM.achieve[2]=2;
								new PopAchieve(StringData.RIDDLER,2);
							}
							while(Facade.gameM.gObjects.length>0){
								Facade.actionC.hitTarget(Facade.gameM.gObjects[0],ActionControl.INSTANT);
							}
							Facade.gameC.playerDeath();
						}else if (testWord2(StringData.CHAMELEON)){
							if (Facade.gameM.achieve[2]==2){
								Facade.gameM.achieve[2]=3;
								new PopAchieve(StringData.RIDDLER,3);
							}
							SpriteSheets.recolour();
						}else if (testWord2(StringData.UNPAUSE)){
							Facade.gameC.pauseGame(false);
						}else if (testWord2(StringData.PAUSE)){
							Facade.gameC.pauseGame(true);
							if (Facade.gameM.achieve[5]==1){
								Facade.gameM.achieve[5]=2;
								new PopAchieve(StringData.EXPLORER,2);
							}
						}
					}else if (e.keyCode==Keyboard.BACKSPACE){
						gameUI.removeLetter(1);
						Facade.soundC.sound(SoundControl.TYPE);
					}
				}
			}else if (Facade.stage.contains(Facade.typingTest)){
				if (e.keyCode==Keyboard.ESCAPE){
					Facade.typingTest.cancel();
				}else if (e.keyCode!=Keyboard.SHIFT){
					Facade.typingTest.update(String.fromCharCode(e.charCode));
				}
			}else if (Facade.stage.contains(Facade.scoreUI)){
				if ((e.keyCode==Keyboard.SPACE)&&(Facade.scoreUI.scoreT.running==false)){
					if (Facade.gameM.level>=11){
						Facade.vicUI.run();
						Facade.transition.add(0xffffff,Facade.vicUI,Facade.scoreUI);
					}else{
						Facade.transition.add(0xffffff,Facade.menuUI,Facade.scoreUI);
					}
				}
			}else if ((e.ctrlKey)&&(e.keyCode==Keyboard.HOME)&&(SPECIAL)){
				gameM.invincible=true;
			}else if ((e.ctrlKey)&&(e.keyCode==Keyboard.PAGE_UP)&&(SPECIAL)){
				gameM.unlockAll();
			}else if ((e.ctrlKey)&&(e.keyCode==Keyboard.PAGE_DOWN)&&(SPECIAL)){
				gameM.clearData();
			}else if(Facade.stage.focus==Facade.menuUI.confirmM.wpm){
				Facade.menuUI.confirmM.diff3();
			}
		}
		
		public function testWord(v:*):Boolean{
			if (v.textObject!=null){
				var tWord:String=v.textObject.text;
				if (tWord==gameUI.typeText.text.substring(gameUI.typeText.text.length-tWord.length)){
					removeLetters(tWord.length);
					v.recycleWord();
					return true;
				}
			}
			return false;
		}
		
		public function testWord2(v:String):Boolean{
			if (v!=null){
				var tWord:String=v;
				if (tWord==gameUI.typeText.text.substring(gameUI.typeText.text.length-tWord.length)){
					removeLetters(tWord.length);
					return true;
				}
			}
			return false;
		}
		
		function removeLetters(i:int){
			gameUI.typeText.text=gameUI.typeText.text.substring(0,gameUI.typeText.text.length-i);
		}
		
		function makeSuperman(){
			Facade.soundC.sound(SoundControl.SUPERMAN);
			var gs:GameSprite=new GameSprite();
			gs.viewSource=SpriteSheets.superman;
			gs.init(Math.random()*400+50,550,-Math.PI/2,1.3,1);
			gs.parent.setChildIndex(gs,0);
			gameM.gObjects.push(gs);
		}
	}
}