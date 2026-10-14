package  {
	import flash.display.MovieClip;
	
	public class MenuBt extends MovieClip{
		var output:Function;

		public function MenuBt() {
			output=run;
			try{
				name=KeyboardControl.BUTTON;
			}catch(e:Error){}
			stop();
			buttonMode=true;
		}
		
		function run(){
			if (Facade.stage.contains(Facade.typingTest)){
				Facade.typingTest.cancel();
			}else{
				//Facade.gameUI.goMenu();
			}
		}
		
		public function over(b:Boolean){
		}
	}
	
}
