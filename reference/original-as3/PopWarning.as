package{
	import flash.display.Sprite;
	import flash.text.TextField;
	import flash.text.TextFormat;
	
	public class PopWarning extends Sprite{
		var frontText:TextField=new TextField;
		var backText:TextField=new TextField;
		var textSwitch:int=0;
		var textChange:int=0;
		var currentText:int=0;
		var red:Boolean=false;
		var added:Boolean;
		
		public function PopWarning(){
			frontText.defaultTextFormat=new TextFormat(Facade.GAME_FONT,14,0xff0000);
			backText.defaultTextFormat=new TextFormat(Facade.GAME_FONT,14,0);
			backText.x=-1;
			backText.y=-1;			
			frontText.width=backText.width=400;
			addChild(backText);
			addChild(frontText);
		}
		
		public function add (i:int){
			currentText=i;
			switch(i){
				case 1:
				default: frontText.text=backText.text=StringData.LOW_HEALTH; break;
			}
			Facade.gameUI.addChild(this);
			backText.x=(frontText.x=0-frontText.textWidth/2)-1;
			added=true;
			textChange=40;
		}
		
		public function update(){
			if (textChange==0){
				if (currentText==0){
					if (Facade.gameM.pShip.health<2){
						add(1);
					}else{
						remove();
					}
				}else{
					if (Facade.gameM.pShip.health<2){
						add(1);
					}else{
						remove();
					}
				}
			}else{
				textChange-=1;
				textSwitch+=1;
				if (textSwitch>2){
					if (red){
						frontText.setTextFormat(new TextFormat(null,null,0xffffff),0,frontText.length);
						red=false;
					}else{
						frontText.setTextFormat(new TextFormat(null,null,0xff0000),0,frontText.length);
						red=true;
					}
					textSwitch=0;
				}
			}
		}
		
		public function remove(){
			if (added){
				parent.removeChild(this);
				added=false;
			}
		}
	}
}