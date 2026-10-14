package  {
	import flash.display.Sprite;
	import flash.text.TextField;
	import flash.text.TextFormat;
	import flash.display.Bitmap;
	import flash.display.BitmapData;
	import flash.filters.GlowFilter;
	import flash.geom.Point;
	import flash.events.Event;
	import flash.geom.ColorTransform;

	
	public class LevelButton extends Sprite{
		public var label:TextField=new TextField();
		public var index:int;
		public var newOver:Sprite=new Sprite;
		var spin:Bitmap=new Bitmap(null,"auto",true);
		var SPIN_SIZE:int=65;
		var CENTER_X:int=7;
		var CENTER_Y:int=12;
		var health:Bitmap=new Bitmap();
		var target:Bitmap=new Bitmap();
		var healthT:TextField=new TextField();
		var targetT:TextField=new TextField();

		public function LevelButton(t:String,i:int){
			name=KeyboardControl.LEVEL_BUTTON;
			index=i;
			buttonMode=true;
			mouseChildren=false;
			label.x=0;
			label.text=t;
			label.width=20;
			label.height=25;
			spin.bitmapData=new BitmapData(SPIN_SIZE,SPIN_SIZE,true,0);
			addChild(spin);
			addChild(label);
			health.x=CENTER_X-20;
			target.x=CENTER_X+8;
			health.y=target.y=CENTER_Y+28;
			healthT.y=targetT.y=CENTER_Y+39;
			healthT.x=CENTER_X-60;
			targetT.x=CENTER_X+3;
			healthT.width=targetT.width=150;
			healthT.height=targetT.height=40;
			targetT.defaultTextFormat=healthT.defaultTextFormat=new TextFormat(Facade.TITLE_FONT,10,0xffffff,true,null,null,null,null,null,null,null,null,-5);
			addChild(health);
			addChild(target);
		}
		
		public function update(_diff:int,_health:int=0,_target:int=0){
			switch(_diff){
				case -1: changeColor(0x333333,SpriteSheets.lockC); buttonMode=false; newLevel(false); break;
				case 0: changeColor(0xffffff,SpriteSheets.nextC); buttonMode=true; newLevel(true); break;
				case 1: changeColor(0x88aaff,SpriteSheets.completeC); buttonMode=true; newLevel(false); break;
				case 2: changeColor(0x88ff88,SpriteSheets.completeC); buttonMode=true; newLevel(false); break;
				case 3: changeColor(0xffff88,SpriteSheets.completeC); buttonMode=true; newLevel(false); break;
				case 4: changeColor(0xffbb88,SpriteSheets.completeC); buttonMode=true; newLevel(false); break;
				case 5: changeColor(0xff8888,SpriteSheets.completeC); buttonMode=true; newLevel(false); break;
			}
			
			health.bitmapData=SpriteSheets.health[_health];
			target.bitmapData=SpriteSheets.target[_target];
			switch(_health){
				case 0: healthT.text=""; break;
				case 1: healthT.filters=[new GlowFilter(Facade.BRONZE,1,3,3,2,1,true)];
						healthT.text=StringData.HEALTH;
						break;
				case 2: healthT.filters=[new GlowFilter(Facade.SILVER,1,3,3,2,1,true)];
						healthT.text=StringData.HEALTH;
						break;
				case 3: healthT.filters=[new GlowFilter(Facade.GOLD,1,3,3,2,1,true)];
						healthT.text=StringData.HEALTH
						break;
			}
			switch(_target){
				case 0: targetT.text=""; break;
				case 1: targetT.filters=[new GlowFilter(Facade.BRONZE,1,3,3,2,1,true)];
						targetT.text=StringData.ACCURACY;
						break;
				case 2: targetT.filters=[new GlowFilter(Facade.SILVER,1,3,3,2,1,true)];
						targetT.text=StringData.ACCURACY;
						break;
				case 3: targetT.filters=[new GlowFilter(Facade.GOLD,1,3,3,2,1,true)];
						targetT.text=StringData.ACCURACY;
						break;
			}
			
		}
		
		public function changeColor(c:uint,bm:BitmapData){
			label.defaultTextFormat=new TextFormat(Facade.SPACE_FONT,20,c,true);
			label.text=label.text;
			graphics.clear();
			newOver.graphics.clear();
			newOver.graphics.lineStyle(0,c);
			newOver.graphics.drawCircle(CENTER_X,CENTER_Y,5);
			newOver.filters=[new GlowFilter(c,1,50,50,16,1,false,true)];
			var red:Number=Math.floor(c/0x10000)/0xFF+0.8;
			var green:Number=Math.floor((c%0x010000)/0x0100)/0xFF+0.8;
			var blue:Number=(c%0x0100)/0xFF+0.8;
			spin.bitmapData.fillRect(spin.bitmapData.rect,0);
			spin.bitmapData.draw(bm,null,new ColorTransform(red,green,blue));
		}
		
		public function newLevel(b:Boolean){
			if (b){
				addChild(newOver);
			}else{
				if (contains(newOver)){
					removeChild(newOver);
				}
			}
		}
		
		var out:Boolean=false;
		
		function onTick(){
			if (out){
				if (newOver.alpha>=1){
					out=false;
				}else{
					newOver.alpha+=0.1;
				}
			}else{
				if (newOver.alpha<=0){
					out=true;
				}else{
					newOver.alpha-=0.1;
				}
			}
			var n:Number=Math.PI/4+spin.rotation*Math.PI/180;
			
			spin.rotation+=1;
			spin.x=CENTER_X-SPIN_SIZE*spin.scaleX/2*Math.sqrt(2)*Math.cos(n);
			spin.y=CENTER_Y-SPIN_SIZE*spin.scaleX/2*Math.sqrt(2)*Math.sin(n);
		}
		
		public function click(){
			if (buttonMode){
				Facade.menuUI.mapUI.addChild(Facade.menuUI.mapUI.diffSelect);
				Facade.menuUI.mapUI.diffSelect.update(index,x,y);
			}
		}
		
		public function over(b:Boolean){
			if (b){
				spin.scaleX=spin.scaleY=health.scaleX=health.scaleY=target.scaleX=target.scaleY=1.2;
				target.x-=2;
				addChild(healthT);
				addChild(targetT);
			}else{
				spin.scaleX=spin.scaleY=health.scaleX=health.scaleY=target.scaleX=target.scaleY=1;
				target.x+=2;
				removeChild(healthT);
				removeChild(targetT);
			}
		}
	}
}