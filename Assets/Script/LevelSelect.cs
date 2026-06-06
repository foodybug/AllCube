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

		if (UI_Play.Instance != null && UI_Play.Instance.ui != null)
		{
			if (UI_Play.Instance.ui.goLevelSelecter != null) UI_Play.Instance.ui.goLevelSelecter.SetActive(false);
			if (UI_Play.Instance.ui.textSelectLevel != null) UI_Play.Instance.ui.textSelectLevel.gameObject.SetActive(false);
		}
		
		MainManager.Instance.nCurLevel = nLevel;
		MainManager.Instance.StartLevel( nLevel);
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
		if( (int)( UI_Play.eLevelClearType.eLevelClearType_Gold) == MainManager.Instance.nClearType[ nLv - 1])
			return Resources.Load( "UI/ui_gold") as Texture;
		else if( (int)( UI_Play.eLevelClearType.eLevelClearType_Silver) == MainManager.Instance.nClearType[ nLv - 1])
			return Resources.Load( "UI/ui_silver") as Texture;
		else if( (int)( UI_Play.eLevelClearType.eLevelClearType_Bronze) == MainManager.Instance.nClearType[ nLv - 1])
			return Resources.Load( "UI/ui_bronze") as Texture;
		else
			return Resources.Load( "UI/ui_bronze") as Texture;
	}
}
