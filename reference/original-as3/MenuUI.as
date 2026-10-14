package{
	import flash.display.Sprite;
	import flash.text.TextField;
	import flash.text.TextFormat;
	import flash.display.Bitmap;
	import flash.filters.GlowFilter;
	import flash.filters.BevelFilter;
	import flash.geom.Point;
	import flash.net.URLRequest;
	import flash.net.navigateToURL;
	import flash.events.Event;
	import flash.geom.Rectangle;
	
	
	public class MenuUI extends Sprite{
		var startB:BitButton;
		var moreB:BitButton;
		var highB:BitButton;
		var achieveB:BitButton;
		var creditsB:BitButton;
		var highC:TextField=new TextField;
		var confirmM:ConfirmMenu=new ConfirmMenu();
		var credits:Sprite=new Sprite;
		var achieveUI:AchieveUI=new AchieveUI;
		var mapUI:MapUI=new MapUI();
		var back:Bitmap=new Bitmap(new MenuB(550,550));
		var back2:Bitmap=new Bitmap;
		//var title0:TextField=new TextField;
		//var title1:TextField=new TextField;
		var title0:Bitmap=new Bitmap(SpriteSheets.title0);
		var title1:Bitmap=new Bitmap(SpriteSheets.title1);
		
		var counter:int=0;
		var introCounter:int=1;
		const INTRO_2:int=100;
		var mapMask:Sprite=new Sprite;
		
		public function titleFormat(_field:TextField){
			_field.defaultTextFormat=new TextFormat(Facade.ROSE_FONT,60,0x1b4e92);
			_field.width=500;
			_field.height=100;
			_field.selectable=false;
			_field.filters=[new BevelFilter(5,30,16777215,.75,0,.75),new GlowFilter(0x1b4e92,1,1,1,1,1,false)];
			//_field.filters=[new GlowFilter(Facade.GOLD,1,5,5,2,1,true),new GlowFilter(Facade.GOLD,1,5,5,2,1)];
		}
									
		public function MenuUI(){
			addChild(back);
			addChild(back2);
			/*titleFormat(title0);
			title0.text=StringData.TITLE0;
			title0.x=550;
			title0.y=360;
			addChild(title0);
			titleFormat(title1);
			title1.text=StringData.TITLE1;
			title1.x=550;
			title1.y=420;
			addChild(title1);*/
			addChild(title0);
			addChild(title1);
			highC.defaultTextFormat=new TextFormat(Facade.TITLE_FONT,16,0xffffff,true);
			highC.filters=[new GlowFilter(Facade.BLUE,1,3,3,2,1)];
			highC.width=100;
			highC.selectable=false;
			highC.x=5;
			highC.y=560;
			highC.autoSize="right";
			addChild(highC);
			/*startB.y=188;
			startB.scaleX=startB.scaleY=1.3;
			highB.y=215;
			achieveB.y=235;
			moreB.y=255;
			creditsB.y=275;
			startB.x=highB.x=achieveB.x=moreB.x=creditsB.x=600;*/
			startB=new BitButton(SpriteSheets.startBt,startGame,this,new Rectangle(330,175,210,60));
			highB=new BitButton(SpriteSheets.highBt,goHigh,this,new Rectangle(390,235,150,42));
			achieveB=new BitButton(SpriteSheets.achieveBt,goAchieve,this,new Rectangle(380,277,160,40));
			moreB=new BitButton(SpriteSheets.moreBt,goMore,this,new Rectangle(395,317,145,40));
			creditsB=new BitButton(SpriteSheets.creditsBt,goCredits,this,new Rectangle(420,357,120,50));
			startB.x=highB.x=achieveB.x=moreB.x=creditsB.x=250;
			var _bm:Bitmap=new Bitmap(SpriteSheets.credits);
			credits.addChild(_bm);
			credits.buttonMode=true;
			mapMask.graphics.beginFill(0);
			mapMask.graphics.drawRect(0,0,550,550);
			mapUI.mask=mapMask;
		}
		
		var twinkleC:int=0
		function onTick(e:Event){
			counter+=1;

			if (counter<=20){
				back2.alpha=counter/20;
			}else{
				back2.alpha=(40-counter)/20;
			}
			if (counter==40){
				twinkleC+=1;
				if (twinkleC>2) twinkleC=0;
				back2.bitmapData=SpriteSheets.twinkle[twinkleC];
				counter=0;
			}
			
			if (introCounter>0){
				if (introCounter<INTRO_2){
					intro1();
				}else if (introCounter>=INTRO_2){
					intro2();
				}
			}
			if (contains(mapUI)){
				mapUI.onTick();
			}
		}
		
		public function navIn(){
			highC.text=String(Facade.gameM.highscore);
			if (contains(mapUI)){
				mapUI.navIn();
			}
			addEventListener(Event.ENTER_FRAME,onTick);
		}
		
		public function navOut(){
			removeEventListener(Event.ENTER_FRAME,onTick);
		}
		
		function intro1(){
			introCounter+=1;
			if (introCounter<=10){
				//startB.x=340+(10-introCounter)*100;
				startB.x=(10-introCounter)*50;
			}
			if ((introCounter>3)&&(introCounter<=13)){
				//highB.x=358+(13-introCounter)*100;
				highB.x=(13-introCounter)*50;
			}
			if ((introCounter>6)&&(introCounter<=16)){
				//achieveB.x=362+(16-introCounter)*100;
				achieveB.x=(16-introCounter)*50;
			}
			if ((introCounter>9)&&(introCounter<=19)){
				//moreB.x=366+(19-introCounter)*100;
				moreB.x=(19-introCounter)*50;
			}
			if ((introCounter>12)&&(introCounter<=22)){
				//creditsB.x=370+(22-introCounter)*100;
				creditsB.x=(22-introCounter)*50;
			}
			if (introCounter<=28){
				/*title0.x=20-(28-introCounter)*100;
				title1.x=195+(28-introCounter)*100;*/
				title0.x=0-(28-introCounter)*100;
				title1.x=(28-introCounter)*100;
			}
			if (introCounter>30){
				introCounter=0;
			}
		}
			
		function intro2(){
			introCounter+=1;
			if (introCounter<=INTRO_2+20){
				/*title0.x=20+600*((introCounter-INTRO_2)/20);
				title1.x=195-600*((introCounter-INTRO_2)/20);*/
				title0.x=30*(introCounter-INTRO_2);
				title1.x=0-30*(introCounter-INTRO_2);
			}
			if (introCounter<=INTRO_2+10){
				//creditsB.x=370+30*((introCounter-INTRO_2)/10);
				//creditsB.y=275+245*((introCounter-INTRO_2)/10);
				creditsB.scaleX=creditsB.scaleY=1-0.1*((introCounter-INTRO_2)/10);
				creditsB.x=7*((introCounter-INTRO_2)/10);
				creditsB.y=180*((introCounter-INTRO_2)/10);
				creditsB.updateHA();
			}
			if ((introCounter>INTRO_2+3)&&(introCounter<=INTRO_2+13)){
				//moreB.x=366-91*((introCounter-INTRO_2-3)/10);
				//moreB.y=255+265*((introCounter-INTRO_2-3)/10);
				moreB.scaleX=moreB.scaleY=1-0.1*((introCounter-INTRO_2-3)/10);
				moreB.x=-87*((introCounter-INTRO_2-3)/10);
				moreB.y=220*((introCounter-INTRO_2-3)/10);
				moreB.updateHA();
			}
			if ((introCounter>INTRO_2+6)&&(introCounter<=INTRO_2+16)){
				//achieveB.x=362-225*((introCounter-INTRO_2-6)/10);
				//achieveB.y=235+285*((introCounter-INTRO_2-6)/10);
				achieveB.scaleX=achieveB.scaleY=1-0.1*((introCounter-INTRO_2-6)/10);
				achieveB.x=-217*((introCounter-INTRO_2-6)/10);
				achieveB.y=255*((introCounter-INTRO_2-6)/10);
				achieveB.updateHA();
			}
			if ((introCounter>INTRO_2+9)&&(introCounter<=INTRO_2+19)){
				//highB.x=358-348*((introCounter-INTRO_2-9)/10);
				//highB.y=215+290*((introCounter-INTRO_2-9)/10);
				highB.scaleX=highB.scaleY=1-0.1*((introCounter-INTRO_2-9)/10);
				highB.x=-348*((introCounter-INTRO_2-9)/10);
				highB.y=275*((introCounter-INTRO_2-9)/10);
				highC.y=563-40*((introCounter-INTRO_2-9)/10);
				highB.updateHA();
			}
			if ((introCounter>INTRO_2+12)&&(introCounter<=INTRO_2+22)){
				startB.x=300*((introCounter-INTRO_2-12)/10);
				startB.updateHA();
			}
			if (introCounter<=INTRO_2+30){
				mapMask.y=550*(30-introCounter+INTRO_2)/30;
			}
			if (introCounter>INTRO_2+30){
				introCounter=0;
			}
		}
		
		public function startGame(){
			introCounter=INTRO_2;
			addChildAt(mapUI,1);
			addChildAt(mapMask,2);
			mapUI.navIn();
			
		}
		
		public function goTest(){
			Facade.transition.add(0,Facade.typingTest,this);
		}
		
		public function goCredits(){
			Facade.transition.add(0xffffff,credits,this);
		}
		
		public function goAchieve(){
			achieveUI.update();
			Facade.transition.add(0xffffff,achieveUI,this);
		}
		
		public function goMore(){
			Facade.goSponsor();
		}
		
		public function goHigh(){
			Facade.highScoreDisplay();
		}
	}
}