package{
	import flash.display.BitmapData;
	import flash.geom.Matrix;
	import flash.geom.Rectangle;
	import flash.filters.ColorMatrixFilter;
	import flash.geom.Point;
	import fl.motion.AdjustColor;

	
	public class SpriteSheets{
		//public static var player:BitmapData=new PlayerShipBD(214,100);
		public static var player:Array=[new PShip0(80,100),new PShip1(80,100),new PShip2(80,100)];
		public static var enemy:Array=[new Enemy0(77,100),new Enemy1(77,100),[new Enemy2a(300,300),new Enemy2a(300,300),new Enemy2a(300,300),new Enemy2a(300,300),new Enemy2a(300,300),new Enemy2b(300,300),new Enemy2c(300,300),new Enemy2d(300,300),new Enemy2d(300,300),new Enemy2d(300,300),new Enemy2b(300,300)],new Enemy3(77,100),new Enemy4(77,100),new Enemy5(77,100),new Enemy6(77,100),new Enemy7(77,100),new Enemy8(77,100),new BitmapData(77,100,true,0)];
		public static var boss:Array=[new Boss0(1000,571),new Boss1a(647,360),new Boss2(700,500)];
		public static var bRes:Array=[[new Door0(1000,571),new Door1(1000,571),new Door2(1000,571)],new Boss1b(647,360),[[new Boss2a(700,500),new Boss2b(700,500),new Boss2c(700,500)],[new Boss2L0(550,550),new Boss2L1a(550,550),new Boss2L1c(550,550),new Boss2L1b(550,550)]]];
		public static var pMissile:Array=[new PMiss0(300,400),new PMiss1(300,400),new PMiss2(300,400)];
		public static var eMissile:Array=[new EMiss0(300,300),new EMiss1(300,300),new EMiss2(300,300)];
		public static var nebula:Array=[new Nebula0(1280,1280),new Nebula1(800,800),new Nebula2(350,370)];
		public static var dash:Array=[new Dash0(500,500),new Dash1(500,500),new Dash2(500,500),new Dash3(500,500),new Dash4(500,500),new Dash5(500,500)];
		public static var achieve:Array=[new Achieve0(160,96),new Achieve1(160,96),new Achieve2(160,96),new Achieve3(160,96),new AchieveP(205,155)];
		public static var turret:BitmapData=new Turret0(700,500);
		public static var back:Array=[new StarBack0(550,550),new StarBack1(550,550),new StarBack2(550,550)];
		public static var twinkle:Array=[new MenuB1(550,550),new MenuB2(550,550),new MenuB3(550,550)];
		public static var credits:BitmapData=new CreditsB(550,550);
		public static var superman:BitmapData=new Superman(100,130);
		public static var lockC:BitmapData=new LockC(65,65);
		public static var nextC:BitmapData=new NextC(65,65);
		public static var completeC:BitmapData=new CompleteC(65,65);
		public static var MAX_FRAME:int=3;
		public static var healthFull:BitmapData=new HealthFull(212,11);
		public static var healthBase:BitmapData=new HealthBase(212,11);
		public static var title0:BitmapData=new Title0(550,550);
		public static var title1:BitmapData=new Title1(550,550);
		public static var health:Array=[new BitmapData(1,1,true,0),new Health1(12,12),new Health2(12,12),new Health3(12,12)];
		public static var target:Array=[new BitmapData(1,1,true,0),new Target1(12,12),new Target2(12,12),new Target3(12,12)];
		public static var star:Array=[new Star00(550,550),new Star01(550,550),new Star10(550,550),new Star11(550,550),new Star20(550,550),new Star21(550,550)];
		public static var startBt:BitmapData=new StartBt(550,550);
		public static var creditsBt:BitmapData=new CreditsBt(550,550);
		public static var highBt:BitmapData=new HighBt(550,550);
		public static var achieveBt:BitmapData=new AchieveBt(550,550);
		public static var moreBt:BitmapData=new MoreBt(550,550);
		
		var gameM:GameModel;
		var count:int;
		
		public function init(){
			gameM=Facade.gameM;
		}
		
		public static function recolour(){
			var _color:AdjustColor=new AdjustColor();
			_color.hue=Math.random()*360-180;
			_color.brightness=0;
			_color.saturation=100;
			_color.contrast=0;
			var _filter:ColorMatrixFilter=new ColorMatrixFilter(_color.CalculateFinalFlatArray());
			
			player[0].applyFilter(new PShip0(80,100),player[0].rect,new Point(0,0),_filter);
			player[1].applyFilter(new PShip1(80,100),player[0].rect,new Point(0,0),_filter);
			player[2].applyFilter(new PShip2(80,100),player[0].rect,new Point(0,0),_filter);
		}
		
		public function onTick(){
			count+=1;
			if (count>1){
				count=0;
				update(gameM.pShip);
				for (var i:int=0;i<gameM.gObjects.length;i+=1){
					if (gameM.gObjects[i] is GameSprite){
						update(gameM.gObjects[i]);
					}
				}
			}
		}
		
		public function update(_sprite:GameSprite,i:int=-1){
			if (_sprite.view.bitmapData==null){
				if (_sprite.viewSource is BitmapData){
					_sprite.view.bitmapData=_sprite.viewSource;
				}
				_sprite.view.scaleX=_sprite.view.scaleY=_sprite.scale;
			}
			if (_sprite.viewSource is Array){
				if (i==-1){
					_sprite.frame+=1;
				}else{
					_sprite.frame=i;
				}
				if (_sprite.frame>=_sprite.viewSource.length) _sprite.frame=0;
				_sprite.view.bitmapData=_sprite.viewSource[_sprite.frame];
			}
		}
	}
}