package{
	
	public class BossEvent{
		public var distance:Number;
		public var type:int;
		
		public function BossEvent(_distance:Number,_type:int){
			distance=_distance;
			type=_type;
		}
		
		public function run(){
			if (Facade.gameM.boss==null){
				if (Facade.gameM.tutFlags[9]==false){
					new PopBox(PopBox.tutText(9));
					Facade.gameM.tutFlags[9]=true;
				}
				Facade.gameM.boss=new BossShip(type);
			}
		}
	}
}