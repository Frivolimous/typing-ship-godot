package{
	
	public class SpawnEvent{
		public var distance:Number;
		public var type:String;
		public var x:Number, y:Number;
		public var commands:Array;
		public var a:Number;
		public var fires:int;
		public var killBy:int;
		public var shielded:Boolean;
		public var value:int;
		public var wordSize:int;
		public var turret:Boolean;
		public var index:int;
		
		public function SpawnEvent(_distance:Number,_t:String,_x:Number,_y:Number,_commands:Array=null){
			type=_t;
			distance=_distance;
			x=_x;
			y=_y;
			commands=_commands;
			a=0.5;
			value=5;
			killBy=ActionControl.MISSILE;
			
//shipstring: "abc"
// a = ship size   ([s]mall,[m]edium,[l]arge)
// b = weapon type ([m]issile,[l]aser,[s]uicide (small=landmine;medium=suicide ship))
// c = defense type([s]hield,[t]urret,[b]oth,[n]one)

			switch(_t.substring(0,2)){
				case "sm": index=0; break;
				case "sl": index=1; break;
				case "ss": index=2; break;
				case "mm": index=3; break;
				case "ml": index=4; break;
				case "ms": index=5; break;
				case "lm": index=6; break;
				case "ll": index=7; break;
				case "xm": index=8; break;
				case "nl": index=9; killBy=ActionControl.LASER; break;
				default: throw(new Error(_t.substring(0,2)+" is not a recognized ship code."));
			}
			
			switch(_t.charAt(0)){
				case "s": wordSize=4; break;
				case "m": wordSize=6; value+=1; a-=0.2; break;
				case "l": wordSize=8; value+=3; a-=0.25; break;
				case "x": wordSize=7; value+=3; a-=0.1; break;
				case "n": wordSize=7; break;
				default: wordSize=-1; break;
			}

			switch (_t.charAt(1)){
				case "m": fires=ActionControl.MISSILE; break;
				case "l": fires=ActionControl.LASER; wordSize+=1; value+=1; a-=0.1; break;
				case "s": fires=ActionControl.SUICIDE; wordSize=3;
							killBy=ActionControl.LASER; value-=3; 
							if (_t.substring(0,1)=="s"){
								a-=0.2;
							}else{
								a+=0.2;
							}break;
				default: break;
			}
			if (_t.charAt(0)=="x"){
				fires=ActionControl.AUTO_MISSILE;
			}
			
			switch (_t.charAt(2)){
				case "s": shielded=true; value+=2; break;
				case "t": turret=true; value+=2; break;
				case "b": shielded=true; turret=true; value+=5; break;
				default: break;
			}
			a*=Facade.gameM.wpm/100;
		}
		
		public function run(_return:Boolean=false):EnemyShip{
			Facade.gameM.totalShips+=1;
			if (_return){
				return new EnemyShip(index,x,y,commands,a,wordSize,fires,killBy,value,shielded,turret);
			}else{
				Facade.gameM.gObjects.push(new EnemyShip(index,x,y,commands,a,wordSize,fires,killBy,value,shielded,turret));
				return null;
			}
		}
	}
}