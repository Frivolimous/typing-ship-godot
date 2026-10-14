package{
	import flash.display.Sprite;
	import flash.text.TextField;
	import flash.text.TextFormat;
	import flash.utils.Timer;
	import flash.events.TimerEvent;
	import flash.filters.GlowFilter;
	
	public class TypingTest extends Sprite{
		public var top:TextField=new TextField();
		public var bottom:TextField=new TextField();
		public var errorS:Sprite=new Sprite();
		public var errorT:Timer=new Timer(1000,1);
		public var countT:Number;
		public var numWords:int;
		public var finishB:MenuBox;
		public var finishT:Timer=new Timer(4000,1);
		
		public function TypingTest(){
			errorT.addEventListener(TimerEvent.TIMER_COMPLETE,errorTick);
			finishT.addEventListener(TimerEvent.TIMER_COMPLETE,finishTick);
			mouseChildren=false;
			name=KeyboardControl.TEST;
			top.defaultTextFormat=new TextFormat(Facade.SPACE_FONT,24,Facade.GOLD,true);
			bottom.defaultTextFormat=new TextFormat(Facade.SPACE_FONT,24,Facade.RED,true);
			top.filters=[new GlowFilter(Facade.GOLD_OVER,0.3)];
			bottom.filters=[new GlowFilter(Facade.RED,0.4)];
			top.wordWrap=bottom.wordWrap=true;
			top.width=bottom.width=490;
			top.height=bottom.height=230;
			top.x=bottom.x=30;
			top.y=30;
			bottom.y=290;
			addChild(top);
			addChild(bottom);
			errorS.graphics.lineStyle(10,0xff0000);
			errorS.graphics.moveTo(50,50);
			errorS.graphics.lineTo(500,500);
			errorS.graphics.moveTo(500,50);
			errorS.graphics.lineTo(50,500);
			finishB=new MenuBox("",175,225,200,100);
			top.text="An evil galactic emperor from a far away star system is sending his fleet to destroy Earth.  Earth's people are frightened, fearing that they will be wiped out entirely.  Fortunately, they have just constructed a super advanced spacecraft that is ready to take on the evil emperor's fleet.";
			numWords=top.text.split(/\W+/).length;
		}
				
		public function update(s:String){
			if (s.toLowerCase()==top.text.charAt(bottom.length).toLowerCase()){
				bottom.appendText(s);
				if(bottom.numLines>3){
					top.scrollV=bottom.scrollV=bottom.numLines-3;
				}
				Facade.stage.focus=Facade.typingTest.top;
				top.setSelection(0,bottom.length);
				if (bottom.length==1){
					countT=(new Date()).time;
				}else if (bottom.length==top.length){
					countT=numWords/((new Date()).time-countT)*60000;
					Facade.gameM.wpm=countT;
					addChild(finishB);
					finishB.text="Your typing speed is:\n   "+String(int(countT))+" WPM.";
					finishT.reset();
					finishT.start();
					if (Facade.gameM.achieve[4]==0){
						Facade.gameM.achieve[4]=1;
						new PopAchieve(StringData.EXPLORER,1);
					}
				}
			}else{
				addChild(errorS);
				errorT.reset();
				errorT.start();
			}
		}
		
		public function errorTick(e:TimerEvent){
			removeChild(errorS);
		}
		
		public function finishTick(e:TimerEvent=null){
			Facade.transition.add(0,Facade.menuUI,this);
			removeChild(finishB);
			bottom.text="";
			top.setSelection(0,0);
			finishT.stop();
		}
		
		public function cancel(){
			if (contains(finishB)){
				finishTick();
			}else if (Facade.stage.contains(Facade.transition)==false){
				addChild(finishB);
				finishB.text="Test Cancelled.\n wpm remains at "+String(Facade.gameM.wpm)+".";
				finishT.reset();
				finishT.start();
			}
		}
	}
}