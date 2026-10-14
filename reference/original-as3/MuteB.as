package  {
	import flash.display.MovieClip;
	
	public class MuteB extends MovieClip{
		var output:Function;

		public function MuteB() {
			output=run;
			try{
				name=KeyboardControl.MUTE;
			}catch(e:Error){}
			stop();
			buttonMode=true;
		}
		
		function run(_x:int){
			switch (currentFrame){
				case 1: if (_x>0) goto(3); else {goto(2); Facade.soundC.stop();} break;
				case 2: if (_x>0) goto(4); else {goto(1); Facade.soundC.play();} break;
				case 3: if (_x>0) goto(1); else {goto(4); Facade.soundC.stop();} break;
				case 4: if (_x>0) goto(2); else {goto(3); Facade.soundC.play();} break;
			}
		}
		
		function goto(i:int){
			gotoAndStop(i);
			switch (currentFrame){
				case 1:
					Facade.soundC.muteM=false;
					Facade.soundC.muteS=false;
					break;
				case 2:
					Facade.soundC.muteM=true;
					Facade.soundC.muteS=false;
					break;
				case 3:
					Facade.soundC.muteM=false;
					Facade.soundC.muteS=true;
					break;
				case 4:
					Facade.soundC.muteM=true;
					Facade.soundC.muteS=true;
					break;
			}
		}
	}
}
