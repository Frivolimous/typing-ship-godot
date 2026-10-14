package{
	import flash.display.Sprite;
	import flash.text.TextField;
	import flash.text.TextFormat;
	import flash.utils.Timer;
	import flash.events.TimerEvent;
	import flash.display.Bitmap;
	import flash.filters.GlowFilter;
	
	public class ScoreUI extends Sprite{
		var title:TextField=new TextField();
		var textA:Array=new Array(7);
		var score:int;
		var cMult:Number;
		var scoreT:Timer=new Timer(10,0);
		var index:int;
		var tickBy:int;
		var cLevel:Array;
		
		var back:Bitmap=new Bitmap(new ScoreB(510,425));
		
		public function ScoreUI(){
			mouseChildren=false;
			back.height=550;
			back.scaleX=back.scaleY;
			addChild(back);
			var _filter:GlowFilter=new GlowFilter(Facade.GOLD,1,5,5,2,1,true);
			var _format:TextFormat=new TextFormat(Facade.SPACE_FONT,30,0xffffff,true);
			for (var i:int=0;i<textA.length;i+=1){
				textA[i]=[new TextField,new TextField];
				textA[i][0].defaultTextFormat=textA[i][1].defaultTextFormat=_format;
				textA[i][1].autoSize="right";
				textA[i][0].width=textA[i][1].width=450;
				textA[i][0].x=25;
				textA[i][1].x=520;
				textA[i][0].y=textA[i][1].y=120+35*i;
				textA[i][0].filters=textA[i][1].filters=[_filter];
				addChild(textA[i][0]);
				addChild(textA[i][1]);
			}
			textA[0][0].y=textA[0][1].y-=30;
			textA[5][0].scaleX=textA[5][0].scaleY=2
			textA[5][1].scaleX=textA[5][1].scaleY=2.5;
			textA[5][1].y+=60;
			textA[5][1].x=520;
			textA[5][0].filters=textA[5][1].filters=[new GlowFilter(Facade.GOLD,1,7,7,2,1,true)];
			textA[6][0].y=textA[6][1].y=500;
			scoreT.addEventListener(TimerEvent.TIMER,scoreTick);
			title.defaultTextFormat=new TextFormat(Facade.SPACE_FONT,50,0xffffff,true);
			title.x=30;
			title.y=5;
			title.width=500;
			title.filters=[new GlowFilter(Facade.GOLD,1,4,4,2,1,true),new GlowFilter(Facade.GOLD,1,4,4,2,1)];
			addChild(title);
		}
		
		public function navIn(){
			clear();
			score=Facade.gameM.score-Facade.gameM.scoreC;
			scoreT.reset();
			scoreT.start();
		}
				
		public function clear(){
			for (var i:int=0;i<textA.length;i+=1){
				textA[i][0].text="";
				textA[i][1].text="";
			}
			score=0;
			index=0;
		}
		
		public function scoreTick(e:TimerEvent){
			switch(index){
				case 0: //set up initial score, remove points for extra letters
					if (textA[index][0].text==""){
						textA[index][0].text=StringData.LEVEL_SCORE;
						textA[index][1].text=String(score);
						title.text=StringData.levelComplete(Facade.gameM.level+1);
						cLevel=Facade.gameM.getLevelScore(Facade.gameM.level);
						if (Facade.gameM.diff>cLevel[1]){
							cLevel[1]=Facade.gameM.diff;
						}
					}else{
						if (Facade.gameUI.typeText.length>0){
							Facade.gameM.perfect=false;
							Facade.gameUI.removeLetter(1);
							score-=1;
							textA[index][1].text=String(score);
						}else{
							index+=1;
						}
					}break;
				case 1: //difficulty bonus tick
					if (textA[index][0].text==""){
						cMult=1+Facade.gameM.wpm/100;
						textA[index][0].text=StringData.DIFF_BONUS+cMult.toFixed(2);
					}else if (textA[index][1].text==""){
						textA[index][1].text=String(score);
						tickBy=score*(cMult-1)/72;
						if (tickBy<1) tickBy=1;
						score*=cMult;
					}else if (int(textA[index][1].text)<score){
						textA[index][1].text=String(int(textA[index][1].text)+tickBy);
						Facade.soundC.sound(SoundControl.SCORE);
					}else{
						textA[index][1].text=String(score);
						index+=1;
					}break;
				case 2: //health bonus tick
					if (textA[index][0].text==""){
						if (Facade.gameM.pShip.injured){
							cMult=1+Facade.gameM.pShip.health/PlayerShip.MAX_HEALTH*0.2;
							textA[index][0].text=StringData.HEALTH_BONUS+cMult.toFixed(2);
							if (Facade.gameM.pShip.health>=PlayerShip.MAX_HEALTH){
								if (cLevel[2]<2) cLevel[2]=2;
							}else if (Facade.gameM.pShip.health>=PlayerShip.MAX_HEALTH/2){
								if (cLevel[2]<1) cLevel[2]=1;
							}else{
								if (cLevel[2]<0) cLevel[2]=0;
							}
						}else{
							cMult=2;
							textA[index][0].text=StringData.HEALTH_PERFECT+cMult.toFixed(2);
							if (cLevel[2]<3) cLevel[2]=3;
							if (Facade.gameM.achieve[3]==0){
								Facade.gameM.achieve[3]=1;
								new PopAchieve(StringData.DEFENDER,1);
							}else if (Facade.gameM.achieve[3]>=1){
								j=0;
								for (i=0;i<12;i+=1){
									if (Facade.gameM.getLevelScore(i)[2]==3){
										j+=1;
									}
								}
								if (Facade.gameM.achieve[3]==1){
									if (j>=5){
										Facade.gameM.achieve[3]=2;
										new PopAchieve(StringData.DEFENDER,2);
									}
								}else if (Facade.gameM.achieve[3]==2){
									if (j>=12){
										Facade.gameM.achieve[3]=3;
										new PopAchieve(StringData.DEFENDER,3);
									}
								}
							}
						}
					}else if (textA[index][1].text==""){
						textA[index][1].text=String(score);
						tickBy=score*(cMult-1)/72;
						if (tickBy<1) tickBy=1;
						score*=cMult;
					}else if (int(textA[index][1].text)<score){
						textA[index][1].text=String(int(textA[index][1].text)+tickBy);
						Facade.soundC.sound(SoundControl.SCORE);
					}else{
						textA[index][1].text=String(score);
						index+=1;
					}break;
				case 3:
					if (textA[index][0].text==""){
						if (Facade.gameM.hitRatio()<1){
							cMult=1+Facade.gameM.hitRatio()/2;
							textA[index][0].text=StringData.ACC_BONUS+cMult.toFixed(2);
						}else{
							cMult=2;
							textA[index][0].text=StringData.ACC_PERFECT+cMult.toFixed(2);
						}
					}else if (textA[index][1].text==""){
						textA[index][1].text=String(score);
						tickBy=score*(cMult-1)/72;
						if (tickBy<1) tickBy=1;
						score*=cMult;
					}else if (int(textA[index][1].text)<score){
						textA[index][1].text=String(int(textA[index][1].text)+tickBy);
						Facade.soundC.sound(SoundControl.SCORE);
					}else{
						textA[index][1].text=String(score);
						index+=1;
					}break;
				case 4:
					textA[index][0].text="----------------------------------------------";
					index+=1;
					break;
				case 5:
					textA[index][0].text=StringData.FINAL_SCORE
					textA[index][1].text=String(score);
					
					if (score>cLevel[0]){
						cLevel[0]=score;
					}
					if (Facade.gameM.hitRatio()==1){
						if (cLevel[3]<3) cLevel[3]=3;
						if (Facade.gameM.achieve[4]==0){
							Facade.gameM.achieve[4]=1;
							new PopAchieve(StringData.PERFECTIONIST,1);
						}else if (Facade.gameM.achieve[4]==1){
							var j:int=0;
							for (var i:int=0;i<12;i+=1){
								if (Facade.gameM.getLevelScore(i)[3]==3){
									j+=1;
								}
							}
							if (j>=5){
								Facade.gameM.achieve[4]=2;
								new PopAchieve(StringData.PERFECTIONIST,2);
							}
						}else if (Facade.gameM.achieve[4]==2){
							j=0;
							for (i=0;i<12;i+=1){
								if (Facade.gameM.getLevelScore(i)[3]==3){
									j+=1;
								}
							}
							if (j>=12){
								Facade.gameM.achieve[4]=3;
								new PopAchieve(StringData.PERFECTIONIST,3);
							}
						}
					}else if (Facade.gameM.hitRatio()>0.75){
						if (cLevel[3]<2) cLevel[3]=2;
					}else if (Facade.gameM.hitRatio()>0.5){
						if (cLevel[3]<1) cLevel[3]=1;
					}else{
						if (cLevel[3]<0) cLevel[3]=0;
					}
					index+=1;
					break;
				case 6:
					scoreT.stop();
					textA[index][0].text=StringData.SPACEBAR;
					break;
			}
		}
	}
}