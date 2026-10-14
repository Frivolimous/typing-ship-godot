package{
	
	public class ObjectCommand{
		public var x:Number,
				y:Number,
				timer:int,
				fire:Boolean,
				move:Boolean;
		
		public function ObjectCommand(_x:Number=0,_y:Number=0,_timer:int=0,_move:Boolean=true,_fire:Boolean=false){
			if ((_x>0)&&(_x<GameUI.VIEW_WIDTH)){
				x=_x+Math.random()*20-10;
				if (x<0) x=0;
				if (x>GameUI.VIEW_WIDTH) x=GameUI.VIEW_WIDTH;
			}else{
				x=_x;
			}
			if ((_y>0)&&(_y<GameUI.VIEW_HEIGHT)){
				y=_y+Math.random()*20-10;
				if (y<0) y=0;
				if (y>GameUI.VIEW_HEIGHT) y=GameUI.VIEW_HEIGHT;
			}else{
				y=_y;
			}
			timer=_timer*400/Facade.gameM.wpm;
			move=_move;
			fire=_fire;
		}
		
		public function condition(v:*):Boolean{
			if (move){
				if ((Math.abs(v.x-x)<20)&&(Math.abs(v.y-y)<20)){
					return true;
				}
			}else{
				if (timer<=0){
					return true;
				}else {
					timer-=1;
				}
			}
			return false;
		}
		public function clone():ObjectCommand{
			return new ObjectCommand(x,y,timer*Facade.gameM.wpm/400,move,fire);
		}
		public function mirror():ObjectCommand{
			return new ObjectCommand(GameUI.VIEW_WIDTH-x,y,timer*Facade.gameM.wpm/400,move,fire);
		}
		public function offset(xOff:Number,yOff:Number):ObjectCommand{
			return new ObjectCommand(x+xOff,y+yOff,timer*Facade.gameM.wpm/400,move,fire);
		}
	}
}