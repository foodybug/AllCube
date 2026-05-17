using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LevelSelect : MonoBehaviour
{
	public RawImage texBg;
	public Text textLevel;
	public int nLevel = 1;

	void Start()
	{
	}
	
	void Update()
	{
	}

	public void onSelectBtn()
	{
		AudioManager.Instance.Play( "Sound/ui_button_down");

		if (UIManager.Instance != null)
		{
			UIManager.Instance.goLevelSelecter.SetActive( false);
			UIManager.Instance.textSelectLevel.gameObject.SetActive( false);
		}
		
		GameMain.Instance.nCurLevel = nLevel;
		GameMain.Instance.StartLevel( nLevel);
	}

	public void SetState(int nLv, LevelSelecter.eLevelSelectBtnState eState)
	{
		if( LevelSelecter.eLevelSelectBtnState.eLevelSelectBtnState_Lock == eState)
		{
			texBg.texture = Resources.Load( "UI/ui_lock") as Texture;
			textLevel.text = "";
			nLevel = nLv;
			_BtnEnable( false);
		}
		else if( LevelSelecter.eLevelSelectBtnState.eLevelSelectBtnState_Clear == eState)
		{
			texBg.texture = _GetLevelClearTexture( nLv);
			textLevel.text = nLv.ToString();
			nLevel = nLv;
			_BtnEnable( true);
		}
		else
		{
			texBg.texture = Resources.Load( "UI/ui_cur") as Texture;
			textLevel.text = nLv.ToString();
			nLevel = nLv;
			_BtnEnable( true);
		}
	}

	private void _BtnEnable(bool bEnable)
	{
		Button btn = gameObject.GetComponent<Button>();
		btn.interactable = bEnable;

		if( true == bEnable)
		{
			textLevel.color = Color.white;
		}
		else
		{
			textLevel.color = Color.gray;
		}
	}

	private Texture _GetLevelClearTexture(int nLv)
	{
		if( (int)( UIManager.eLevelClearType.eLevelClearType_Gold) == GameMain.Instance.nClearType[ nLv - 1])
			return Resources.Load( "UI/ui_gold") as Texture;
		else if( (int)( UIManager.eLevelClearType.eLevelClearType_Silver) == GameMain.Instance.nClearType[ nLv - 1])
			return Resources.Load( "UI/ui_silver") as Texture;
		else if( (int)( UIManager.eLevelClearType.eLevelClearType_Bronze) == GameMain.Instance.nClearType[ nLv - 1])
			return Resources.Load( "UI/ui_bronze") as Texture;
		else
			return Resources.Load( "UI/ui_bronze") as Texture;
	}
}
