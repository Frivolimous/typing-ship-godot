//shipstring: "abc"
// a = ship size   ([s]mall,[m]edium,[l]arge)
// b = weapon type ([m]issile,[l]aser,[s]uicide (small=landmine;medium=suicide ship))
// c = defense type([s]hield,[t]urret,[b]oth,[n]one)

package{
	import flash.utils.ByteArray;
	import flash.media.Sound;

	public class LevelData{
		static var i:Number;
		static var level:int;
		static var array:Array;
		static var mult:Number;
		
		public static function loadData(_level:int):Array{
			mult=1-Facade.gameM.wpm/300;
			if (mult<0.5) mult=0.5;
				
			array=new Array();
			i=0;
			level=_level;
			switch (level){
				case 0:
					missile("sn",0.05, 5,0, 5,6, 4,12);
					mirror();
					
					missile("sn",0.15, 0,3, 7,3, 12,3);
					missile("sn",0.01, 12,6, 5,6, 0,6);
					repeat(0.01,3,2);
					
					laser("sn",0.1, 6,0);
					
					missile("sn",0.1, 3,0, 7,5, 4,5, 7,0);
					repeat(0.01,5);
					laser("sn",0,12,5);
					
					missile("sn",0.13, 2,0, 2,4, 12,9);
					repeat(0.01,7,1,true);
					laser("sn",0,0,6);
					insert(-4);
					laser("sn",0,12,6);
					insert(-4);
					
					laser("sn",0.15,0,3);
					laser("sn",0.005,12,3);
					laser("sn",0.005,0,6);
					laser("sn",0.005,12,6);
					
					missile("sn",0.13, 3,0, 4,4, 7,0);
					missile("sn",0.01, 12,5, 7,6, 0,5);
					repeat(0.02,4,2);
					laser("sn",0.04,0,2);
					laser("sn",0.01,12,2);
					laser("sn",0.02,0,4);
					laser("sn",0.01,12,4);
					missile("sn",0.07, 9,0, 8,4, 5,0);
					missile("sn",0.01, 0,5, 5,6, 12,5);
					repeat(0.02,4,2);
					break;
					
				case 1:
					missile("sn",0.1, 4,0, 4,7, 0,12);
					missile("sn",0,2,0, 2,6, 0,9);
					repeat(0,1,2,true);
					
					missile("sn",0.12, 0,3, 7,3, 12,3);
					offset(0,0,1);
					repeat(0.015,1,2);
					missile("sn",0.02, 12,5, 5,5, 0,5);
					offset(0,0,1);
					repeat(0.015,1,2);
					laser("ss",0,6,0);
					
					suicide("mn",0.14, 3,0, 3,3);
					repeat(0.008,3,1,true);
					
					suicide("mn",0.1, 0,2, 8,2);
					mirror(0.005);
					suicide("mn",0.03, 3,0, 3,3);
					mirror(0.001);
					repeat(0.01,5,4);
					
					missile("sn",0.15, 0,3, 7,4, 12,7);
					repeat(0.02,9,1,true);
					laser("ss",0,4,0);
					insert(-6);
					laser("ss",0,8,0);
					insert(-2);
					suicide("mn",0.05, 1,0, 4,4);
					repeat(0.01,3,1,true);
					
					laser("ss",0.12,0,3);
					laser("ss",0.005,12,3);
					laser("ss",0.01,0,6);
					laser("ss",0.005,12,6);
					
					missile("sn",0.18, 2,0, 2,6, 0,7);
					missile("sn",0, 3,0, 3,6, 12,9);
					repeat(0.03,4,2,true);
					suicide("mn",0.04, 0,3, 4,5);
					repeat(0.005,3,1,true);
					
					laser("sn",0.13,0,3);
					laser("sn",0.005,12,3);
					laser("sn",0.01,0,6);
					laser("sn",0.005,12,6);
					suicide("mn",0.025, 2,12, 3,4);
					repeat(0.005,7,1,true);
					
					suicide("mn",0.2, 0,7, 3,6);
					mirror();
					suicide("mn",0.01,0,2, 6,3);
					mirror();
					suicide("mn",0.01,0,4, 4,3);
					mirror();
					suicide("mn",0.01,5,0, 5,1);
					mirror();
					
					missile("sn",0.12, 3,0, 6,7, 12,8);
					repeat(0.02,11,1,true);
					laser("ss",0,0,6);
					insert(-7);
					laser("ss",0,12,6);
					insert(-3);
					laser("mn",0,6,0);
					suicide("mn",0.03,0,2, 6,3);
					repeat(0.01,3,1,true);
					
					break;
					
				case 2:
					missile("sn",0.1, 6,0, 6,5, 0,9);
					repeat(0.01,5,1,true);
					
					missile("sn",0.1, 0,1, 5,4, 0,8);
					mirror();
					repeat(0.03, 3,2,true);
					laser("sn", 0,0,6);
					insert(-6);
					laser("sn",0,12,6);
					insert(-4);
					laser("mn",0,6,0);
					
					missile("sn",0.17, 3,0, 3,4, 12,11);
					missile("sn",0, 4,0, 5,4, 8,0);
					repeat(0.025,3,2);
					laser("sn",0.01, 2,0);
					mirror();
					missile("sn",0.09, 12,3, 8,3, 1,12);
					missile("sn",0, 12,4, 8,5, 12,8);
					repeat(0.025,3,2);
					laser("sn",0.01,0,6);
					mirror();
					suicide("mn",0.09, 0,3, 3,3);
					repeat(0.01,3);
					
					laser("sn",0.13, 0,3);
					mirror();
					laser("mn",0, 6,0);
					laser("sn",0.01, 0,6);
					mirror();
					
					missile("sn",0.18, 2,0, 2,7, 2,12);
					offset(0,1,0);
					repeat(0.015,1,2);
					repeat(0.05,1,4,true);
					laser("mn",0,0,4);
					missile("sn",0.05, 0,2, 7,2, 12,2);
					offset(0,0,1);
					repeat(0.015,1,2);
					laser("mn",0,12,4);
					missile("sn",0.05, 12,5, 5,5, 0,5);
					offset(0,0,1);
					repeat(0.015,1,2);
					
					missile("sn",0.2, 0,12, 4,4, 12,9);
					repeat(0.006,3,1,true);
					laser("mn",0.01,4,0);
					mirror(0.01);
					suicide("mn",0.05, 0,3, 3,3);
					repeat(0.015,7,1,true);
					
					laser("mn",0.15, 0,7);
					mirror();
					laser("sn",0.015, 0,4);
					mirror();
					laser("mn",0.02, 3,0);
					mirror();
					laser("sn",0.015, 5,0);
					mirror();
					
					missile("sn",0.2, 6,0, 6,4, 0,0);
					mirror(0.027);
					missile("sn",0.027, 6,0, 6,6, 0,9);
					mirror(0.028);
					repeat(0.028,4,4);
					laser("sn",0,0,3);
					insert(-18);
					laser("sn",0,12,3);
					insert(-15);
					laser("sn",0,0,6);
					insert(-12);
					laser("sn",0,12,6);
					insert(-9);
					laser("mn",0,3,0);
					insert(-6);
					laser("mn",0,9,0);
					insert(-3);
					suicide("ms",0.08, 0,12, 3,4);
					repeat(0.015,7,1,true);
					
					break;
				case 3:
					missile("sn",0.1, 0,3, 3,5, 8,0);
					repeat(0.02,5,1,true);
					laser("ss",0.02,5,0);
					mirror(0.02);
					suicide("mn",0.02, 0,2, 6,3);
					mirror(0.02);
					suicide("mn",0.02, 0,4, 6,5);
					mirror(0.02);
					repeat(0.02,1,4);
					laser("ms",0.02,0,2);
					mirror(0.02);
					
					boss(0.2,0);
					break;
				case 4:
					missile("sn",0.05, 5,0, 5,6, 4,12);
					mirror();
					
					missile("sn",0.15, 0,3, 7,3, 12,3);
					missile("sn",0.01, 12,6, 5,6, 0,6);
					repeat(0.01,3,2);
					laser("ss",0.03,3,0);
					mirror();
					laser("ss",0.02,0,9);
					mirror();
					
					missile("sn",0.25,6,0,6,5,0,10);
					repeat(0.015,7,1,true);
					laser("ss",0.015,0,3);
					mirror();
					missile("sn",0.015,1,0,5,4,12,5);
					repeat(0.015,5,1,true);
					laser("ss",0.02,3,0);
					mirror();
					
					missile("sn",0.2,5,0,5,6,1,12);
					mirror();
					repeat(0.03,3,2);
					laser("ms",0.03,0,9);
					mirror();
					
					laser("ms",0.25,6,0);
					laser("ss",0.01,0,3);
					mirror(0.01);
					offset(0.02,0,3);
					mirror(0.02);
					
					suicide("sn",0.25,1.5,0,1.5,6);
					offset(0,3);
					offset(0,3);
					offset(0,3);
					suicide("sn",0.028,3,0,3,6);
					offset(0,3);
					offset(0,3);
					repeat(0.028,3,7);
					
					missile("sn",0.25,0,4,3,6,6,0);
					repeat(0.03,11,1,true);
					laser("ss",0,2,0);
					insert(-9);
					laser("ss",0,4,0);
					insert(-6);
					laser("ss",0,10,0);
					insert(-3);
					laser("ss",0,8,0);
					
					missile("sn",0.25,12,2,7,3,0,2);
					missile("sn",0.015,1,12,5,6,12,8);
					repeat(0.03,5,2);
					laser("ms",0,0,6);
					insert(-8);
					laser("ms",0,12,6);
					insert(-6);
					laser("ms",0,6,0);
					insert(-4);
					break;
					
				case 5:
					missile("ss",0.05,5,0,5,4,2,0);
					mirror();
					repeat(0.03,1,2);
					missile("sn",0.03,5,0,5,4,2,0);
					mirror();
					repeat(0.03,1,2);
					
					missile("ss",0.1,6,0,6,5,0,10);
					repeat(0.06,5,1,true);
					missile("sn",0,1,0,2,4,3,0);
					missile("sn",0,2,0,3,4,4,0);
					repeat(0,1,2);
					insert(-10);
					insert(-9);
					insert(-7);
					insert(-6);
					missile("sn",0,12,1,8,2,12,3);
					missile("sn",0,12,2,8,3,12,4);
					repeat(0,1,2);
					insert(-6);
					insert(-5);
					insert(-3);
					insert(-2);
					
					missile("ss",0.15,9,0,9,6,9,12);
					offset(0,1);
					missile("mn",0.02,2,0,2,6,2,12);
					offset(0,2);
					missile("sn",0.04,0,2,5,4,12,7);
					repeat(0.01,3,1,true);
					
					missile("sn",0.15,1,0,1,6,1,12);
					offset(0,1);
					repeat(0.04,1,2);
					missile("mn",0.02,10,0,10,6,10,12);
					repeat(0.03,3,3);
					laser("ms",0,5,0);
					mirror();
					insert(-4);
					
					suicide("sn",0.32,1.5,0,1.5,6);
					offset(0,3);
					offset(0,3);
					offset(0,3);
					repeat(0.035,7,4,true);
					
					missile("ss",0.3,6,0,6,5,0,9);
					missile("mn",0.01,0,8,4,5,5,0);
					mirror();
					repeat(0.07,3,3,true);
					laser("ms",0,2,0);
					insert(-9);
					laser("ms",0,10,0);
					insert(-6);
					
					missile("mn",0.2,2,0,2,5,2,12);
					missile("ss",0,3,0,3.5,5,7,0);
					repeat(0.05,3,2);
					missile("mn",0.03,12,2,5,5,2,12);
					missile("ss",0,12,3.5,7,5,12,7);
					repeat(0.05,3,2);
					suicide("ms",0.06,0,12,3,4);
					repeat(0.015,3,1,true);
					break;
					
				case 6:
					missile("mn",0.05,2,0,2,7,2,12);
					mirror();
					missile("mn",0,4,0,4,6,2,12);
					mirror();
					
					missile("ms",0.15,1,0,5,4,11,12);
					mirror();
					missile("mn",0.01,0,10,3,6,0,3);
					repeat(0.01,3,1,true);
					
					missile("ms",0.2,0,4,7,4,12,4);
					offset(0.02,0,2);
					repeat(0.03,3,2);
					laser("ms",0.04,0,2);
					laser("ms",0.04,0,8);
					
					missile("ms",0.25,2,0,2,7,2,12);
					missile("ms",0.01,3,0,5,5,12,7);
					repeat(0.06,1,2);
					missile("mn",0.05,2,0,2,7,2,12);
					missile("mn",0.01,3,0,5,5,12,7);
					repeat(0.06,3,2);
					laser("ss",0,12,3);
					insert(-7);
					laser("ss",0,8,0);
					insert(-4);
					laser("ss",0,6,0);
					insert(-1);
					
					missile("ms",0.2,5,0,5,6,0,10);
					mirror();
					missile("mn",0.02,3,0,3,4,0,7);
					repeat(0.035,11,1,true);
					laser("ms",0,0,3);
					insert(-8);
					laser("ms",0,12,3);
					insert(-6);
					
					missile("ms",0.2,3,0,3,7,2,12);
					mirror();
					missile("ms",0.04,4,0,4,6,2,12);
					mirror(); 
					missile("ms",0.04,5,0,5,5,2,12);
					mirror();
					missile("mn",0.07,3,0,3,7,2,12);
					mirror();
					missile("mn",0.04,4,0,4,6,2,12);
					mirror();
					missile("mn",0.04,5,0,5,5,2,12);
					mirror();
					
					laser("mn",0.2,0,3);
					mirror();
					laser("sn",0.01,0,6);
					mirror();
					laser("ln",0,6,0);
					
					suicide("ss",0.35, 1.4,0, 1.4,6);
					offset(0,3.8);
					offset(0,3.8);
					repeat(0.05,4,3,true);
					break;
					
				case 7:
					suicide("sn",0.1,1.5,0,1.5,6);
					offset(0,3);
					offset(0,3);
					offset(0,3);
					laser("ms",0.04,3,0);
					mirror();
					
					suicide("sn",0.09,1.5,0,1.5,6);
					offset(0,3);
					offset(0,3);
					offset(0,3);
					missile("mn",0.02,0,2,6,2,12,2);
					missile("mn",0.02,12,4,6,4,0,4);
					
					suicide("sn",0.09,1.5,0,1.5,6);
					offset(0,3);
					offset(0,3);
					offset(0,3);
					missile("ss",0.02,3,0,3,6,2,12);
					mirror(0.02);
					
					suicide("ss",0.09,1.5,0,1.5,6);
					offset(0,3);
					offset(0,3);
					offset(0,3);
					missile("ms",0.02,0,2,3,6,0,9);
					mirror(0.02);
					
					boss(0.2,1);
					break;
					
				case 8:
					missile("sn",0.07, 0,2, 4,2, 0,7);
					repeat(0.008,5,1,true);
					
					missile("sn",0.05, 0,3, 5,4, 12,8);
					repeat(0.015,9,1,true);
					laser("ln",0,3,0);
					insert(-7);
					laser("ln",0,9,0);
					insert(-5);
					laser("ln",0,6,0);
					missile("sn",0.02, 1,12, 4,6, 12,2);
					repeat(0.02,5,1,true);
					
					missile("sn",0.17, 12,12, 8,3, 2,6, 12,7);
					repeat(0.01,12,1);
					laser("ln",0,3,0);
					insert(-10);
					laser("ln",0,9,0);
					insert(-5);
					suicide("mn",0.05,0,0, 3,4);
					repeat(0.01,3);
					
					laser("ms",0.23, 4,0);
					mirror(0.01);
					laser("ls",0.01, 6,0);
					laser("ss",0.04, 0,4);
					mirror(0.022);
					offset(0.022,0,3);
					mirror(0.022);
					
					missile("ss",0.22, 0,12, 4,3, 10,6, 0,6);
					repeat(0.03,7,1);
					laser("ls",0,5,0);
					insert(-5);
					laser("ls",0,7,0);
					insert(-1);
					suicide("mn",0.13,12,0, 9,4);
					repeat(0.01,3);
					
					laser("ls",0.3,2,0);
					offset(0.01,2);
					offset(0.01,2);
					offset(0.01,2);
					offset(0.01,2);
					suicide("mn",0.13,0,12, 2,5);
					repeat(0.01,7,1,true);
					break;
				case 9:
					missile("ln",0.05, -1,-1, 5,6, 13,13);
					mirror(0.01);
					
					missile("mn",0.1, 0,1, 9,3, 0,6);
					missile("mn",0.013, 12,8, 3,6, 12,3);
					repeat(0.013,3,2);
					missile("ln",0.03, -1,3, 6,5, 13,3);
					missile("ln",0, 13,5, 6,3, -1,4);
					laser("lb",0.06, 4.5,0);
					laser("lb",0.01, 7.5,0);
					
					missile("ln",0.3, 3,-1, 3,5, 3,13);
					mirror();
					missile("ln",0.04, -1,1, 6,3, 9,13);
					mirror();
					laser("lt",0.04, 0,5);
					mirror();
					missile("mn",0.14, 0,1, 5,5, 12,1);
					missile("mn",0, 0,9, 6,6, 0,3);
					missile("mn",0, 12,9, 7,5, 0,9);
					missile("mn",0, 12,1, 6,4, 12,10);
					
					missile("ss",0.14, 2,12, 2,3, 9,2.5, 10,12);
					repeat(0.01,7);
					missile("ls",0.07, -1,6, 8,6, 13,6);
					offset(0,0,2);
					laser("lb",0.05, 0,7);
					
					ship("lmt", 0.1, 3,0,[move(3,3),halt(15),halt(10,true),move(0,3)]);
					mirror();
					ship("lmt", 0.05, 5,0,[move(5,3),halt(15),halt(10,true),move(0,3)]);
					mirror();
					missile("mn",0.06, 0,12, 4,7, 4,0);
					mirror();
					missile("mn",0.03, 6,0, 6,5, 1,12);
					mirror();
					
					missile("ms",0.23, 2,0, 2,6, 6,4, 10,12);
					mirror(0.018);
					missile("mn",0.018, 2,0, 2,6, 6,4, 10,12);
					repeat(0.018,5,1,true);
					missile("ln",0.04, -1,-1, 2,3, -1,13);
					mirror();
					laser("lb",0.05,6,0);
					
					laser("lb",0.35, 4,0);
					mirror();
					stealth(0.05, 3,5, 0,12);
					mirror(0.01);
					stealth(0.01, 6,4, 6,0);
					break;
				case 10:
					missile("lb",0.03, 2,-1, 2,13);
					mirror();
					missile("lb",0.05,-1,2,13,2);
					mirror();
					laser("lb",0.05,0,5);
					mirror();
					missile("lb",0.05, 3,-1, 3,6, 1,13);
					mirror();
					missile("ss",0.03, 6,0, 6,5, 1,0);
					repeat(0.03,3,1,true);
					
					missile("sn",0.1, 2,12, 2,3, 8,4, 10,12);
					repeat(0.01,13,1,true);
					laser("ss",0.02,5,0);
					mirror();
					missile("sn",0.05, 2,0, 2,7, 2,12);
					repeat(0.01,5,1,true);
					stealth(0.05, 6,7,6,0);
					
					missile("sn",0.1, 6,0, 6,5, 3,2, 12,7);
					repeat(0.014,9);
					missile("lb",0.03, 2,-1, 2,7, 2,13);
					mirror(0.02);
					laser("ss",0.04, 4,0);
					mirror(0.02);
					stealth(0.07, 5,3, 0,6);
					stealth(0.02, 8,5, 12,7);
					
					missile("lb",0.2, 2,-1, 5,4, 2,13);
					mirror();
					laser("sn",0.02, 0,7);
					mirror(0.015);
					offset(0.015,0,-2);
					mirror(0.015);
					offset(0.015,0,-2);
					mirror(0.015);
					laser("sn",0.015, 3,0);
					mirror(0.015);
					laser("sn",0.015, 7,0);
					mirror(0.015);
					
					missile("mn",0.2, 0,2, 6,4, 12,2);
					repeat(0.02,3);
					missile("sn",0.01, 12,3, 6,5, 0,3);
					clone(0.01,-2);
					repeat(0.01,4,2);
					clone(0.01,-2);
					repeat(0.02,3)
					laser("lb",0.01,0,6);
					mirror(0.01);
					stealth(0.05,5,5, 5,0);
					mirror(0.03);
					
					suicide("sn",0.3, 1.4,0, 1.4,6);
					offset(0,3.8);
					offset(0,3.8);
					repeat(0.03,2,3,true);
					missile("sn",0, 0,6, 6,7, 12,10);
					repeat(0.01,7);
					missile("mn",0, 2,0, 2,5, 1,12);
					mirror();
					missile("mn",0.06, 4,0, 4,5, 1,12);
					mirror();
					missile("ln",0, 13,5, 6,7, -1,10);
					
					laser("ms",0.18, 4,0);
					mirror(0.01);
					laser("ls",0.01, 6,0);
					laser("ss",0.06, 0,4);
					mirror(0.02);
					offset(0.02,0,3);
					mirror(0.02);
					stealth(0.05, 4,5, 0,12);
					mirror(0.02);
					
					suicide("mn",0.2, 3,0, 5,2);
					mirror();
					repeat(0.06,1,2);
					suicide("mn",0, 0,0, 2,2);
					mirror();
					repeat(0.06,1,4);
					suicide("mn",0, 0,7, 3,6);
					mirror();
					repeat(0.06,1,6);
					suicide("ms",0, 5,0, 5,3);
					mirror();
					break;
				case 11:
					missile("ln",0.1, 1,-1, 4,6, -1,13);
					mirror();
					laser("ln",0.06, 0,5);
					mirror();
					
					missile("ls",0.09, -1,2, 6,3.5, 13,5);
					missile("ls",0, 13,8, 6,6.5, -1,5);
					laser("ls",0.06, 3,0);
					mirror();
					
					missile("lt",0.09, 5,-1, 5,4, -1,8);
					mirror();
					laser("lt",0.09, 0,3);
					mirror();
					
					missile("lb",0.09, -1,1, 4,2, -1,12);
					mirror();
					laser("lb",0.09, 5,0);
					mirror();
					
					stealth(0.2, 3,4, 0,5);
					mirror(0.024);
					stealth(0.024, 6,3, 6,0, true);
					
					boss(0.02,2);
					break;
				
				default: throw(new Error("Level number OOB: "+level));
			}
			return array;
		}
		
		static function boss0Suicides():Array{
			array=new Array();
			i=0;
			//600,550
			ship("msn",0,1.6,3,[halt(16),fire()]);
			ship("msn",0,2.2,3.4,[halt(13),fire()]);
			ship("msn",0,2.8,3.7,[halt(10),fire()]);
			ship("msn",0,6.8,3.7,[halt(10),fire()]);
			ship("msn",0,7.4,3.4,[halt(13),fire()]);
			ship("msn",0,8,3,[halt(16),fire()]);
			return array;
		}
		
		static function bossLoop():Array{
			array=new Array();
			i=Facade.gameM.distance;
			
			switch(Facade.gameM.boss.index){
				case 0:
					missile("sn",0.3, 0,4, 7,6, 12,6);
					missile("sn",0.015, 12,3, 5,5, 0,5);
					repeat(0.015,2,2);
					
					laser("ss",0.2, 0,9);
					mirror(0.01);
					break;
				case 1:
					missile("mn",0.25, 0,3+Math.random()*3, 5,6, 12,7);
					mirror();
					repeat(0.03,1,2);
					
					laser("ms",0.35,0,8);
					mirror(0.01);
					
					break;
				case 2:
					laser("ms",0.3,0,6+Math.random()*3);
					mirror();
					missile("mn",0.3, 0,0, 2,4, 5,6, 12,10);
					repeat(0.02,5,1,true);
					stealth(0.3, 2+Math.random()*8,5+Math.random()*2, (Math.random()>0.5?0:12),5);
					break;
				default: throw(new Error("Level out of bounds: "+level));
			}
			return array;
		}
		
		static function setDist(_distance:Number){
			if (_distance>=0.1){
				_distance*=mult;
			}
			i+=_distance;
		}

		
		static function loopBack(j:int=0){
			if (j<0){
				j=array.length+j;
			}
			array.push(new LevelEvent(i,LevelEvent.LOOP,j));
		}
				
		static function wait(_distance:Number=0):LevelEvent{
			setDist(_distance);
			var m:LevelEvent=new LevelEvent(i,LevelEvent.WAIT,_distance);
			
			array.push(m);
			return m;
		}
		
		static function jump(j:Number=0){
			array.push(new LevelEvent(i,LevelEvent.JUMP,j));
		}
		
		static function laser(_type:String,_distance:Number,_x:Number,_y:Number,_speed:int=0){
			var _commands:Array;
			
			if (_x==0){
				_commands=[move(2,_y),halt(10-_speed),halt(20-_speed,true),move(-2,_y)];
			}else if (_x==12){
				_commands=[move(10,_y),halt(10-_speed),halt(20-_speed,true),move(14,_y)];
			}else if (_y==0){
				_commands=[move(_x,2),halt(10-_speed),halt(20-_speed,true),move(_x,-2)];
			}else if (_y==12){
				_commands=[move(_x,10),halt(10-_speed),halt(20-_speed,true),move(_x,14)];
			}else{
				throw(new Error("Invalid spawn location: "+_x+" "+_y));
			}
			ship(_type.charAt(0)+"l"+_type.charAt(1),_distance,_x,_y,_commands);
		}
		
		static function suicide(_type:String,_distance:Number,_x:Number,_y:Number,_xTo:Number,_yTo:Number){
			ship(_type.charAt(0)+"s"+_type.charAt(1),_distance,_x,_y,[move(_xTo,_yTo),fire()]);
		}
		
		static function stealth(_distance:Number,_x:Number,_y:Number,_xTo:Number,_yTo:Number,_shield:Boolean=false){
			ship(_shield?"xms":"xmn",_distance,_x,_y,[halt(8),halt(10,true),halt(10,true),halt(10,true),halt(10,true),halt(5,true),move(_xTo,_yTo)]);
		}
		
		static function missile(_type:String,_distance:Number,_x:Number,_y:Number,_xTo1:Number,_yTo1:Number,_xTo2:Number=-1,_yTo2:Number=-1,_xTo3:Number=-1,_yTo3:Number=-1){
			ship(_type.charAt(0)+"m"+_type.charAt(1),_distance,_x,_y,[move(_xTo1,_yTo1),move(_xTo2,_yTo2,((_xTo3==-1)?true:false)),((_xTo3!=-1)?move(_xTo3,_yTo3,true):halt(1))]);
		}
		
		static function insert(j:int){
			if (j<0){
				j=array.length+j;
			}
			array[array.length-1].distance=array[j].distance;
			array.splice(j,0,array.pop());
		}
		
		static function ship(_type:String,_distance:Number,_x:Number,_y:Number,_commands:Array=null){
			setDist(_distance);
			_x*=GameUI.VIEW_WIDTH/12;
			_y*=GameUI.VIEW_HEIGHT/12;
			array.push(new SpawnEvent(i,_type,_x,_y,_commands));
		}
		
		static function boss(_distance:Number,_type:int){
			setDist(_distance);
			array.push(new BossEvent(i,_type));
		}
		
		static function clone(_distance:Number=0,j:int=-1){
			if (j<0){
				j=array.length+j;
			}
			setDist(_distance);
			var tCommands:Array=new Array();
			for (var k:int=0;k<array[j].commands.length;k+=1){
				tCommands.push(array[j].commands[k].clone());
			}
			array.push(new SpawnEvent(i,array[j].type,array[j].x,array[j].y,tCommands));
		}

		static function mirror(_distance:Number=0,j:int=-1){
			if (j<0){
				j=array.length+j;
			}
			setDist(_distance);
			var tCommands:Array=new Array();
			for (var k:int=0;k<array[j].commands.length;k+=1){
				tCommands.push(array[j].commands[k].mirror());
			}
			array.push(new SpawnEvent(i,array[j].type,GameUI.VIEW_WIDTH-array[j].x,array[j].y,tCommands));
		}
		
		static function offset(_distance:Number=0,xOff:Number=0,yOff:Number=0,j:int=-1){
			if (j<0){
				j=array.length+j;
			}
			setDist(_distance);
			xOff*=GameUI.VIEW_WIDTH/12;
			yOff*=GameUI.VIEW_HEIGHT/12;
			var tCommands:Array=new Array();
			for (var k:int=0;k<array[j].commands.length;k+=1){
				tCommands.push(array[j].commands[k].offset(xOff,yOff));
			}
			array.push(new SpawnEvent(i,array[j].type,array[j].x+xOff,array[j].y+yOff,tCommands));
		}
		
		static function repeat(_distance:Number,repeatN:int=1,repeatC:int=1,reverse:Boolean=false){
			for (var k:int=0;k<repeatN*repeatC;k++){
				if (k%repeatC==0){						
					if (reverse){
						mirror(_distance,0-repeatC);
					}else{
						clone(_distance,0-repeatC);
					}
				}else{
					if (reverse){
						mirror((array[array.length-repeatC].distance-array[array.length-repeatC-1].distance),0-repeatC);
					}else{
						clone((array[array.length-repeatC].distance-array[array.length-repeatC-1].distance),0-repeatC);
					}
				}
			}
		}
		
		
					
		static function move(_x:Number,_y:Number,_fire:Boolean=false):ObjectCommand{
			_x*=GameUI.VIEW_WIDTH/12;
			_y*=GameUI.VIEW_HEIGHT/12;
			return new ObjectCommand(_x,_y,0,true,_fire);
		}
		
		static function halt(_timer:int,_fire:Boolean=false):ObjectCommand{
			return new ObjectCommand(GameUI.VIEW_WIDTH/2,GameUI.VIEW_HEIGHT-100,_timer,false,_fire);
		}
		
		static function fire():ObjectCommand{
			return new ObjectCommand(0,0,1,false,true);
		}
	}
}