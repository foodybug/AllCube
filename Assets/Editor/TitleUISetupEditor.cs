#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class TitleUISetupEditor : EditorWindow
{
    [MenuItem("Tools/AllCube/Complete Title UI Bindings")]
    public static void SetupTitleUI()
    {
        // 1. Title 씬 열기 및 확인
        string scenePath = "Assets/Scene/Title.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError("[TitleUISetup] Title.unity scene is invalid or missing at: " + scenePath);
            return;
        }

        Debug.Log("[TitleUISetup] Successfully loaded Title scene.");

        // [로고 텍스처 임포트 설정 강제 - Sprite 타입 지정 및 AlphaIsTransparency 활성화]
        string logoPath = "Assets/Resources/UI/logo.png";
        TextureImporter importer = AssetImporter.GetAtPath(logoPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            Debug.Log("[TitleUISetup] Enforced Sprite/AlphaIsTransparency configuration on: " + logoPath);
        }

        // 2. UI_Title 컴포넌트 탐색 및 생성
        UI_Title uiTitle = FindObjectOfType<UI_Title>();
        if (uiTitle == null)
        {
            Debug.Log("[TitleUISetup] Could not find UI_Title in scene. Creating a default 'TitleUI' object.");
            GameObject newGo = new GameObject("TitleUI");
            uiTitle = Undo.AddComponent<UI_Title>(newGo);
        }

        GameObject targetGo = uiTitle.gameObject;

        // 3. Title (컨트롤러) 컴포넌트 검사 및 부착
        Title titleController = targetGo.GetComponent<Title>();
        if (titleController == null)
        {
            titleController = Undo.AddComponent<Title>(targetGo);
            Debug.Log("[TitleUISetup] Attached 'Title' controller component to " + targetGo.name);
        }

        // 4. 실제 UI 컴포넌트들이 씬 내에 존재하지 않는 경우 자동 생성하여 구조물 완비
        CreateTitleUIStructure(uiTitle);

        // 5. 생성된 UI 컴포넌트들을 UI_Title 인스펙터 참조에 영구 바인딩 (Assign)
        uiTitle.texLogo = FindComponentInScene<UnityEngine.UI.RawImage>("texLogo");
        uiTitle.textTouchScreen = FindComponentInScene<UnityEngine.UI.Text>("textTouchScreen");
        
        var optionBtn = FindComponentInScene<UnityEngine.UI.Button>("btnOption");
        if (optionBtn == null) optionBtn = FindComponentInScene<UnityEngine.UI.Button>("btnSound");
        if (optionBtn != null)
        {
            uiTitle.btnOption = optionBtn;
        }

        // 6. Title 컨트롤러에 UI_Title 뷰 바인딩
        var serializedMain = new SerializedObject(titleController);
        var uiProp = serializedMain.FindProperty("ui");
        if (uiProp != null)
        {
            uiProp.objectReferenceValue = uiTitle;
            serializedMain.ApplyModifiedProperties();
        }

        // UI_Title에 대해서도 직렬화 수정사항 적용
        var serializedUI = new SerializedObject(uiTitle);
        serializedUI.ApplyModifiedProperties();

        // 7. 변경 사항 표시 및 씬 저장
        EditorUtility.SetDirty(targetGo);
        EditorUtility.SetDirty(uiTitle);
        EditorUtility.SetDirty(titleController);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[TitleUISetup] ===================================================");
        Debug.Log("[TitleUISetup]  Title UI Setup & Auto-Creation Completed Successfully!");
        Debug.Log("[TitleUISetup]  - Host Object: " + targetGo.name);
        Debug.Log("[TitleUISetup]  - UI_Title: Component references permanently assigned");
        Debug.Log("[TitleUISetup]    * texLogo: " + (uiTitle.texLogo != null ? "Bound & Ready" : "NOT FOUND"));
        Debug.Log("[TitleUISetup]    * textTouchScreen: " + (uiTitle.textTouchScreen != null ? "Bound & Ready" : "NOT FOUND"));
        Debug.Log("[TitleUISetup]    * btnOption: " + (uiTitle.btnOption != null ? "Bound & Ready" : "NOT FOUND"));
        Debug.Log("[TitleUISetup] ===================================================");
    }

    private static void CreateTitleUIStructure(UI_Title uiTitle)
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
            Debug.Log("[TitleUISetup] Created default 'Canvas' for UI placement.");
        }

        // 2. EventSystem 생성 및 구성 (마우스 클릭 동작 필수)
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(esGo, "Create EventSystem");
            Debug.Log("[TitleUISetup] Created default 'EventSystem' for click input handler.");
        }

        Transform canvasTrans = canvas.transform;

        // 3. texLogo (로고 이미지) 자동 생성
        if (FindComponentInScene<UnityEngine.UI.RawImage>("texLogo") == null)
        {
            GameObject logoGo = new GameObject("texLogo");
            logoGo.transform.SetParent(canvasTrans, false);
            var rawImg = logoGo.AddComponent<UnityEngine.UI.RawImage>();
            
            // 로고는 위쪽에 큼직하게 배치
            rawImg.rectTransform.anchoredPosition = new Vector2(0, 150);
            rawImg.rectTransform.sizeDelta = new Vector2(400, 120);
            
            // UI/logo 로드하여 기본 바인딩 (하얀 블록 방지)
            rawImg.texture = Resources.Load("UI/logo") as Texture;
            rawImg.color = Color.white;

            Undo.RegisterCreatedObjectUndo(logoGo, "Create texLogo");
            Debug.Log("[TitleUISetup] Created 'texLogo' RawImage with logo texture.");
        }

        // 4. textTouchScreen (터치 유도 텍스트) 자동 생성
        if (FindComponentInScene<UnityEngine.UI.Text>("textTouchScreen") == null)
        {
            GameObject textGo = new GameObject("textTouchScreen");
            textGo.transform.SetParent(canvasTrans, false);
            var textComp = textGo.AddComponent<UnityEngine.UI.Text>();
            
            textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (textComp.font == null)
            {
                Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
                if (fonts != null && fonts.Length > 0) textComp.font = fonts[0];
            }

            textComp.text = "TOUCH TO START";
            textComp.fontSize = 28;
            textComp.alignment = TextAnchor.MiddleCenter;
            textComp.color = Color.white;
            
            // 하단 쪽에 자연스럽게 펄싱 가독성 확보 배치
            textComp.rectTransform.anchoredPosition = new Vector2(0, -100);
            textComp.rectTransform.sizeDelta = new Vector2(400, 80);
            
            // 가독성 아웃라인 및 그림자 장착
            AddOutlineAndShadow(textGo);
            
            Undo.RegisterCreatedObjectUndo(textGo, "Create textTouchScreen");
            Debug.Log("[TitleUISetup] Created 'textTouchScreen' Text with Outline styling.");
        }

        // 5. btnSound (사운드 제어 버튼) 자동 생성
        if (FindComponentInScene<UnityEngine.UI.Button>("btnSound") == null)
        {
            GameObject btnGo = new GameObject("btnSound");
            btnGo.transform.SetParent(canvasTrans, false);
            var btnComp = btnGo.AddComponent<UnityEngine.UI.Button>();
            var rawImg = btnGo.AddComponent<UnityEngine.UI.RawImage>();
            
            // 사운드 버튼은 우측 상단 앵커로 정렬
            rawImg.rectTransform.anchorMin = new Vector2(1, 1);
            rawImg.rectTransform.anchorMax = new Vector2(1, 1);
            rawImg.rectTransform.pivot = new Vector2(1, 1);
            rawImg.rectTransform.anchoredPosition = new Vector2(-40, -40); // 우상단 여백 40
            rawImg.rectTransform.sizeDelta = new Vector2(80, 80);
            
            // UI/btn_option 로드하여 기본 바인딩 (하얀 블록 방지)
            Texture optTex = Resources.Load("UI/btn_option") as Texture;
            if (optTex == null) optTex = Resources.Load("UI/sound_on") as Texture;
            rawImg.texture = optTex;
            rawImg.color = Color.white;

            Undo.RegisterCreatedObjectUndo(btnGo, "Create btnSound");
            Debug.Log("[TitleUISetup] Created 'btnSound' Button at top-right anchor.");
        }
    }

    private static void AddOutlineAndShadow(GameObject go)
    {
        if (go.GetComponent<UnityEngine.UI.Outline>() == null)
        {
            var outline = go.AddComponent<UnityEngine.UI.Outline>();
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
