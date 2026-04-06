using HarmonyLib;
using GameNetcodeStuff;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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

        private static int index = -1;
        private static int Indexx
        {
            get
            {
                if (index == -1)
                    index = Mathf.Clamp(Plugin.ResValue.Value, 0, Presets.Length - 1);
                return index;
            }
            set => index = Mathf.Clamp(value, 0, Presets.Length - 1);
        }

        private static void applyres(RenderTexture rt, int index)
        {
            if (rt == null) return;
            var (w, h, _) = Presets[index];
            if (rt.width == w && rt.height == h) return;
            rt.Release();
            rt.width = w;
            rt.height = h;
        }

        [HarmonyPatch(typeof(PlayerControllerB), "Start")]
        [HarmonyPostfix]
        private static void pcb(PlayerControllerB __instance)
        {
            if (__instance.gameplayCamera?.targetTexture == null) return;
            applyres(__instance.gameplayCamera.targetTexture, Indexx);
        }

        [HarmonyPatch(typeof(IngamePlayerSettings), "SetPixelResolution")]
        [HarmonyPostfix]
        private static void setres(IngamePlayerSettings __instance)
        {
            if (__instance.playerGameplayScreenTex == null) return;
            applyres(__instance.playerGameplayScreenTex, Indexx);
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
            Plugin.ResValue.Value = Indexx;
            ApplyRes();
        }

        [HarmonyPatch(typeof(IngamePlayerSettings), "DiscardChangedSettings")]
        [HarmonyPostfix]
        private static void discard()
        {
            Indexx = Mathf.Clamp(Plugin.ResValue.Value, 0, Presets.Length - 1);
            Refreshelement();
        }

        private static void Injectbutton(IngamePlayerSettings instance)
        {
            Reselement existing = Object.FindObjectOfType<Reselement>(includeInactive: true);
            if (existing != null)
            {
                Transform valueTransform = existing.transform.Find("Value");
                if (valueTransform != null)
                {
                    TextMeshProUGUI existingValueText = valueTransform.GetComponent<TextMeshProUGUI>();
                    if (existingValueText != null)
                        existingValueText.text = Presets[Indexx].label;
                }
                Plugin.Logger.LogInfo("Resolution element injected...");
                return;
            }

            SettingsOption[] bruh = Object.FindObjectsOfType<SettingsOption>(includeInactive: true);
            if (bruh == null || bruh.Length == 0) return;

            Transform container = null;
            Transform[] allTransforms = Object.FindObjectsOfType<Transform>(includeInactive: true);
            foreach (Transform t in allTransforms)
            {
                if (t.name == "SettingsPanel" && t.parent?.name == "QuickMenu")
                {
                    container = t;
                    break;
                }
            }

            if (container == null)
            {
                container = bruh[bruh.Length - 1].transform.parent;
            }

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
            labelRect.anchorMin = new Vector2(0f, 0f);
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
            leftRect.anchorMin = new Vector2(0.4f, 0.1f);
            leftRect.anchorMax = new Vector2(0.52f, 0.9f);
            leftRect.sizeDelta = Vector2.zero;
            leftRect.anchoredPosition = Vector2.zero;
            Image leftImg = lbutton.AddComponent<Image>();
            leftImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            Button leftButton = lbutton.AddComponent<Button>();
            GameObject leftLabel = new GameObject("Text");
            leftLabel.transform.SetParent(lbutton.transform, worldPositionStays: false);
            RectTransform leftLabelRect = leftLabel.AddComponent<RectTransform>();
            leftLabelRect.anchorMin = Vector2.zero;
            leftLabelRect.anchorMax = Vector2.one;
            leftLabelRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI leftText = leftLabel.AddComponent<TextMeshProUGUI>();
            leftText.text = "<";
            leftText.fontSize = 16f;
            leftText.alignment = TextAlignmentOptions.Center;
            font(leftText, bruh);

            GameObject value = new GameObject("Value");
            value.transform.SetParent(root.transform, worldPositionStays: false);
            RectTransform valueRect = value.AddComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0.52f, 0f);
            valueRect.anchorMax = new Vector2(0.76f, 1f);
            valueRect.sizeDelta = Vector2.zero;
            valueRect.anchoredPosition = Vector2.zero;
            TextMeshProUGUI valueText = value.AddComponent<TextMeshProUGUI>();
            valueText.text = Presets[Indexx].label;
            valueText.fontSize = 11f;
            valueText.alignment = TextAlignmentOptions.Center;
            font(valueText, bruh);

            GameObject rbutton = new GameObject("ButtonRight");
            rbutton.transform.SetParent(root.transform, worldPositionStays: false);
            RectTransform rightRect = rbutton.AddComponent<RectTransform>();
            rightRect.anchorMin = new Vector2(0.76f, 0.1f);
            rightRect.anchorMax = new Vector2(0.88f, 0.9f);
            rightRect.sizeDelta = Vector2.zero;
            rightRect.anchoredPosition = Vector2.zero;
            Image rightImg = rbutton.AddComponent<Image>();
            rightImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            Button rightButton = rbutton.AddComponent<Button>();
            GameObject rightLabel = new GameObject("Text");
            rightLabel.transform.SetParent(rbutton.transform, worldPositionStays: false);
            RectTransform rightLabelRect = rightLabel.AddComponent<RectTransform>();
            rightLabelRect.anchorMin = Vector2.zero;
            rightLabelRect.anchorMax = Vector2.one;
            rightLabelRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI rightText = rightLabel.AddComponent<TextMeshProUGUI>();
            rightText.text = ">";
            rightText.fontSize = 16f;
            rightText.alignment = TextAlignmentOptions.Center;
            font(rightText, bruh);

            leftButton.onClick.AddListener(() =>
            {
                Indexx = (Indexx - 1 + Presets.Length) % Presets.Length;
                valueText.text = Presets[Indexx].label;
                IngamePlayerSettings.Instance.changesNotApplied = true;
            });

            rightButton.onClick.AddListener(() =>
            {
                Indexx = (Indexx + 1) % Presets.Length;
                valueText.text = Presets[Indexx].label;
                IngamePlayerSettings.Instance.changesNotApplied = true;
            });
        }

        private static void Refreshelement()
        {
            Reselement marker = Object.FindObjectOfType<Reselement>(includeInactive: true);
            if (marker == null) return;
            Transform valueTransform = marker.transform.Find("Value");
            if (valueTransform == null) return;
            TextMeshProUGUI valueText = valueTransform.GetComponent<TextMeshProUGUI>();
            if (valueText == null) return;
            valueText.text = Presets[Indexx].label;
        }

        private static void ApplyRes()
        {
            if (IngamePlayerSettings.Instance?.playerGameplayScreenTex != null)
                applyres(IngamePlayerSettings.Instance.playerGameplayScreenTex, Indexx);

            PlayerControllerB[] players = Object.FindObjectsOfType<PlayerControllerB>();
            foreach (var p in players)
            {
                if (p.gameplayCamera?.targetTexture != null)
                    applyres(p.gameplayCamera.targetTexture, Indexx);
            }
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
    }

    internal class Reselement : MonoBehaviour { }
}