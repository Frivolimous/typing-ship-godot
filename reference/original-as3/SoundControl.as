package  {
	import flash.media.SoundChannel;
	import flash.utils.Timer;
	import flash.events.TimerEvent;
	import flash.media.SoundTransform;
	
	public class SoundControl {
		public static const TYPE:int=0,
							CLICK:int=1,
							SCORE:int=2,
							LASER:int=3,
							EXPLODE_S:int=4,
							EXPLODE_B:int=5,
							CHARGE:int=6,
							HEAL:int=7,
							EMP:int=8,
							BOSS_CHARGE:int=9,
							BOSS_FIRE:int=10,
							SUPERMAN:int=11,
							
							MAX_VOLUME:Number=0.5;
							
		var music:Array=[Music0,Music1,Music2,Music3,Music4];
		var soundA:Array=[TypeS,ClickS,ScoreS,LaserS,ExplodeSS,ExplodeBS,ChargeS,HealS,EmpS,BChargeS,BFireS,SuperS];
		var volumeA:Array=[1,1,1,0.3,0.4,0.3,1,1.5,1.3,0.75,1,0.5];
		var musicI:int;
		var playing:int;
		var musicC:SoundChannel;
		var muteM:Boolean;
		var muteS:Boolean;
		var fadeT:Timer=new Timer(10,0);
		var fadeIn:Boolean;
		var volume:Number=0;
				
		public function init(){
			fadeT.addEventListener(TimerEvent.TIMER,fadeTick);
			play(0);
		}
		
		public function sound(i:int){
			if (muteS==false){
				if (soundA[i]!=null){
					(new soundA[i]).play(0,0,new SoundTransform(volumeA[i]));
				}
			}
		}
		
		public function play(i:int=-1){
			//return;
			
			if (i==-1){
				i=musicI;
			}
			musicI=i;
			if (fadeT.running){
				if (musicI==playing){
					fadeIn=true;
				}else{
					fadeIn=false;
				}
				return;
			}
			fadeIn=false;
			if (musicC!=null){
				fadeT.reset();
				fadeT.start();
			}else{
				musicSwap();
			}
		}
		
		public function stop(){
			if (musicC!=null){
				if (fadeT.running){
					fadeIn=false;
					return;
				}
				fadeIn=false;
				fadeT.reset();
				fadeT.start();
			}
		}
		
		public function fadeTick(e:TimerEvent){
			if (fadeIn){
				volume+=0.01;
				if (volume>=MAX_VOLUME){
					volume=MAX_VOLUME;
					musicC.soundTransform=new SoundTransform(MAX_VOLUME);
					fadeT.stop();
					return;
				}
			}else{
				volume-=0.01;
				if (volume<=0){
					musicSwap();
					return;
				}
			}
			musicC.soundTransform=new SoundTransform(volume);
		}
		
		public function musicSwap(){
			if (musicC!=null){
				musicC.stop();
			}
			volume=0;
			fadeIn=true;
			if (muteM){
				fadeT.stop();
			}else{
				fadeT.reset();
				fadeT.start();
				playing=musicI;
				musicC=(new music[musicI]).play(0,99,new SoundTransform(0.01));
			}
		}
		
		public function set mute(b:Boolean){
			if (b){
				stop();
				Facade.muteB.gotoAndStop(4);
				muteS=muteM=true;
			}else{
				if (muteM){
					play();
				}
				Facade.muteB.gotoAndStop(1);
				muteS=muteM=false;
			}
		}
		
		public function get mute():Boolean{
			if (muteS&&muteM){
				return true;
			}else{
				return false;
			}
		}
	}
}
