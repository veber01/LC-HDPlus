using HarmonyLib;
using GameNetcodeStuff;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using System.Collections.Generic;

namespace HDPlus.Patches
{

    internal class ResolutionPatch
    {
        public static readonly (int width, int height, string label)[] Presets =
        {
            (860,  520,  "Vanilla"),
            (1280, 720,  "1280x720"),
            (1920, 1080, "1920x1080"),
            (2560, 1440, "2560x1440"),
            (3840, 2160, "3840x2160"),
        };

        public static readonly (int width, int height, string label)[] UWPresets =
        {
            (860,  520,  "Vanilla"),
            (2560, 1080, "2560x1080 (21:9)"),
            (3440, 1440, "3440x1440 (21:9)"),
        };

        private static int _index = -1;
        private static int Indexx
        {
            get
            {
                if (_index == -1)
                    _index = Mathf.Clamp(Plugin.ResValue.Value, 0, Presets.Length - 1);
                return _index;
            }
            set => _index = Mathf.Clamp(value, 0, Presets.Length - 1);
        }

        private static int _uwIndex = -1;
        private static int UWIndex
        {
            get
            {
                if (_uwIndex == -1)
                    _uwIndex = Mathf.Clamp(Plugin.UWResValue.Value, 0, UWPresets.Length - 1);
                return _uwIndex;
            }
            set => _uwIndex = Mathf.Clamp(value, 0, UWPresets.Length - 1);
        }
        private static bool UWOn => Plugin.UWEnabled.Value;
        private static (int width, int height, string label) ActivePreset => UWOn ? UWPresets[UWIndex] : Presets[Indexx];
        private static float ActiveAspect => (float)ActivePreset.width / ActivePreset.height;

        private static void applyres(RenderTexture rt)
        {
            if (rt == null) return;
            var (w, h, _) = ActivePreset;
            if (rt.width == w && rt.height == h) return;
            rt.Release();
            rt.width = w;
            rt.height = h;
        }

        private static void ApplyRes()
        {
            if (IngamePlayerSettings.Instance?.playerGameplayScreenTex != null)
                applyres(IngamePlayerSettings.Instance.playerGameplayScreenTex);

            PlayerControllerB[] players = UnityEngine.Object.FindObjectsOfType<PlayerControllerB>();
            foreach (var p in players)
            {
                if (p.gameplayCamera?.targetTexture != null)
                    applyres(p.gameplayCamera.targetTexture);
            }

            if (UWOn)
                UWStuff();
            else
                ResetAspect();
        }

        private static void UWStuff()
        {
            float aspect = ActiveAspect;

            Camera camera = GameNetworkManager.Instance?.localPlayerController?.gameplayCamera;
            camera?.ResetAspect();

            GameObject panelObject = GameObject.Find("Systems/UI/Canvas/Panel");
            if (panelObject != null && panelObject.TryGetComponent(out AspectRatioFitter arf))
            {
                arf.enabled = true;
                arf.aspectRatio = aspect;
            }

            GameObject canvasObject = GameObject.Find("Systems/UI/Canvas");
            if (canvasObject != null && canvasObject.TryGetComponent(out CanvasScaler canvasScaler))
                canvasScaler.referenceResolution = new Vector2(500f * aspect, 500f);

            HUDManager hudManager = HUDManager.Instance;
            if (hudManager != null)
            {
                GameObject terminalObject = GameObject.Find("TerminalScript");
                if (terminalObject != null && terminalObject.TryGetComponent(out Terminal terminal))
                {
                    RenderTexture termTex = terminal.playerScreenTexHighRes;
                    termTex.Release();
                    termTex.height = 580;
                    termTex.width = Convert.ToInt32(580 * aspect);
                }

                GameObject hudObject = hudManager.HUDContainer;
                if (hudObject != null && hudObject.TryGetComponent(out AspectRatioFitter arf2))
                    arf2.aspectRatio = aspect;

                GameObject uiCamObject = GameObject.Find("Systems/UI/UICamera");
                if (uiCamObject != null && uiCamObject.TryGetComponent(out Camera uiCamera))
                    uiCamera.fieldOfView = Mathf.Min(106f / aspect, 60f);

                GameObject inventoryObject = hudManager.Inventory.canvasGroup.gameObject;
                if (inventoryObject != null && inventoryObject.TryGetComponent(out RectTransform invRect))
                {
                    invRect.anchoredPosition = Vector2.zero;
                    invRect.anchorMax = new Vector2(0.5f, 0f);
                    invRect.anchorMin = new Vector2(0.5f, 0.5f);
                    invRect.pivot = new Vector2(0.5f, 0f);
                }

                GameObject helmetModel = GameObject.Find("PlayerHUDHelmetModel");
                if (helmetModel != null && helmetModel.TryGetComponent(out Transform helmetTransform))
                {
                    Vector3 scale = helmetTransform.localScale;
                    scale.x = 0.3628f * Mathf.Max(aspect / 2.3f, 1f);
                    helmetTransform.localScale = scale;
                }
            }
        }
        private static bool IsVanilla => !UWOn && Indexx == 0;
        private static void ResetAspect()
        {
            if (!_originalsCaptured)
            {
                Plugin.Logger.LogWarning("[HDPlus] ResetAspect called but originals not captured");
                return;
            }

            Camera camera = GameNetworkManager.Instance?.localPlayerController?.gameplayCamera;camera?.ResetAspect();
            GameObject panelObject = GameObject.Find("Systems/UI/Canvas/Panel");
            if (panelObject != null && panelObject.TryGetComponent(out AspectRatioFitter arf))
            {
                arf.aspectRatio = _originalPanelAspect;
                arf.enabled = !IsVanilla;
            }

            GameObject canvasObject = GameObject.Find("Systems/UI/Canvas");
            if (canvasObject != null && canvasObject.TryGetComponent(out CanvasScaler canvasScaler))
            {
                canvasScaler.referenceResolution = _originalCanvasRes;
            }

            HUDManager hudManager = HUDManager.Instance;
            if (hudManager != null)
            {
                GameObject hudObject = hudManager.HUDContainer;
                if (hudObject != null && hudObject.TryGetComponent(out AspectRatioFitter arf2))
                {
                    arf2.aspectRatio = _originalHudAspect;
                }

                GameObject uiCamObject = GameObject.Find("Systems/UI/UICamera");
                if (uiCamObject != null && uiCamObject.TryGetComponent(out Camera uiCamera))
                {
                    uiCamera.fieldOfView = _originalUICamFOV;
                }

                GameObject inventoryObject = hudManager.Inventory.canvasGroup.gameObject;
                if (inventoryObject != null && inventoryObject.TryGetComponent(out RectTransform invRect))
                {
                    invRect.anchorMin = _originalInvAnchorMin;
                    invRect.anchorMax = _originalInvAnchorMax;
                    invRect.pivot = _originalInvPivot;
                    invRect.anchoredPosition = _originalInvPos;
                }
                GameObject helmetModel = GameObject.Find("PlayerHUDHelmetModel");
                if (helmetModel != null && helmetModel.TryGetComponent(out Transform helmetTransform))
                {
                    Vector3 scale = helmetTransform.localScale;
                    scale.x = _originalHelmetX;
                    helmetTransform.localScale = scale;
                }
            }
        }


        private static float _originalPanelAspect = -1f;
        private static float _originalHudAspect = -1f;
        private static float _originalUICamFOV = -1f;
        private static float _originalHelmetX = -1f;
        private static Vector2 _originalCanvasRes = Vector2.zero;
        private static bool _originalsCaptured = false;
        private static Vector2 _originalInvAnchorMin = Vector2.zero;
        private static Vector2 _originalInvAnchorMax = Vector2.zero;
        private static Vector2 _originalInvPivot = Vector2.zero;
        private static Vector2 _originalInvPos = Vector2.zero;

        private static void SaveOrigs()
        {

            if (_originalsCaptured) return;

            GameObject panelObject = GameObject.Find("Systems/UI/Canvas/Panel");
            if (panelObject != null && panelObject.TryGetComponent(out AspectRatioFitter arf))
                _originalPanelAspect = arf.aspectRatio;

            GameObject canvasObject = GameObject.Find("Systems/UI/Canvas");
            if (canvasObject != null && canvasObject.TryGetComponent(out CanvasScaler canvasScaler))
                _originalCanvasRes = canvasScaler.referenceResolution;

            HUDManager hudManager = HUDManager.Instance;
            if (hudManager != null)
            {
                GameObject hudObject = hudManager.HUDContainer;
                if (hudObject != null && hudObject.TryGetComponent(out AspectRatioFitter arf2))
                    _originalHudAspect = arf2.aspectRatio;

                GameObject uiCamObject = GameObject.Find("Systems/UI/UICamera");
                if (uiCamObject != null && uiCamObject.TryGetComponent(out Camera uiCamera))
                    _originalUICamFOV = uiCamera.fieldOfView;

                GameObject helmetModel = GameObject.Find("PlayerHUDHelmetModel");
                if (helmetModel != null && helmetModel.TryGetComponent(out Transform helmetTransform))
                    _originalHelmetX = helmetTransform.localScale.x;

                GameObject inventoryObject = hudManager.Inventory.canvasGroup.gameObject;
                if (inventoryObject != null && inventoryObject.TryGetComponent(out RectTransform invRect))
                {
                    _originalInvAnchorMin = invRect.anchorMin;
                    _originalInvAnchorMax = invRect.anchorMax;
                    _originalInvPivot = invRect.pivot;
                    _originalInvPos = invRect.anchoredPosition;
                }
            }


            if (_originalPanelAspect > 0)
            {
                _originalsCaptured = true;
            }
        }


        [HarmonyPatch(typeof(PlayerControllerB), "Start")]
        [HarmonyPostfix]
        private static void pcb(PlayerControllerB __instance)
        {
            if (__instance.gameplayCamera?.targetTexture == null) return;
            SaveOrigs();
            applyres(__instance.gameplayCamera.targetTexture);
            if (UWOn)
                UWStuff();
            else
                ResetAspect();

        }

        [HarmonyPatch(typeof(IngamePlayerSettings), "SetPixelResolution")]
        [HarmonyPostfix]
        private static void setres(IngamePlayerSettings __instance)
        {
            if (__instance.playerGameplayScreenTex == null) return;
            applyres(__instance.playerGameplayScreenTex);
        }
        private static IEnumerator inject(IngamePlayerSettings instance)
        {
            yield return null;
            yield return null;
            Injectbutton(instance);
        }

        [HarmonyPatch(typeof(IngamePlayerSettings), "UpdateGameToMatchSettings")]
        [HarmonyPostfix]
        private static void update(IngamePlayerSettings __instance)
        {
            __instance.StartCoroutine(inject(__instance));
        }

        [HarmonyPatch(typeof(IngamePlayerSettings), "SaveChangedSettings")]
        [HarmonyPostfix]
        private static void save()
        {
            if (UWOn)
                Plugin.UWResValue.Value = UWIndex;
            else
                Plugin.ResValue.Value = Indexx;

            ApplyRes();
        }

        [HarmonyPatch(typeof(IngamePlayerSettings), "DiscardChangedSettings")]
        [HarmonyPostfix]
        private static void discard()
        {
            if (UWOn)
                UWIndex = Mathf.Clamp(Plugin.UWResValue.Value, 0, UWPresets.Length - 1);
            else
                Indexx = Mathf.Clamp(Plugin.ResValue.Value, 0, Presets.Length - 1);

            Refreshelement();
        }

        private static void Injectbutton(IngamePlayerSettings instance)
        {
            var activePresets = UWOn ? UWPresets : Presets;
            int activeIndex = UWOn ? UWIndex : Indexx;

            Reselement existing = UnityEngine.Object.FindObjectOfType<Reselement>(includeInactive: true);
            if (existing != null)
            {
                Transform vt = existing.transform.Find("Value");
                if (vt != null)
                {
                    TextMeshProUGUI vtext = vt.GetComponent<TextMeshProUGUI>();
                    if (vtext != null)
                        vtext.text = activePresets[activeIndex].label;
                }
                return;
            }

            SettingsOption[] bruh = UnityEngine.Object.FindObjectsOfType<SettingsOption>(includeInactive: true);
            if (bruh == null || bruh.Length == 0) return;

            Transform container = null;
            Transform[] allTransforms = UnityEngine.Object.FindObjectsOfType<Transform>(includeInactive: true);
            foreach (Transform t in allTransforms)
            {
                if (t.name == "SettingsPanel" && t.parent?.name == "QuickMenu")
                {
                    container = t;
                    break;
                }
            }

            if (container == null)
                container = bruh[bruh.Length - 1].transform.parent;

            if (container == null) return;

            GameObject root = new GameObject("menuelement");
            root.transform.SetParent(container, worldPositionStays: false);
            root.AddComponent<Reselement>();

            for (int i = 0; i < container.childCount; i++)
            {
                if (container.GetChild(i).name == "PixelRes")
                {
                    root.transform.SetSiblingIndex(i + 1);
                    break;
                }
            }

            RectTransform pixelResRect = container.Find("PixelRes")?.GetComponent<RectTransform>();
            RectTransform rootRect = root.AddComponent<RectTransform>();
            Image rootBg = root.AddComponent<Image>();
            GameObject pixelResObj = container.Find("PixelRes")?.gameObject;
            if (pixelResObj != null)
            {
                Image sourceImg = pixelResObj.GetComponentInChildren<Image>();
                if (sourceImg != null)
                {
                    rootBg.sprite = sourceImg.sprite;
                    rootBg.type = Image.Type.Sliced;
                    rootBg.fillCenter = true;
                    rootBg.color = sourceImg.color;
                    rootBg.pixelsPerUnitMultiplier = sourceImg.pixelsPerUnitMultiplier;
                }
            }
            else
            {
                rootBg.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
            }

            if (pixelResRect != null)
            {
                rootRect.anchorMin = pixelResRect.anchorMin;
                rootRect.anchorMax = pixelResRect.anchorMax;
                rootRect.pivot = pixelResRect.pivot;
                rootRect.sizeDelta = pixelResRect.sizeDelta;
                rootRect.anchoredPosition = new Vector2(
                    pixelResRect.anchoredPosition.x,
                    pixelResRect.anchoredPosition.y - (pixelResRect.sizeDelta.y * 2f) - 20f
                );
            }
            else
            {
                rootRect.anchorMin = new Vector2(0.5f, 0.5f);
                rootRect.anchorMax = new Vector2(0.5f, 0.5f);
                rootRect.pivot = new Vector2(0.5f, 0.5f);
                rootRect.sizeDelta = new Vector2(184f, 30f);
                rootRect.anchoredPosition = Vector2.zero;
            }
            GameObject label = new GameObject("Label");
            label.transform.SetParent(root.transform, worldPositionStays: false);
            RectTransform labelRect = label.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.03f, 0f);
            labelRect.anchorMax = new Vector2(0.4f, 1f);
            labelRect.sizeDelta = Vector2.zero;
            labelRect.anchoredPosition = Vector2.zero;
            TextMeshProUGUI labelText = label.AddComponent<TextMeshProUGUI>();
            labelText.text = "RESOLUTION";
            labelText.fontSize = 13f;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            font(labelText, bruh);
            GameObject lbutton = new GameObject("ButtonLeft");
            lbutton.transform.SetParent(root.transform, worldPositionStays: false);
            RectTransform leftRect = lbutton.AddComponent<RectTransform>();
            leftRect.anchorMin = new Vector2(0.44f, 0.1f);
            leftRect.anchorMax = new Vector2(0.55f, 0.9f);
            leftRect.sizeDelta = Vector2.zero;
            leftRect.anchoredPosition = Vector2.zero;
            Image leftImg = lbutton.AddComponent<Image>();
            leftImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            Button leftButton = lbutton.AddComponent<Button>();
            GameObject leftLabel = new GameObject("Text");
            leftLabel.transform.SetParent(lbutton.transform, worldPositionStays: false);
            RectTransform llRect = leftLabel.AddComponent<RectTransform>();
            llRect.anchorMin = Vector2.zero;
            llRect.anchorMax = Vector2.one;
            llRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI leftText = leftLabel.AddComponent<TextMeshProUGUI>();
            leftText.text = "<";
            leftText.fontSize = 16f;
            leftText.alignment = TextAlignmentOptions.Center;
            font(leftText, bruh);
            GameObject value = new GameObject("Value");
            value.transform.SetParent(root.transform, worldPositionStays: false);
            RectTransform valueRect = value.AddComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0.55f, 0f);
            valueRect.anchorMax = new Vector2(0.86f, 1f);
            valueRect.sizeDelta = Vector2.zero;
            valueRect.anchoredPosition = Vector2.zero;
            TextMeshProUGUI valueText = value.AddComponent<TextMeshProUGUI>();
            valueText.text = activePresets[activeIndex].label;
            valueText.fontSize = 11f;
            valueText.alignment = TextAlignmentOptions.Center;
            font(valueText, bruh);
            GameObject rbutton = new GameObject("ButtonRight");
            rbutton.transform.SetParent(root.transform, worldPositionStays: false);
            RectTransform rightRect = rbutton.AddComponent<RectTransform>();
            rightRect.anchorMin = new Vector2(0.87f, 0.1f);
            rightRect.anchorMax = new Vector2(0.98f, 0.9f);
            rightRect.sizeDelta = Vector2.zero;
            rightRect.anchoredPosition = Vector2.zero;
            Image rightImg = rbutton.AddComponent<Image>();
            rightImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            Button rightButton = rbutton.AddComponent<Button>();
            GameObject rightLabel = new GameObject("Text");
            rightLabel.transform.SetParent(rbutton.transform, worldPositionStays: false);
            RectTransform rlRect = rightLabel.AddComponent<RectTransform>();
            rlRect.anchorMin = Vector2.zero;
            rlRect.anchorMax = Vector2.one;
            rlRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI rightText = rightLabel.AddComponent<TextMeshProUGUI>();
            rightText.text = ">";
            rightText.fontSize = 16f;
            rightText.alignment = TextAlignmentOptions.Center;
            font(rightText, bruh);

            leftButton.onClick.AddListener(() =>
            {
                if (UWOn)
                {
                    UWIndex = (UWIndex - 1 + UWPresets.Length) % UWPresets.Length;
                    valueText.text = UWPresets[UWIndex].label;
                }
                else
                {
                    Indexx = (Indexx - 1 + Presets.Length) % Presets.Length;
                    valueText.text = Presets[Indexx].label;
                }
                IngamePlayerSettings.Instance.changesNotApplied = true;
            });
            rightButton.onClick.AddListener(() =>
            {
                if (UWOn)
                {
                    UWIndex = (UWIndex + 1) % UWPresets.Length;
                    valueText.text = UWPresets[UWIndex].label;
                }
                else
                {
                    Indexx = (Indexx + 1) % Presets.Length;
                    valueText.text = Presets[Indexx].label;
                }
                IngamePlayerSettings.Instance.changesNotApplied = true;
            });
        }
        private static void Refreshelement()
        {
            Reselement marker = UnityEngine.Object.FindObjectOfType<Reselement>(includeInactive: true);
            if (marker == null) return;
            Transform vt = marker.transform.Find("Value");
            if (vt == null) return;
            TextMeshProUGUI valueText = vt.GetComponent<TextMeshProUGUI>();
            if (valueText == null) return;
            valueText.text = UWOn ? UWPresets[UWIndex].label : Presets[Indexx].label;
        }
        private static void font(TextMeshProUGUI target, SettingsOption[] options)
        {
            foreach (var opt in options)
            {
                TextMeshProUGUI existing = opt.GetComponentInChildren<TextMeshProUGUI>();
                if (existing != null)
                {
                    target.font = existing.font;
                    target.color = existing.color;
                    return;
                }
            }
        }
        [HarmonyPatch(typeof(HUDManager), "UpdateScanNodes")]
        [HarmonyPostfix]
        private static void scannodes(PlayerControllerB playerScript, HUDManager __instance,
                    Dictionary<RectTransform, ScanNodeProperties> ___scanNodes)
        {
            if (!UWOn) return;

            RectTransform[] scanElements = __instance.scanElements;
            GameObject playerScreen = __instance.playerScreenTexture.gameObject;
            if (!playerScreen.TryGetComponent(out RectTransform screenTransform)) return;

            Rect rect = screenTransform.rect;
            for (int i = 0; i < scanElements.Length; i++)
            {
                if (___scanNodes.TryGetValue(scanElements[i], out ScanNodeProperties scanNode))
                {
                    Vector3 viewportPos = playerScript.gameplayCamera.WorldToViewportPoint(scanNode.transform.position);
                    scanElements[i].anchoredPosition = new Vector2(
                        rect.xMin + rect.width * viewportPos.x,
                        rect.yMin + rect.height * viewportPos.y
                    );
                }
            }
        }







    }

    internal class Reselement : MonoBehaviour { }
}