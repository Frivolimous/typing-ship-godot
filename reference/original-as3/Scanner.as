package  {
	import flash.geom.Point;

	public class Scanner extends GameObject{
		var count:int;
		var toCount:int;
		var origin:GameSprite;
		
		public function Scanner(_v:BossShip){
			wordSize=3;
			addWord();
			do{
				do{
					x=Math.random()*_v.view.bitmapData.width;
					y=Math.random()*_v.view.bitmapData.height;
				}while (_v.view.bitmapData.getPixel32(x,y)<0x01000000);			
				if (_v.index==0){
					if ((_v.over!=null)&&(SpriteSheets.bRes[_v.index][0].getPixel32(x,y)>=0x01000000)){
						y=500;
						continue;
					}
				}
				
				x*=_v.scale;
				y*=_v.scale;
				x-=_v.view.width/2;
				y-=_v.view.height/2;
				var _point:Point=Facade.gameUI.gameV.globalToLocal(Facade.gameM.boss.localToGlobal(new Point(this.x,this.y)));
				x=_point.x;
				y=_point.y;
			}while((y>475)||(x<35));
			count=300;
			graphics.lineStyle(3,0xff5500);
			graphics.drawCircle(0,0,count);
			graphics.drawCircle(0,0,1);
			Facade.gameUI.gameV.addChild(this);
		}
		
		public function remove(){
			Facade.gameM.boss.scanner=null;
			parent.removeChild(this);
			recycleWord();
		}
		
		public function scan(){
			toCount+=30;
			if (count-toCount>5){
				addWord();
			}else if (wordSize<10){
				wordSize=10;
				addWord();			
			}else{
				new LaserEffect(Facade.gameM.pShip,this);
				Facade.gameM.starfield.explosion(x,y,20);
				remove();
				Facade.gameM.boss.injure();
			}
		}
		
		public function update(){
			if (textObject!=null){
				textObject.rotation=0-this.rotation-parent.rotation;
			}
			if ((count>0)&&(toCount>0)){
				if (count<5){
					count=5;
				}
				count-=3;
				toCount-=3;
				graphics.clear()
				graphics.lineStyle(3,0xff5500);
				graphics.drawCircle(0,0,count);
				graphics.drawCircle(0,0,1);
				if (count<=5){
					count=-1;
				}
			}
		}
	}
}
