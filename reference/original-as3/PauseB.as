package  {
	import flash.display.MovieClip;
	
	public class PauseB extends MovieClip{
		var output:Function;

		public function PauseB() {
			output=run;
			try{
				name=KeyboardControl.BUTTON;
			}catch(e:Error){}
			stop();
			buttonMode=true;
		}
		
		function run(){
			if (currentFrame==1){
				Facade.gameC.pauseGame(true);
			}else{
				Facade.gameC.pauseGame(false);
			}
		}
		
		public function over(b:Boolean){
		}
	}
	
}
