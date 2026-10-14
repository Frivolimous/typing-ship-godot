package  {
	import flash.display.Sprite;
	import flash.display.Bitmap;
	import flash.text.TextField;
	import flash.text.TextFormat;
	
	public class AchieveUI extends Sprite{
		var achieve:Array=new Array(6);
		var medalText:Array=new Array(6);
		var display:Array=[new TextField,new TextField,new TextField];
		var star:Array=[new Bitmap,new Bitmap,new Bitmap];
		
		public function AchieveUI() {
			var back:Bitmap=new Bitmap(new AchieveB(550,550));
			addChild(back);
			for (var i:int=0;i<6;i+=1){
				achieve[i]=new Award();
				achieve[i].x=13+(i%3)*187;
				achieve[i].y=44+Math.floor(i/3)*187;
				addChild(achieve[i]);
			}
			achieve[0].label.text=StringData.SOLDIER;
			medalText[0]=StringData.getAchieveArray(StringData.SOLDIER);
			achieve[1].label.text=StringData.CONQUEROR;
			medalText[1]=StringData.getAchieveArray(StringData.CONQUEROR);
			achieve[2].label.text=StringData.RIDDLER;
			medalText[2]=StringData.getAchieveArray(StringData.RIDDLER);
			achieve[3].label.text=StringData.DEFENDER;
			medalText[3]=StringData.getAchieveArray(StringData.DEFENDER);
			achieve[4].label.text=StringData.PERFECTIONIST;
			medalText[4]=StringData.getAchieveArray(StringData.PERFECTIONIST);
			achieve[5].label.text=StringData.EXPLORER;
			medalText[5]=StringData.getAchieveArray(StringData.EXPLORER);
			//didnt put in any of those
			for (i=0;i<3;i+=1){
				display[i].defaultTextFormat=new TextFormat(Facade.TITLE_FONT,11,Facade.GOLD,true);
				display[i].selectable=false;
				display[i].wordWrap=true;
				display[i].x=75;
				display[i].y=390+40*i;
				display[i].width=225;
				display[i].height=60;
				addChild(display[i]);
				star[i].bitmapData=SpriteSheets.star[i*2];
				addChild(star[i]);
			}
			display[0].y=390;
			display[1].y=434;
			display[2].y=488;
			
			var button:UIButton=new UIButton(StringData.DONE,close,Facade.GOLD,Facade.GOLD_OVER);
			button.x=355;
			button.y=440;
			addChild(button);
			button.scaleX=button.scaleY=2;
		}
		
		public function update(){
			for (var i:int=0;i<6;i+=1){
				achieve[i].trophy.bitmapData=SpriteSheets.achieve[Facade.gameM.achieve[i]];
			}
		}
		
		public function popText(_b:*=null){
			display[0].text=display[1].text=display[2].text="";
			for (var j:int=0;j<3;j+=1){
				star[j].bitmapData=SpriteSheets.star[j*2];
			}
			
			if (_b!=null){
				var i:int=achieve.indexOf(_b);
				display[0].text=medalText[i][0];
				if (Facade.gameM.achieve[i]>=1){
					display[1].text=medalText[i][1];
					star[0].bitmapData=SpriteSheets.star[1];
				}else{
					display[1].text="???";
				}
				if (Facade.gameM.achieve[i]>=2){
					display[2].text=medalText[i][2];
					star[1].bitmapData=SpriteSheets.star[3];
				}else{
					display[2].text="???";
				}
				if (Facade.gameM.achieve[i]>=3){
					star[2].bitmapData=SpriteSheets.star[5];
				}
			}
		}
		
		public function close(){
			Facade.transition.add(0xffffff,Facade.menuUI,this);
		}

	}
	
}
