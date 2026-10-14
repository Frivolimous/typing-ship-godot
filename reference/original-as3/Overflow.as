package  {
	
	public class Overflow {
		var word:String;
		var delay:int=30;
		public function Overflow(s:String) {
			word=s;
			Facade.gameM.gOverflow.push(this);
		}

		public function update(){
			if (delay>0){
				delay-=1;
				return true;
			}else{
				remove();
				return false;
			}
		}
		
		public function remove(){
			Facade.wordList.push(word);
			Facade.gameM.gOverflow.splice(Facade.gameM.gOverflow.indexOf(this),1);
		}
	}
}
