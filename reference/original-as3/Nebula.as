package  {
	import flash.display.Bitmap;
	
	public class Nebula extends Bitmap{

		public function Nebula() {
			bitmapData=SpriteSheets.nebula[Math.floor(Math.random()*3)];
			width=50+Math.random()*100;
			scaleY=scaleX;
			x=20+Math.random()*(Facade.stage.stageWidth-20-width);
			y=0-height;
		}

	}
	
}
