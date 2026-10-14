package{
	import flash.display.Sprite;
	import flash.text.TextField;
	import flash.text.TextFormat;
	import flash.events.KeyboardEvent;
	import flash.ui.Keyboard;
	
	public class PopBox extends Sprite{
		public static const RIGHT:String="right",
							LEFT:String="left",
							UP:String="up",
							DOWN:String="down";
		
		public static function tutText(i:int):String{
			switch(i){
				case 0: return "This is an Enemy Ship.  Type in the word shown below it to target it with your missiles.";
				case 1: return "This is a Laser Ship.  It will shoot an unblockable laser at you if not killed quickly.";
				case 2: return "This ship is Shielded.  You must type in two words to destroy it.";
				case 3: return "This is a Suicide Ship.  It will gain speed and attempt to ram you.";
				case 4: return "Larger ships have larger words to type in order to destroy them.";
				case 5: return "This ship has a Defense Turret.  If you dont destroy it then it will shoot down any missiles you launch at this ship.";
				case 6: return "This is a Space Mine.  If it gets too close it will damage you.";
				case 7: return "Some ships shoot missiles.  Shoot them down before they hit you.";
				case 8: return "You have been damaged!  Type in the word next to your Hull Bar to repair your ship.";
				case 9: return "A boss is approaching!  Type in the small words to hone into its weak spot, then type the long word to damage it.";
				case 10: return "Good job!  Two more shots to defeat the boss.";
				default: return "";
			}
		}
		public function PopBox(s:String,o:*=null,dir:String=null){
			mouseChildren=false;
			if (Facade.gameUI.tutBox!=null){
				Facade.gameUI.tutBox.remove();
			}
			Facade.gameUI.tutBox=this;
			Facade.gameC.pauseGame(true);
			var _text:TextField=new TextField;
			_text.defaultTextFormat=new TextFormat("Arial",13,0xffffff,true);
			_text.wordWrap=true;
			_text.text=s;
			_text.width=200;
			_text.x=175;
			_text.y=150;
			addChild(_text);
			_text=new TextField;
			_text.defaultTextFormat=new TextFormat("Arial",10,0xffffff,true);
			_text.text="hit SPACEBAR to continue\nhit ESCAPE to disable tutorial";
			_text.x=175;
			_text.y=270;
			_text.width=200;
			addChild(_text);
			graphics.lineStyle(3,0xffffff);
			graphics.beginFill(0,0.2);
			graphics.drawRect(175,150,200,150);
			graphics.endFill();
			if (Facade.stage.contains(Facade.gameUI)){
				Facade.gameUI.gameV.addChild(this);
			}else{
				Facade.stage.addChild(this);
			}
																							
			Facade.stage.addEventListener(KeyboardEvent.KEY_UP,keyUp);
			if (o!=null){
				if (dir==null){
					if (o.y<150){
						dir=UP;
					}else if (o.y>300){
						dir=DOWN;
					}else if (o.x<175){
						dir=LEFT;
					}else if (o.x>375){
						dir=RIGHT;
					}
				}
				if (dir==UP){
					graphics.moveTo(275,150);
					graphics.lineTo(275,o.y);
					graphics.lineTo(o.x,o.y);
				}else if (dir==DOWN){
					graphics.moveTo(275,300);
					graphics.lineTo(275,o.y);
					graphics.lineTo(o.x,o.y);
				}else if (dir==LEFT){
					graphics.moveTo(175,225);
					graphics.lineTo(o.x,225);
					graphics.lineTo(o.x,o.y);
				}else if (dir==RIGHT){
					graphics.moveTo(375,225);
					graphics.lineTo(o.x,225);
					graphics.lineTo(o.x,o.y);
				}
			}
		}
		
		public function keyUp(e:KeyboardEvent){
			if (e.keyCode==Keyboard.SPACE){
				remove();
			}else if (e.keyCode==Keyboard.ESCAPE){
				remove();
				Facade.gameM.tutOff();
			}
		}
		
		function remove(){
			parent.removeChild(this);
			Facade.gameUI.tutBox=null;
			if (Facade.stage.contains(Facade.gameUI)){
				Facade.gameC.pauseGame(false);
			}
			Facade.stage.removeEventListener(KeyboardEvent.KEY_UP,keyUp);
		}
	}
}