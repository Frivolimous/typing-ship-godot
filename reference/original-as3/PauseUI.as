package  {
	import flash.display.Sprite;
	import flash.text.TextField;
	import flash.text.TextFormat;
	import flash.filters.GlowFilter;
	
	public class PauseUI extends Sprite{
		var confirmM:Sprite;

		public function PauseUI() {
			graphics.beginFill(0,0.3);
			graphics.drawRect(0,0,Facade.stage.stageWidth,Facade.stage.stageHeight);
			var _textField:TextField=new TextField();
			_textField.defaultTextFormat=new TextFormat(Facade.TITLE_FONT,50,0xffffff,true);
			_textField.filters=[new GlowFilter(Facade.GOLD),new GlowFilter(Facade.GOLD,1,6,6,2,1,true)];
			_textField.width=300;
			_textField.selectable=false;
			_textField.text=StringData.PAUSED;
			_textField.x=125;
			_textField.y=200;
			addChild(_textField);
			
			var _button:UIButton=new UIButton(StringData.MENU,goMenu,Facade.GOLD,Facade.GOLD_OVER,true);
			_button.x=230;
			_button.y=270;
			addChild(_button);
			
			confirmM=new Sprite;
			confirmM.y=290;
			_button=new UIButton(StringData.YES,menuYes,Facade.GOLD,Facade.GOLD_OVER,true);
			_button.x=200;
			confirmM.addChild(_button);
			_button=new UIButton(StringData.NO,menuNo,Facade.GOLD,Facade.GOLD_OVER,true);
			_button.x=275;
			confirmM.addChild(_button);
		}
		
		public function goMenu(){
			if (Facade.stage.contains(confirmM)){
				menuYes();
			}else{
				addChild(confirmM);
			}
		}
		
		function menuYes(){
			removeChild(confirmM);
			Facade.gameC.pauseGame(false);
			Facade.gameC.finalDeath();
		}
		
		function menuNo(){
			removeChild(confirmM);
			Facade.gameC.pauseGame(false);
			Facade.stage.focus=Facade.gameUI;
		}
	}
}
