using System.Collections.Generic;
using Study.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Study3D.OutPost
{
    // B키 건설 메뉴 → 포탑 선택 → 미리보기를 마우스 위치에 띄우고 좌클릭으로 설치
    // 메뉴/미리보기 UI는 실행 시 코드로 만들어짐 (씬에 UI를 따로 만들 필요 없음)
    // 씬의 빈 오브젝트(TurretSystem)에 TurretRepairer와 같이 붙임
    public class TurretBuilder : MonoBehaviour
    {
        // 메뉴가 열려 있거나 배치 중이면 true → PlayerController가 사격 입력을 막음
        public static bool IsBusy { get; private set; }

        [Header("건설 가능한 포탑")]
        [SerializeField] private List<TurretData> turrets = new();

        [Header("배치")]
        [Tooltip("설치할 수 있는 바닥 레이어 (Default, Terrain 등)")]
        [SerializeField] private LayerMask groundMask = 1;
        [Tooltip("이 레이어와 겹치면 설치 불가 (Buildings, Obstacle, Enemy, 포탑 등)")]
        [SerializeField] private LayerMask blockMask;
        [Tooltip("포탑이 차지하는 반지름 (m)")]
        [SerializeField] private float footprintRadius = 0.8f;
        [Tooltip("플레이어에게서 이 거리 안에만 설치 가능 (m)")]
        [SerializeField] private float maxBuildDistance = 8f;
        [SerializeField] private float rotateStep = 45f;   // R키로 돌리는 각도

        [Header("미리보기")]
        [Tooltip("반투명 머티리얼 (URP Lit, Surface Type = Transparent). 비우면 원래 머티리얼 사용")]
        [SerializeField] private Material ghostMaterial;
        [SerializeField] private Color okColor = new(0.3f, 1f, 0.3f, 0.4f);
        [SerializeField] private Color badColor = new(1f, 0.3f, 0.3f, 0.4f);

        [Header("UI")]
        [SerializeField] private TMP_FontAsset font;   // 한글 폰트

        private enum Mode { None, Menu, Placing }

        private Mode mode = Mode.None;
        private TurretData selected;
        private GameObject ghost;
        private Renderer[] ghostRenderers;
        private MaterialPropertyBlock propertyBlock;
        private float ghostYaw;
        private Camera cam;

        private GameObject menuRoot;
        private GameObject hintRoot;
        private TMP_Text hintText;
        private readonly List<(TurretData data, Button button, TMP_Text costText)> menuButtons = new();

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        // 컴포넌트를 처음 붙였을 때 레이어 기본값 채우기
        private void Reset()
        {
            groundMask = LayerMask.GetMask("Default", "Terrain");
            blockMask = LayerMask.GetMask("Buildings", "Obstacle", "Pillars", "Trees", "Enemy", "Turret");
        }

        private void Start()
        {
            propertyBlock = new MaterialPropertyBlock();
            BuildUI();
            SetMode(Mode.None);
        }

        private void OnDestroy()
        {
            IsBusy = false;
        }

        private void Update()
        {
            if (cam == null) cam = Camera.main;

            // 게임이 끝났으면 건설 종료
            if (GameManager.IsGameOver)
            {
                if (mode != Mode.None) SetMode(Mode.None);
                return;
            }

            PlayerController player = PlayerController.Instance;
            bool playerDead = player == null || (player.TryGetComponent(out PlayerHealth hp) && hp.IsDead);
            bool inventoryOpen = InventorySystem.IsInitialized && InventorySystem.Instance.IsOpen;

            // 플레이어가 없거나 죽었거나 인벤토리가 열리면 건설 종료
            if (playerDead || inventoryOpen)
            {
                if (mode != Mode.None) SetMode(Mode.None);
                return;
            }

            bool bDown = SimpleInput.GetKeyDown(Key.B);
            bool escDown = SimpleInput.GetKeyDown(Key.Escape);

            switch (mode)
            {
                case Mode.None:
                    if (bDown) SetMode(Mode.Menu);
                    break;

                case Mode.Menu:
                    if (bDown || escDown) { SetMode(Mode.None); break; }
                    RefreshMenu();
                    break;

                case Mode.Placing:
                    bool rightDown = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
                    if (bDown || escDown || rightDown) { SetMode(Mode.None); break; }
                    UpdatePlacing(player.transform.position);
                    break;
            }
        }

        private void SetMode(Mode newMode)
        {
            mode = newMode;
            IsBusy = newMode != Mode.None;

            if (menuRoot != null) menuRoot.SetActive(newMode == Mode.Menu);
            if (hintRoot != null) hintRoot.SetActive(newMode == Mode.Placing);
            if (newMode != Mode.Placing) DestroyGhost();
            if (newMode == Mode.Menu) RefreshMenu();
        }

        #region 배치

        private void Select(TurretData data)
        {
            selected = data;
            CreateGhost(data);
            SetMode(Mode.Placing);
        }

        private void UpdatePlacing(Vector3 playerPos)
        {
            if (SimpleInput.GetKeyDown(Key.R)) ghostYaw += rotateStep;

            bool hasPoint = TryGetGroundPoint(out Vector3 pos);
            ghost.SetActive(hasPoint);
            if (!hasPoint)
            {
                hintText.text = Hint("Point at the ground", false);
                return;
            }

            Quaternion rot = Quaternion.Euler(0f, ghostYaw, 0f);
            ghost.transform.SetPositionAndRotation(pos, rot);

            // 설치 가능 여부 (안 되는 이유를 하나만 보여줌)
            string reason = null;
            Vector3 flat = pos - playerPos;
            flat.y = 0f;
            if (flat.magnitude > maxBuildDistance) reason = "Too far away";
            else if (IsBlocked(pos)) reason = "Blocked by an object";
            else if (IsBomb(selected)) reason = BombBlockReason(pos);
            if (reason == null && !TurretCost.CanAfford(selected, false)) reason = "Not enough materials";

            bool ok = reason == null;
            SetGhostColor(ok ? okColor : badColor);
            hintText.text = Hint(ok ? null : reason, ok);

            bool clicked = SimpleInput.GetMouseButtonDown(SimpleInput.MouseButton.Left);
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (ok && clicked && !overUI) Place(pos, rot);
        }

        #region 하이브 폭탄

        // 프리팹에 HiveBomb이 붙어 있으면 폭탄
        private static bool IsBomb(TurretData data)
            => data != null && data.TurretPrefab != null && data.TurretPrefab.GetComponent<HiveBomb>() != null;

        // 하이브가 남아 있고, 아직 설치된 폭탄이 없을 때만 건설 가능
        private static bool CanBuildBomb()
            => Hive.Instance != null && !Hive.Instance.IsDestroyed && HiveBomb.Active == null;

        // 설치 불가 이유 (가능하면 null)
        private static string BombBlockReason(Vector3 pos)
        {
            if (Hive.Instance == null || Hive.Instance.IsDestroyed) return "No Hive";
            if (HiveBomb.Active != null) return "Bomb already planted";
            if (!Hive.Instance.IsInPlantZone(pos)) return "Must be placed near the Hive";
            return null;
        }

        #endregion

        private bool TryGetGroundPoint(out Vector3 point)
        {
            point = default;
            if (cam == null) return false;

            Ray ray = cam.ScreenPointToRay(SimpleInput.GetMousePosition());
            if (!Physics.Raycast(ray, out RaycastHit hit, 200f, groundMask, QueryTriggerInteraction.Ignore)) return false;

            point = hit.point;
            return true;
        }

        private bool IsBlocked(Vector3 pos)
        {
            // 바닥에 살짝 띄운 구로 검사 (바닥 자체와 겹치지 않게)
            Vector3 center = pos + Vector3.up * (footprintRadius + 0.05f);
            return Physics.CheckSphere(center, footprintRadius, blockMask, QueryTriggerInteraction.Ignore);
        }

        private void Place(Vector3 pos, Quaternion rot)
        {
            TurretCost.Pay(selected, false);

            GameObject obj = Instantiate(selected.TurretPrefab, pos, rot);
            if (obj.TryGetComponent(out AutoTurret turret)) turret.Init(selected);
            else if (obj.TryGetComponent(out HiveBomb bomb)) bomb.Init(selected);

            Debug.Log($"{selected.DisplayName} 건설");
            SetMode(Mode.None);
        }

        private string Hint(string error, bool ok)
        {
            string name = selected != null ? selected.DisplayName : "";
            string cost = TurretCost.Describe(selected, false);
            string line = ok || error == null ? "" : $"\n<color=#FF6060>{error}</color>";
            return $"{name}  ({cost})\nLMB Build · R Rotate · RMB/B Cancel{line}";
        }

        #endregion

        #region 미리보기 (모델만 복사)

        // 프리팹의 MeshRenderer만 복사해서 만듦 → 스크립트/콜라이더가 없어서 아무 동작도 안 함
        private void CreateGhost(TurretData data)
        {
            DestroyGhost();

            ghost = new GameObject($"Ghost_{data.DisplayName}");
            Transform prefabRoot = data.TurretPrefab.transform;

            foreach (MeshFilter mf in data.TurretPrefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null || !IsActiveInPrefab(mf.transform, prefabRoot)) continue;
                if (!mf.TryGetComponent(out MeshRenderer sourceRenderer)) continue;

                var part = new GameObject(mf.name);
                part.transform.SetParent(ghost.transform, false);
                part.transform.localPosition = prefabRoot.InverseTransformPoint(mf.transform.position);
                part.transform.localRotation = Quaternion.Inverse(prefabRoot.rotation) * mf.transform.rotation;
                part.transform.localScale = Divide(mf.transform.lossyScale, prefabRoot.lossyScale);

                part.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                var renderer = part.AddComponent<MeshRenderer>();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                Material[] mats = sourceRenderer.sharedMaterials;
                if (ghostMaterial != null)
                    for (int i = 0; i < mats.Length; i++) mats[i] = ghostMaterial;
                renderer.sharedMaterials = mats;
            }

            ghostRenderers = ghost.GetComponentsInChildren<Renderer>();
        }

        private void DestroyGhost()
        {
            if (ghost != null) Destroy(ghost);
            ghost = null;
            ghostRenderers = null;
        }

        private void SetGhostColor(Color color)
        {
            if (ghostMaterial == null || ghostRenderers == null) return;   // 원래 머티리얼이면 색 안 바꿈

            propertyBlock.SetColor(BaseColorId, color);   // URP
            propertyBlock.SetColor(ColorId, color);       // Built-in
            foreach (Renderer r in ghostRenderers) r.SetPropertyBlock(propertyBlock);
        }

        private static bool IsActiveInPrefab(Transform t, Transform root)
        {
            for (Transform cur = t; cur != null; cur = cur.parent)
            {
                if (!cur.gameObject.activeSelf) return false;
                if (cur == root) break;
            }
            return true;
        }

        private static Vector3 Divide(Vector3 a, Vector3 b)
            => new(a.x / b.x, a.y / b.y, a.z / b.z);

        #endregion

        #region UI 만들기

        private void BuildUI()
        {
            // 화면 캔버스
            var canvasObj = new GameObject("TurretBuild UI", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObj.transform.SetParent(transform, false);
            var canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // ── 건설 메뉴 (하단 중앙)
            menuRoot = CreatePanel("Menu", canvasObj.transform, new Color(0f, 0f, 0f, 0.75f));
            var menuRect = (RectTransform)menuRoot.transform;
            menuRect.anchorMin = menuRect.anchorMax = new Vector2(0.5f, 0f);
            menuRect.pivot = new Vector2(0.5f, 0f);
            menuRect.anchoredPosition = new Vector2(0f, 180f);

            var layout = menuRoot.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 16, 20);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            menuRoot.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            menuRoot.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateText("Title", menuRoot.transform, "Build Turret   <size=20>(B Close)</size>", 30, Color.white);

            var row = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(menuRoot.transform, false);
            var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 12f;
            rowLayout.childControlWidth = rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = rowLayout.childForceExpandHeight = false;

            foreach (TurretData data in turrets)
            {
                if (data == null) continue;
                menuButtons.Add(CreateTurretButton(row.transform, data));
            }

            // ── 배치 안내 (상단 중앙)
            hintRoot = CreatePanel("Hint", canvasObj.transform, new Color(0f, 0f, 0f, 0.6f));
            var hintRect = (RectTransform)hintRoot.transform;
            hintRect.anchorMin = hintRect.anchorMax = new Vector2(0.5f, 1f);
            hintRect.pivot = new Vector2(0.5f, 1f);
            hintRect.anchoredPosition = new Vector2(0f, -120f);
            hintRect.sizeDelta = new Vector2(760f, 120f);
            hintText = CreateText("Text", hintRoot.transform, "", 24, Color.white);
            var hintTextRect = (RectTransform)hintText.transform;
            hintTextRect.anchorMin = Vector2.zero;
            hintTextRect.anchorMax = Vector2.one;
            hintTextRect.offsetMin = hintTextRect.offsetMax = Vector2.zero;
        }

        private (TurretData, Button, TMP_Text) CreateTurretButton(Transform parent, TurretData data)
        {
            GameObject buttonObj = CreatePanel(data.DisplayName, parent, new Color(0.2f, 0.22f, 0.25f, 1f));
            var element = buttonObj.AddComponent<LayoutElement>();
            element.preferredWidth = 240f;
            element.preferredHeight = 200f;

            var button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonObj.GetComponent<Image>();
            button.onClick.AddListener(() => Select(data));

            var layout = buttonObj.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            if (data.Icon != null)
            {
                var iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                iconObj.transform.SetParent(buttonObj.transform, false);
                var icon = iconObj.GetComponent<Image>();
                icon.sprite = data.Icon;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                iconObj.GetComponent<LayoutElement>().preferredHeight = 90f;
            }

            CreateText("Name", buttonObj.transform, data.DisplayName, 24, Color.white);
            TMP_Text costText = CreateText("Cost", buttonObj.transform, "", 18, Color.white);
            return (data, button, costText);
        }

        private void RefreshMenu()
        {
            foreach (var entry in menuButtons)
            {
                bool canAfford = TurretCost.CanAfford(entry.data, false);
                bool bombOk = !IsBomb(entry.data) || CanBuildBomb();   // 폭탄은 동시에 1개만
                entry.button.interactable = canAfford && bombOk && entry.data.TurretPrefab != null;
                entry.costText.text = bombOk ? TurretCost.Describe(entry.data, false) : "Already planted";
                entry.costText.color = canAfford && bombOk ? Color.white : new Color(1f, 0.4f, 0.4f);
            }
        }

        private static GameObject CreatePanel(string objName, Transform parent, Color color)
        {
            var go = new GameObject(objName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private TMP_Text CreateText(string objName, Transform parent, string text, float size, Color color)
        {
            var go = new GameObject(objName, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return tmp;
        }

        #endregion
    }
}
