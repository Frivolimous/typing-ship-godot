package{
	import flash.display.Sprite;
	import flash.text.TextField;
	import flash.text.TextFormat;
	
	public class ScrambleObject extends TextObject{
		public var pArray:Array=new Array();
		var swaps:int;

		public function ScrambleObject(i:int,j:int=1){
			frontText.defaultTextFormat=new TextFormat(Facade.GAME_FONT,25,0xffffff);
			backText.defaultTextFormat=new TextFormat(Facade.GAME_FONT,25,0);
			wordSize=i;
			swaps=j;
			text=Facade.wordList.shift(i);
			backText.x=-1;
			backText.y=-1;
			addChild(backText);
			addChild(frontText);
		}
		
		override public function set text(s:String){
			var cWord:String=s;
			var tWord:String;
			var match:int;
			var k:int;
			loopI:for (var i:int=0;i<Facade.wordList.list[wordSize].length;i+=1){
				tWord=Facade.wordList.list[wordSize][i]
				match=0;
				for (var j:int=0;j<cWord.length;j+=1){
					k=tWord.indexOf(cWord.charAt(j));
					if (k>=0){
						match+=1;
						tWord=tWord.substring(0,k)+tWord.substring(k+1);
					}
					if (match<=j){
						continue loopI;
					}
				}
				pArray.push(Facade.wordList.list[wordSize][i]);
			}
			for (i=0;i<swaps;i+=1){
				cWord=swapLetters(cWord);
			}
			frontText.text=backText.text=cWord;
		}
		
		function swapLetters(tWord:String):String{
			var cWord:String="";
			var i:int=Math.random()*tWord.length;
			var j:int=Math.random()*tWord.length;
			for (var k:int=0;k<tWord.length;k+=1){
				if (k==i){
					cWord+=tWord.charAt(j);
				}else if (k==j){
					cWord+=tWord.charAt(i);
				}else{
					cWord+=tWord.charAt(k);
				}
			}
			for (i=0;i<pArray.length;i+=1){
				if (cWord==pArray[i]){
					return swapLetters(tWord);
				}
			}
			return cWord;
		}
	}
}