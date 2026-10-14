package{
	import flash.display.Sprite;
	
	public class GameObject extends Sprite{
		public var 
				textObject:TextObject,
				wordSize:int=-1,
				wordX:int=0,
				_Priority:int;
				
		public function addWord(i:int=0){
			recycleWord();
			if(wordSize>=0){
				textObject=new TextObject((i==0)?wordSize:i,_Priority);
				textObject.rotation=0-rotation;
				textObject.x+=wordX;
				addChild(textObject);
			}
		}

		public function recycleWord(){
			if (textObject!=null){
				Facade.wordList.push(textObject.text);
				removeChild(textObject);
				textObject=null;
			}
		}
		
		public function get priority():int{
			return _Priority;
		}
		
		public function set priority(i:int){
			_Priority=i;
			if (textObject!=null){
				textObject.priority(i);
			}
		}
		
		public function show(b:Boolean){
			if (textObject!=null){
				textObject.show(b);
			}
		}
	}
}