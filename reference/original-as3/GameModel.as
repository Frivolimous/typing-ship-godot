package{
	import flash.net.SharedObject;
	
	public class GameModel{
		const VERSION:int=10;
		
		public var starfield:Starfield=new Starfield(),
				running:Boolean,
				canType:Boolean,
				bossMode:Boolean,
				perfect:Boolean,
				distance:Number=0,
				maxDistance:Number,
				scoreH:int=0,
				scoreC:int=0,
				levelData:Array,
				level:int=0,
				missedShips:int=0,
				totalShips:int=0,
				invincible:Boolean,
				wpm:int=40,

				pShip:PlayerShip=new PlayerShip(),
				boss:BossShip,
				gObjects:Array=new Array(),
				gOverflow:Array=new Array(),
				hull:ShipSystem=new ShipSystem(),
				tutFlags:Array=new Array(11),
				_Save:SharedObject=SharedObject.getLocal("SAVE");
		
		public function init(){
			for (var i:int=0;i<tutFlags.length;i+=1){
				tutFlags[i]=false;
			}
			if ((_Save.size==0)||(_Save.data.version!=VERSION)){
				clearData();
			}
		}
		
		
		public function tutReset(){
			if (wpm>50){
				for (var i:int=0;i<tutFlags.length;i+=1){
					tutFlags[i]=true;
				}
			}
		}
		
		public function clearData(){
			_Save.data.achieve= [0,0,0,0,0,0];
			_Save.data.achieveC=[0,0,0,0,0,0];
			//unlockAll();
			_Save.data.levelScore=[[0,0,0,0],[0,0,0,0],[0,0,0,0,0],[0,0,0,0],[0,0,0,0],[0,0,0,0],[0,0,0,0],[0,0,0,0],[0,0,0,0],[0,0,0,0],[0,0,0,0],[0,0,0,0]];
			_Save.data.version=VERSION;
		}
		
		public function unlockAll(){
			_Save.data.levelScore=[[100,1,0,0],[100,1,0,0],[100,1,0,0],[100,1,0,0],[100,1,0,0],[100,1,0,0],[100,1,0,0],[100,1,0,0],[100,1,0,0],[100,1,0,0],[100,1,0,0],[100,1,0,0]];
		}
		
		public function get achieve():Array{
			return _Save.data.achieve;
		}
		
		public function set achieve(a:Array){
			throw(new Error("Can't change this!"));
		}
		
		public function get achieveC():Array{
			return _Save.data.achieveC;
		}
		
		public function set achieveC(a:Array){
			throw(new Error("Can't change this!"));
		}
		
		public function getLevelScore(i:int):Array{
			return _Save.data.levelScore[i];
		}
		
		public function get highscore():int{
			var m:int=0;
			for (var i:int=0;i<_Save.data.levelScore.length;i+=1){
				m+=_Save.data.levelScore[i][0];
			}
			return m;
		}
		
		public function set highscore(i:int){
			throw(new Error("Can't change high score!"));
		}
		
		public function get score():int{
			return scoreH;
		}
		
		public function set score(s:int){
			scoreH=s;
			Facade.gameUI.showScore(scoreH);
		}
		
		public function tutOff(){
			for (var i:int=0;i<tutFlags.length;i+=1){
				tutFlags[i]=true;
			}
		}
		
		public function get diff():int{
			switch(wpm){
				case Facade.EASY: return 1;
				case Facade.NORMAL: return 2;
				case Facade.HARD: return 3;
				case Facade.EXTREME: return 4;
				case Facade.INSANE: return 5;
				default: throw(new Error("wpm value is wonky: "+String(wpm)));
			}
		}
		
		public function set diff(i:int){
			throw(new Error("property diff is read-only"));
		}
		
		public function hitRatio():Number{
			return ((totalShips-missedShips)/totalShips);
		}
	}
}