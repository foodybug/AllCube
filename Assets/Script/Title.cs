using UnityEngine;

public class Title : MonoBehaviour
{
	private bool m_bInitialized = false;

	[Header("Title UI References")]
	public UnityEngine.UI.RawImage texLogo;
	public UnityEngine.UI.Text textTouchScreen;
	public GameObject goBtnSound;

	void Start()
	{
		Debug.Log("[Title Debug] Start active. Title script is successfully attached and running.");
		// Start 시점에 GameMain.Instance가 null일 수 있으므로 Lazy Initialization 진행
	}

	private void Initialize()
	{
		// 씬 내에 로드되어 있으나 비활성화된 GameMain 복구 시도
		if (GameMain.Instance == null)
		{
			GameMain[] inactiveMain = Resources.FindObjectsOfTypeAll<GameMain>();
			if (inactiveMain != null && inactiveMain.Length > 0)
			{
				if (inactiveMain[0].gameObject.scene.isLoaded)
				{
					Debug.Log("[Title Debug] Found inactive GameMain in scene. Forcing Active!");
					inactiveMain[0].gameObject.SetActive(true);
				}
			}
		}

		if (GameMain.Instance == null) return;

		Debug.Log("[Title Debug] Initialize active. GameMain.Instance is now valid.");
		GameMain.Instance.eCurState = eGameState.eGameState_Logo;
		CameraManager.Instance.Init();
		
		// Title UI 초기 세팅
		if (texLogo != null) texLogo.gameObject.SetActive(true);
		if (textTouchScreen != null) textTouchScreen.gameObject.SetActive(true);

		// sound 버튼 이미지 적용
		if (goBtnSound != null)
		{
			UnityEngine.UI.RawImage soundImg = goBtnSound.GetComponent<UnityEngine.UI.RawImage>();
			if (soundImg != null)
			{
				if (0 == GameMain.Instance.nSoundEnable)
				{
					soundImg.texture = Resources.Load("UI/sound_off") as Texture;
					AudioManager.Instance.StopBgm();
				}
				else
				{
					soundImg.texture = Resources.Load("UI/sound_on") as Texture;
					AudioManager.Instance.PlayBgm("Sound/bgm");
				}
			}
		}

		// 로비 복귀 플래그가 참인 경우 즉시 UI 구성 전환 (Title 자체 UI 제어)
		if (GameMain.StartInLevelSelect)
		{
			GameMain.StartInLevelSelect = false;
			GameMain.Instance.eCurState = eGameState.eGameState_Select;
			
			if (textTouchScreen != null) textTouchScreen.gameObject.SetActive(false);
			if (texLogo != null) texLogo.gameObject.SetActive(false);
			if (goBtnSound != null) goBtnSound.SetActive(true);

			// 레벨 선택창 UI는 GameMain을 통해 생성 지시
			GameMain.Instance.GoLevelSelectScene();
		}

		m_bInitialized = true;
	}

	void Update()
	{
		if (!m_bInitialized)
		{
			// 매 프레임 대기 도중 씬 내부의 비활성화 매니저가 깨어났는지/깨울 수 있는지 상시 추적
			if (GameMain.Instance == null)
			{
				GameMain[] inactiveMain = Resources.FindObjectsOfTypeAll<GameMain>();
				if (inactiveMain != null && inactiveMain.Length > 0)
				{
					if (inactiveMain[0].gameObject.scene.isLoaded)
					{
						inactiveMain[0].gameObject.SetActive(true);
					}
				}
			}

			if (GameMain.Instance != null)
			{
				Initialize();
			}
			return;
		}

		if (GameMain.Instance == null) return;

		// 로고 화면에서 터치 입력 감지 시 곧바로 Play 씬으로 전환 처리
		if (eGameState.eGameState_Logo == GameMain.Instance.eCurState)
		{
			if (MainManager.Instance != null && MainManager.Instance.IsTransitioning)
			{
				// IsTransitioning 상태에 의한 클릭 차단 여부 로그
				Debug.Log("[Title Debug] MainManager is currently transitioning. Click ignored.");
				return;
			}

			if (Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Began))
			{
				Debug.Log("[Title Debug] Screen Click/Touch detected. Calling StartLevel on GameMain.");
				AudioManager.Instance.PlayBgm("Sound/bgm");
				AdmobManager.Instance.Show();

				// 중간 로비 UI 구성 단계 없이 즉시 플레이 씬으로 시작되도록 StartLevel 호출
				GameMain.Instance.StartLevel(GameMain.Instance.nSaveLevel);
			}
		}
	}
}
