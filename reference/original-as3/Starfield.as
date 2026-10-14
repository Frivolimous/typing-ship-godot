package{
	import flash.display.Bitmap;
	import flash.display.BitmapData;
	import flash.filters.GlowFilter;
	import flash.geom.Point;
	import flash.display.Shape;
	import flash.display.Sprite;
	
	public class Starfield extends Bitmap{
		public var stars:Array=new Array();
		public var particles:Array=new Array();
		public var objects:Array=new Array();
		public var trueHeight:int;
		public var nebTick:int;
		public var back:Bitmap;
		var soundC:SoundControl;
		var gameV:Sprite;
		var wpm:int=60;

		public function Starfield(){
			back=new Bitmap;
			setBack(0);
			Facade.stage.addChildAt(back,0);
			soundC=Facade.soundC;
			gameV=Facade.gameUI.gameV;
			trueHeight=550;
			bitmapData=new BitmapData(GameUI.VIEW_WIDTH,trueHeight,true,0);
			for (var i:int=0;i<40;i+=1){
				stars.push(new Star(1+Math.random()*1,Math.floor(Math.random()*550),Math.random()*trueHeight));
			}
			nebTick=9000;
		}
		
		public function setBack(i:int){
			back.bitmapData=SpriteSheets.back[Math.floor(i/4)];
		}
		
		public function update(){
			if (Math.random()<0.3*wpm/60){
				stars.push(new Star(1+Math.random()*3,Math.floor(Math.random()*stage.stageWidth)));
				if (stars[stars.length-1].v>3){
					stars.push(new Star(stars[stars.length-1].v,stars[stars.length-1].x+1));
					stars.push(new Star(stars[stars.length-1].v,stars[stars.length-1].x,-1));
					stars.push(new Star(stars[stars.length-1].v,stars[stars.length-1].x-1,-1));
				}else if (stars[stars.length-1].v>2){
					stars.push(new Star(stars[stars.length-1].v,stars[stars.length-1].x+1));
				}
			}
			if (nebTick>100000/wpm){
				objects.push(new Nebula());
				stage.addChildAt(objects[objects.length-1],1);
				nebTick=0;
			}else{
				nebTick+=1;
			}
			var i:int=0;
			bitmapData.lock();
			while (i<stars.length){
				bitmapData.setPixel32(stars[i].x,stars[i].y,0);
				stars[i].y+=stars[i].v*wpm/60;
				if (stars[i].y>trueHeight){
					stars.splice(i,1);
				}else{
					bitmapData.setPixel32(stars[i].x,stars[i].y,0xffffffff);
					i+=1;
				}
			}
			bitmapData.unlock();
			i=0;
			while(i<objects.length){
				objects[i].y+=wpm/100;
				if (objects[i].y>trueHeight){
					objects[i].parent.removeChild(objects[i]);
					objects.splice(i,1);
				}else{
					i+=1;
				}
			}
			i=0;
			while (i<particles.length){
				particles[i].update(wpm);
				if (particles[i].timer>particles[i].life){
					gameV.removeChild(particles[i]);
					particles.splice(i,1);
				}else{
					i+=1;
				}
			}
		}
		
		public function explosion(_x:Number=-1,_y:Number=-1,_c:int=50){
			if (_c>20){
				soundC.sound(SoundControl.EXPLODE_B);
			}else{
				soundC.sound(SoundControl.EXPLODE_S);
			}

			var temp:uint=0xcccccc;
			if (_x==-1){
				var _point:Point=new Point(Math.random()*550,Math.random()*550);
			}else{
				_point=new Point(_x,_y);
			}
			for (var i:int=0;i<_c;i++){
				var _shape:Particle=new Particle(temp,_point.x,_point.y,1+Math.random()*4,Math.random()*2*Math.PI,Math.random()*0.5,200+Math.random()*100);
				
				gameV.addChildAt(_shape,0);
				particles.push(_shape);
				
			}
		}

	}
}