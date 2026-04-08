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
        private static Vector2 oldboxsize = Vector2.zero;
        private static RectTransform? newboxsize = null;

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
            CacheReferences();
            float aspect = ActiveAspect;

            Camera? camera = GameNetworkManager.Instance?.localPlayerController?.gameplayCamera;
            camera?.ResetAspect();

            if (_panel != null)
            {
                _panel.enabled = true;
                _panel.aspectRatio = aspect;
            }

            if (_canvas != null)
                _canvas.referenceResolution = new Vector2(500f * aspect, 500f);

            HUDManager hudManager = HUDManager.Instance;
            if (hudManager != null)
            {
                if (_terminal?.playerScreenTexHighRes != null)
                {
                    RenderTexture termTex = _terminal.playerScreenTexHighRes;
                    termTex.Release();
                    termTex.height = 580;
                    termTex.width = Convert.ToInt32(580 * aspect);
                }
                if (_hud != null) _hud.aspectRatio = aspect;
                if (_camera != null) _camera.fieldOfView = Mathf.Min(106f / aspect, 60f);
                if (_invrect != null)
                {
                    _invrect.anchoredPosition = Vector2.zero;
                    _invrect.anchorMax = new Vector2(0.5f, 0f);
                    _invrect.anchorMin = new Vector2(0.5f, 0.5f);
                    _invrect.pivot = new Vector2(0.5f, 0f);
                }
                if (_helmet != null)
                {
                    Vector3 scale = _helmet.localScale;
                    scale.x = 0.3628f * Mathf.Max(aspect / 2.3f, 1f);
                    _helmet.localScale = scale;
                }
            }
        }
        private static bool IsVanilla => !UWOn && Indexx == 0;
        private static void ResetAspect()
        {
            CacheReferences();
            if (!_originalsCaptured)
            {
                return;
            }

            if (_panel != null)
            {
                _panel.aspectRatio = _originalPanelAspect;
                _panel.enabled = !IsVanilla;
            }
            if (_canvas != null)
            {
                _canvas.referenceResolution = _originalCanvasRes;
            }

            HUDManager hudManager = HUDManager.Instance;
            if (hudManager != null)
            {
                if (_hud != null)
                {
                    _hud.aspectRatio = _originalHudAspect;
                }
                if (_camera != null)
                {
                    _camera.fieldOfView = _originalUICamFOV;
                }

                if (_invrect != null)
                {
                    _invrect.anchorMin = _originalInvAnchorMin;
                    _invrect.anchorMax = _originalInvAnchorMax;
                    _invrect.pivot = _originalInvPivot;
                    _invrect.anchoredPosition = _originalInvPos;
                }
                if (_helmet != null)
                {
                    Vector3 scale = _helmet.localScale;
                    scale.x = _originalHelmetX;
                    _helmet.localScale = scale;
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
        private static AspectRatioFitter? _panel = null;
        private static CanvasScaler? _canvas = null;
        private static Camera? _camera = null;
        private static RectTransform? _invrect = null;
        private static Transform? _helmet = null;
        private static Terminal? _terminal = null;
        private static AspectRatioFitter? _hud = null;

        private static void CacheReferences()
        {
            if (_panel == null)
            {
                GameObject panel = GameObject.Find("Systems/UI/Canvas/Panel");
                panel?.TryGetComponent(out _panel);
            }
            if (_canvas == null)
            {
                GameObject canvas = GameObject.Find("Systems/UI/Canvas");
                canvas?.TryGetComponent(out _canvas);
            }
            if (_camera == null)
            {
                GameObject uiCam = GameObject.Find("Systems/UI/UICamera");
                uiCam?.TryGetComponent(out _camera);
            }
            if (_helmet == null)
            {
                GameObject? helmet = GameObject.Find("PlayerHUDHelmetModel");
                if (helmet != null)
                    _helmet = helmet.transform;
            }
            if (_terminal == null)
            {
                GameObject terminalObj = GameObject.Find("TerminalScript");
                terminalObj?.TryGetComponent(out _terminal);
            }
            if (_hud == null && HUDManager.Instance != null)
            {
                HUDManager.Instance.HUDContainer
                    ?.TryGetComponent(out _hud);
            }
            if (_invrect == null && HUDManager.Instance != null)
            {
                _invrect = HUDManager.Instance.Inventory?.canvasGroup?.GetComponent<RectTransform>();
            }
        }

        private static void SaveOrigs()
        {
            if (_originalsCaptured) return;

            CacheReferences();

            if (_panel != null)
                _originalPanelAspect = _panel.aspectRatio;

            if (_canvas != null)
                _originalCanvasRes = _canvas.referenceResolution;

            if (_hud != null)
                _originalHudAspect = _hud.aspectRatio;

            if (_camera != null)
                _originalUICamFOV = _camera.fieldOfView;

            if (_helmet != null)
                _originalHelmetX = _helmet.localScale.x;

            if (_invrect != null)
            {
                _originalInvAnchorMin = _invrect.anchorMin;
                _originalInvAnchorMax = _invrect.anchorMax;
                _originalInvPivot = _invrect.pivot;
                _originalInvPos = _invrect.anchoredPosition;
            }

            _originalsCaptured = _panel != null;
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

            Transform? container = null;
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

            RectTransform? pixelResRect = container.Find("PixelRes")?.GetComponent<RectTransform>();
            RectTransform rootRect = root.AddComponent<RectTransform>();
            Image rootBg = root.AddComponent<Image>();
            GameObject? pixelResObj = container.Find("PixelRes")?.gameObject;
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

            if (newboxsize == null)
            {
                Transform[] allT = UnityEngine.Object.FindObjectsOfType<Transform>(includeInactive: true);
                foreach (Transform t in allT)
                {
                    if (t.name == "Graphics")
                    {
                        Transform parent = t.parent;
                        if (parent == null) break;
                        bool foundGraphics = false;
                        for (int j = 0; j < parent.childCount; j++)
                        {
                            Transform child = parent.GetChild(j);
                            if (child.name == "Graphics") { foundGraphics = true; continue; }
                            if (foundGraphics && child.name.StartsWith("BoxFrame"))
                            {
                                newboxsize = child.GetComponent<RectTransform>();
                                if (newboxsize != null && pixelResRect != null)
                                {
                                    oldboxsize = newboxsize.sizeDelta;
                                    float growth = pixelResRect.sizeDelta.y + 20f;
                                    newboxsize.sizeDelta = new Vector2(
                                        oldboxsize.x,
                                        oldboxsize.y + growth
                                    );
                                    newboxsize.anchoredPosition = new Vector2(
                                        newboxsize.anchoredPosition.x,
                                        newboxsize.anchoredPosition.y - (growth / 2f)
                                    );
                                }
                                break;
                            }
                        }
                        break;
                    }
                }
            }

            GameObject label = new GameObject("Label");
            label.transform.SetParent(root.transform, worldPositionStays: false);
            RectTransform labelRect = label.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.03f, 0f);
            labelRect.anchorMax = new Vector2(0.4f, 1f);
            labelRect.sizeDelta = Vector2.zero;
            labelRect.anchoredPosition = Vector2.zero;
            TextMeshProUGUI labelText = label.AddComponent<TextMeshProUGUI>();
            labelText.text = "Resolution";
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