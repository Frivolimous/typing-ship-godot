package  {
	import flash.display.Shape;
	
	public class Particle extends Shape{
		var vX:Number;
		var vY:Number;
		var timer:Number=0;
		var life:int;

		public function Particle(color:uint,_x:Number,_y:Number,_r:Number,_d:Number,_v:Number,_l:int){
			graphics.beginFill(color);
			graphics.drawCircle(0,0,_r);
			vX=_v*Math.sin(_d);
			vY=_v*Math.cos(_d);
			x=_x+vX*10;
			y=_y+vY*10;

			life=_l;
		}
		
		public function update(i:int){
			x+=vX*i/60;
			y+=vY*i/60;
			timer+=5*i/60;
			alpha=0.2+0.8*(1-timer/life);
		}
	}	
}
