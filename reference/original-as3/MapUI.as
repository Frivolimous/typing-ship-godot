package  {
	import flash.display.Sprite;
	import flash.display.Bitmap;
	
	public class MapUI extends Sprite{
		var levels:Array=new Array();
		var diffSelect:DiffSelect=new DiffSelect();
		var back:Bitmap=new Bitmap(new MapB(550,550));

		public function MapUI() {
			addChild(back);
			var _name:String;
			for (var i:int=0;i<12;i+=1){
				if ((i+1)%4==0){
					_name="B";
				}else{
					_name=String(i+1-Math.floor((i+1)/4));
				}
				levels.push(new LevelButton(_name,i));
				addChild(levels[i]);
			}
			levels[0].x=138; levels[0].y=365;
			levels[1].x=216; levels[1].y=314;
			levels[2].x=307; levels[2].y=314;
			levels[3].x=405; levels[3].y=324;
			levels[4].x=478; levels[4].y=285;
			levels[5].x=453; levels[5].y=208;
			levels[6].x=374; levels[6].y=204;
			levels[7].x=296; levels[7].y=203;
			levels[8].x=193; levels[8].y=193;
			levels[9].x=106; levels[9].y=165;
			levels[10].x=148; levels[10].y=72;
			levels[11].x=251; levels[11].y=40;
		}
		
		public function navIn(){
			if (contains(diffSelect)){
				removeChild(diffSelect);
			}
			
			var _levelD:Array;
			for (var i:int=0;i<levels.length;i+=1){
				_levelD=Facade.gameM.getLevelScore(i);
				if (_levelD[1]==0){
					if ((i==0)||(Facade.gameM.getLevelScore(i-1)[1]>0)){
						levels[i].update(0);
					}else{
						levels[i].update(-1);
					}
				}else{
					levels[i].update(_levelD[1],_levelD[2],_levelD[3]);
				}
			}
		}
		
		public function onTick(){
			for (var i:int=0;i<12;i+=1){
				levels[i].onTick();
			}
		}
	}
}
