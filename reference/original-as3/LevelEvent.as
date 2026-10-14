package{
	
	public class LevelEvent{
		public static var LOOP:String="Loop";
		public static var WAIT:String="Wait";
		public static var JUMP:String="Jump";
		
		public var type:String;
		public var index:Number;
		public var distance:Number;
		
		public function LevelEvent(_dist:Number,_type:String,_i:Number){
			distance=_dist;
			type=_type;
			index=_i;
			}
		
		public function run(){
			if (type==LOOP){
				var tArray:Array=LevelData.loadData(Facade.gameM.level).splice(index);
				for (var i:int=0;i<tArray.length;i+=1){
					tArray[i].distance+=distance;
					Facade.gameM.levelData.push(tArray[i]);
				}
			}else if (type==JUMP){
				Facade.gameM.distance+=index;
			}
		}
		
		public function shift(n:Number):*{
			distance+=n;
			
		}
	}
}