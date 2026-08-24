#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class ResultUISetupEditor : EditorWindow
{
    [MenuItem("Tools/AllCube/Complete Result UI Bindings")]
    public static void SetupResultUI()
    {
        // 1. Result 씬 열기 및 확인
        string scenePath = "Assets/Scene/Result.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError("[ResultUISetup] Result.unity scene is invalid or missing at: " + scenePath);
            return;
        }

        Debug.Log("[ResultUISetup] Successfully loaded Result scene.");

        // 2. 유저가 생성한 UI_Result 컴포넌트 탐색
        UI_Result uiResult = FindObjectOfType<UI_Result>();
        if (uiResult == null)
        {
            Debug.Log("[ResultUISetup] Could not find UI_Result in scene. Creating a default 'ResultUI' object.");
            GameObject newGo = new GameObject("ResultUI");
            uiResult = Undo.AddComponent<UI_Result>(newGo);
        }

        GameObject targetGo = uiResult.gameObject;

        // 3. ResultMain 컴포넌트 검사 및 부착
        ResultMain resultMain = targetGo.GetComponent<ResultMain>();
        if (resultMain == null)
        {
            resultMain = Undo.AddComponent<ResultMain>(targetGo);
            Debug.Log("[ResultUISetup] Attached 'ResultMain' component to " + targetGo.name);
        }

        // 4. 실제 UI 컴포넌트들이 씬 내에 존재하지 않는 경우 자동 생성하여 구조물 완비
        CreateResultUIStructure(uiResult);

        // 5. 생성된 UI 컴포넌트들을 UI_Result 인스펙터 참조에 영구 바인딩 (Assign)
        uiResult.btnRetry = FindComponentInScene<UnityEngine.UI.Button>("btnRetry");
        uiResult.btnTitle = FindComponentInScene<UnityEngine.UI.Button>("btnTitle");
        uiResult.texRetryBtnBg = FindComponentInScene<UnityEngine.UI.RawImage>("texRetryBtnBg");
        uiResult.textResultTime = FindComponentInScene<UnityEngine.UI.Text>("textResultTime");
        uiResult.texResultIcon = FindComponentInScene<UnityEngine.UI.RawImage>("texResultIcon");

        if (uiResult.btnRetry != null)
        {
            uiResult.textRetry = uiResult.btnRetry.GetComponentInChildren<UnityEngine.UI.Text>(true);
            uiResult.texRetry = uiResult.btnRetry.GetComponentInChildren<UnityEngine.UI.RawImage>(true);
        }

        if (uiResult.btnTitle != null)
        {
            uiResult.textTitle = uiResult.btnTitle.GetComponentInChildren<UnityEngine.UI.Text>(true);
        }

        // 6. ResultMain에 UI_Result 뷰 바인딩
        var serializedMain = new SerializedObject(resultMain);
        var uiProp = serializedMain.FindProperty("ui");
        if (uiProp != null)
        {
            uiProp.objectReferenceValue = uiResult;
            serializedMain.ApplyModifiedProperties();
        }

        // UI_Result에 대해서도 직렬화 수정사항 적용
        var serializedUI = new SerializedObject(uiResult);
        serializedUI.ApplyModifiedProperties();

        // 7. 변경 사항 표시 및 씬 저장
        EditorUtility.SetDirty(targetGo);
        EditorUtility.SetDirty(uiResult);
        EditorUtility.SetDirty(resultMain);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[ResultUISetup] ===================================================");
        Debug.Log("[ResultUISetup]  Result UI Setup & Auto-Creation Completed Successfully!");
        Debug.Log("[ResultUISetup]  - Host Object: " + targetGo.name);
        Debug.Log("[ResultUISetup]  - UI_Result: Component references permanently assigned");
        Debug.Log("[ResultUISetup]  - UI Details:");
        Debug.Log("[ResultUISetup]    * btnRetry: " + (uiResult.btnRetry != null ? "Bound & Ready" : "NOT FOUND"));
        Debug.Log("[ResultUISetup]    * btnTitle: " + (uiResult.btnTitle != null ? "Bound & Ready" : "NOT FOUND"));
        Debug.Log("[ResultUISetup]    * texRetryBtnBg: " + (uiResult.texRetryBtnBg != null ? "Bound & Ready" : "NOT FOUND"));
        Debug.Log("[ResultUISetup]    * textResultTime: " + (uiResult.textResultTime != null ? "Bound & Ready" : "NOT FOUND"));
        Debug.Log("[ResultUISetup]    * texResultIcon: " + (uiResult.texResultIcon != null ? "Bound & Ready" : "NOT FOUND"));
        Debug.Log("[ResultUISetup] ===================================================");
    }

    private static void CreateResultUIStructure(UI_Result uiResult)
    {
        // 1. Canvas 생성 및 구성
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("Canvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas");
            Debug.Log("[ResultUISetup] Created default 'Canvas' for UI placement.");
        }

        // 2. EventSystem 생성 및 구성 (마우스 클릭 동작 필수)
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(esGo, "Create EventSystem");
            Debug.Log("[ResultUISetup] Created default 'EventSystem' for click input handler.");
        }

        Transform canvasTrans = canvas.transform;

        // 3. texResultIcon (메달 이미지) 자동 생성
        if (FindComponentInScene<UnityEngine.UI.RawImage>("texResultIcon") == null)
        {
            GameObject iconGo = new GameObject("texResultIcon");
            iconGo.transform.SetParent(canvasTrans, false);
            var rawImg = iconGo.AddComponent<UnityEngine.UI.RawImage>();
            
            // 메달 아이콘은 중앙 상단에 배치
            rawImg.rectTransform.anchoredPosition = new Vector2(0, 160);
            rawImg.rectTransform.sizeDelta = new Vector2(120, 120);
            rawImg.texture = Resources.Load("UI/ui_time_bronze") as Texture;
            rawImg.color = Color.white;
            Undo.RegisterCreatedObjectUndo(iconGo, "Create texResultIcon");
            Debug.Log("[ResultUISetup] Created 'texResultIcon' RawImage.");
        }

        // 4. texRetryBtnBg (결과 텍스트 뒷배경 판넬) 자동 생성 및 디자인
        if (FindComponentInScene<UnityEngine.UI.RawImage>("texRetryBtnBg") == null)
        {
            GameObject bgGo = new GameObject("texRetryBtnBg");
            bgGo.transform.SetParent(canvasTrans, false);
            var rawImg = bgGo.AddComponent<UnityEngine.UI.RawImage>();
            
            // Y: -10 위치에 큼지막하게 배치하여 텍스트 판넬 역할 부여
            rawImg.rectTransform.anchoredPosition = new Vector2(0, -10);
            rawImg.rectTransform.sizeDelta = new Vector2(360, 220);
            
            // Play 씬과 일관된 UI 테두리 판넬(msgbox) 리소스 매핑
            rawImg.texture = Resources.Load("UI/msgbox") as Texture;
            rawImg.color = Color.white;
            
            Undo.RegisterCreatedObjectUndo(bgGo, "Create texRetryBtnBg");
            Debug.Log("[ResultUISetup] Created 'texRetryBtnBg' Panel with msgbox border style.");
        }

        // 5. textResultTime (결과/랭킹 출력 텍스트) 자동 생성
        if (FindComponentInScene<UnityEngine.UI.Text>("textResultTime") == null)
        {
            GameObject textGo = new GameObject("textResultTime");
            textGo.transform.SetParent(canvasTrans, false);
            var textComp = textGo.AddComponent<UnityEngine.UI.Text>();
            
            textComp.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Font/PretendardVariable.ttf");
            if (textComp.font == null) textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            textComp.fontSize = 28;
            textComp.alignment = TextAnchor.MiddleCenter;
            textComp.color = Color.white;
            
            // 판넬 윈도우 상단 쪽에 맞춤 배치하여 Time 표기가 위쪽에 시원하게 노출되도록 함
            textComp.rectTransform.anchoredPosition = new Vector2(0, 15);
            textComp.rectTransform.sizeDelta = new Vector2(340, 200);
            
            // 가독성 아웃라인 및 그림자 컴포넌트 추가
            AddOutlineAndShadow(textGo);
            
            Undo.RegisterCreatedObjectUndo(textGo, "Create textResultTime");
            Debug.Log("[ResultUISetup] Created 'textResultTime' Text with Outline styling.");
        }

        // 6. btnRetry (재도전 버튼 및 자식 에셋 구조) 자동 생성
        if (FindComponentInScene<UnityEngine.UI.Button>("btnRetry") == null)
        {
            GameObject btnGo = new GameObject("btnRetry");
            btnGo.transform.SetParent(canvasTrans, false);
            var btnComp = btnGo.AddComponent<UnityEngine.UI.Button>();
            var rawImg = btnGo.AddComponent<UnityEngine.UI.RawImage>();
            
            // Y: -180 위치에 깔끔한 가로 배치
            rawImg.rectTransform.anchoredPosition = new Vector2(0, -180);
            rawImg.rectTransform.sizeDelta = new Vector2(200, 60);
            rawImg.color = new Color(0, 0, 0, 0); // 클릭용 투명 히트박스 지정 (하얀 네모 방지)
            Undo.RegisterCreatedObjectUndo(btnGo, "Create btnRetry");

            // 자식 texRetry 생성
            GameObject texRetryGo = new GameObject("texRetry");
            texRetryGo.transform.SetParent(btnGo.transform, false);
            var retryRaw = texRetryGo.AddComponent<UnityEngine.UI.RawImage>();
            retryRaw.rectTransform.sizeDelta = new Vector2(200, 60);
            retryRaw.texture = Resources.Load("UI/retry_bg") as Texture;
            retryRaw.color = Color.white;

            // 자식 textRetry 생성
            GameObject textRetryGo = new GameObject("textRetry");
            textRetryGo.transform.SetParent(btnGo.transform, false);
            var retryText = textRetryGo.AddComponent<UnityEngine.UI.Text>();
            
            var baseText = FindComponentInScene<UnityEngine.UI.Text>("textResultTime");
            if (baseText != null) retryText.font = baseText.font;
            
            retryText.fontSize = 24;
            retryText.alignment = TextAnchor.MiddleCenter;
            retryText.color = Color.white;
            retryText.rectTransform.sizeDelta = new Vector2(200, 60);
            
            // 글씨 아웃라인 장착
            AddOutlineAndShadow(textRetryGo);
            
            Debug.Log("[ResultUISetup] Created 'btnRetry' Button with Outline text.");
        }

        // 7. btnTitle (Title 씬 이동 버튼) 자동 생성
        if (FindComponentInScene<UnityEngine.UI.Button>("btnTitle") == null)
        {
            GameObject btnGo = new GameObject("btnTitle");
            btnGo.transform.SetParent(canvasTrans, false);
            var btnComp = btnGo.AddComponent<UnityEngine.UI.Button>();
            var rawImg = btnGo.AddComponent<UnityEngine.UI.RawImage>();
            
            // btnRetry 버튼의 하단(anchoredPosition Y: -260)에 적정 여백으로 배치
            rawImg.rectTransform.anchoredPosition = new Vector2(0, -260);
            rawImg.rectTransform.sizeDelta = new Vector2(200, 60);
            
            // 로비(Title) 이동 버튼 디자인 입히기 (msgbox 프레임에 차분한 톤 지정)
            rawImg.texture = Resources.Load("UI/msgbox") as Texture;
            rawImg.color = new Color(0.8f, 0.75f, 0.72f, 0.9f);

            // 자식 textTitle 생성
            GameObject textTitleGo = new GameObject("textTitle");
            textTitleGo.transform.SetParent(btnGo.transform, false);
            var nextText = textTitleGo.AddComponent<UnityEngine.UI.Text>();
            
            var baseText = FindComponentInScene<UnityEngine.UI.Text>("textResultTime");
            if (baseText != null) nextText.font = baseText.font;
            
            nextText.text = "Title";
            nextText.fontSize = 24;
            nextText.alignment = TextAnchor.MiddleCenter;
            nextText.color = Color.white;
            nextText.rectTransform.sizeDelta = new Vector2(200, 60);
            
            // 글씨 아웃라인 장착
            AddOutlineAndShadow(textTitleGo);

            Undo.RegisterCreatedObjectUndo(btnGo, "Create btnTitle");
            Debug.Log("[ResultUISetup] Created 'btnTitle' Button with Outline text.");
        }
    }

    private static void AddOutlineAndShadow(GameObject go)
    {
        if (go.GetComponent<UnityEngine.UI.Outline>() == null)
        {
            var outline = go.AddComponent<UnityEngine.UI.Outline>();
            // 가독성을 극대화시키는 짙은 아웃라인 칼라 지정
            outline.effectColor = new Color(0.12f, 0.08f, 0.05f, 1.0f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }
        if (go.GetComponent<UnityEngine.UI.Shadow>() == null)
        {
            var shadow = go.AddComponent<UnityEngine.UI.Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.5f);
            shadow.effectDistance = new Vector2(2f, -2f);
        }
    }

    private static T FindComponentInScene<T>(string name) where T : Component
    {
        T[] items = Resources.FindObjectsOfTypeAll<T>();
        if (items != null)
        {
            foreach (T item in items)
            {
                if (item != null && item.gameObject != null && item.gameObject.scene.isLoaded && item.name != null)
                {
                    if (item.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return item;
                    }
                }
            }
            foreach (T item in items)
            {
                if (item != null && item.gameObject != null && item.gameObject.scene.isLoaded && item.name != null)
                {
                    if (item.name.ToLower().Contains(name.ToLower()))
                    {
                        return item;
                    }
                }
            }
        }
        return null;
    }
}
#endif