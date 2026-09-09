using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;

namespace NeonMenu.Patches;

public static class TypewriterOverlay
{
    public static float SecondsPerCharacter = 0.05f;
    public static float DelayBeforeCloseButton = 2f;
    public static Color BackgroundColor = new Color(0f, 0f, 0f, 0.92f);
    public static Color TextColor = Color.white;

    private const float TextBoxWidthFraction = 0.90f;
    private const float TextBoxHeightFraction = 0.70f;
    private const float FontSizeMinFraction = 0.20f;
    private const float FontSizeMaxFraction = 0.45f;

    private static Sprite cachedSolidSprite;

    public static void Show(MainMenuManager mainMenuManager, string message, TextMeshPro fontSource)
    {
        try
        {
            var camera = Camera.main;
            if (camera == null)
            {
                NeonMenuPlugin.Logger.LogError("no Camera.main found!");
                return;
            }

            float visibleHeight = camera.orthographicSize * 2f;
            float visibleWidth = visibleHeight * camera.aspect;

            SetMainMenuInteractable(mainMenuManager, false);

            Vector3 forward = camera.transform.forward;
            Vector3 backgroundPosition = camera.transform.position + forward * 5f;
            Vector3 textPosition = camera.transform.position + forward * 4.9f;
            var closePosition = camera.transform.position + forward * 4.8f - camera.transform.up * (visibleHeight * 0.32f);

            var background = new GameObject("NeonMenu_Overlay_Background");
            background.transform.position = backgroundPosition;
            background.transform.rotation = camera.transform.rotation;
            var spriteRenderer = background.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = GetSolidSprite();
            spriteRenderer.color = BackgroundColor;
            spriteRenderer.sortingOrder = 32000;
            background.AddComponent<FullScreenScaler>();

            var text = new GameObject("NeonMenu_Overlay_Text");
            text.transform.position = textPosition;
            text.transform.rotation = camera.transform.rotation;
            var tmp = text.AddComponent<TextMeshPro>();
            if (fontSource != null)
            {
                tmp.font = fontSource.font;
                tmp.fontSharedMaterial = fontSource.fontSharedMaterial;
            }

            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = true;
            tmp.color = TextColor;
            tmp.rectTransform.sizeDelta = new Vector2(visibleWidth * TextBoxWidthFraction, visibleHeight * TextBoxHeightFraction);

            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = visibleHeight * FontSizeMinFraction;
            tmp.fontSizeMax = visibleHeight * FontSizeMaxFraction;
            tmp.text = message;
            tmp.maxVisibleCharacters = 0;
            var textRenderer = tmp.GetComponent<Renderer>();
            if (textRenderer != null) textRenderer.sortingOrder = 32001;

            var closeButton = BuildCloseButton(closePosition, camera.transform.rotation, tmp, background, text, visibleHeight, mainMenuManager);
            closeButton.SetActive(false);

            RegisterInIl2Cpp();
            var driver = text.AddComponent<TypewriterDriver>();
            driver.textArea = tmp;
            driver.closeButton = closeButton;
            driver.secondsPerCharacter = SecondsPerCharacter;
            driver.delayBeforeCloseButton = DelayBeforeCloseButton;
            driver.Begin();
        }
        catch (Exception ex)
        {
            NeonMenuPlugin.Logger.LogError($"exception in TypewriterOverlay.Show: {ex}");
        }
    }

    private static bool il2cppRegistered;

    private static void RegisterInIl2Cpp()
    {
        if (il2cppRegistered) return;
        il2cppRegistered = true;

        try
        {
            ClassInjector.RegisterTypeInIl2Cpp<TypewriterDriver>();
        }
        catch (Exception ex)
        {
            NeonMenuPlugin.Logger.LogError($"failed to register the driver in IL2CPP: {ex}");
        }
    }

    private static void SetMainMenuInteractable(MainMenuManager mainMenuManager, bool interactable)
    {
        if (mainMenuManager == null) return;

        try
        {
            string methodName = interactable ? "ActivateMainMenuUI" : "DeactivateMainMenuUI";
            var method = AccessTools.Method(typeof(MainMenuManager), methodName);
            if (method == null)
            {
                NeonMenuPlugin.Logger.LogError($"can't find MainMenuManager.{methodName}");
                return;
            }

            method.Invoke(mainMenuManager, null);
        }
        catch (Exception ex)
        {
            NeonMenuPlugin.Logger.LogError($"exception when setting main menu interactivity: {ex}");
        }
    }

    private static GameObject BuildCloseButton(Vector3 position, Quaternion rotation, TextMeshPro fontSource, GameObject backgroundGameObject, GameObject textGameObject, float visibleHeight, MainMenuManager mainMenuManager)
    {
        var close = new GameObject("NeonMenu_Overlay_CloseButton");
        close.transform.position = position;
        close.transform.rotation = rotation;

        var closeText = close.AddComponent<TextMeshPro>();
        closeText.font = fontSource.font;
        closeText.fontSharedMaterial = fontSource.fontSharedMaterial;
        closeText.text = "Close";
        closeText.alignment = TextAlignmentOptions.Center;
        closeText.color = TextColor;
        closeText.fontSize = visibleHeight * 0.45f;
        var closeRenderer = closeText.GetComponent<Renderer>();
        if (closeRenderer != null) closeRenderer.sortingOrder = 32002;

        var collider = close.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(visibleHeight * 0.85f, visibleHeight * 0.40f);

        var passive = close.AddComponent<PassiveButton>();
        passive.OnClick = new Button.ButtonClickedEvent();
        passive.OnClick.AddListener((Action)(() =>
        {
            try
            {
                SetMainMenuInteractable(mainMenuManager, true);
                UnityEngine.Object.Destroy(backgroundGameObject);
                UnityEngine.Object.Destroy(textGameObject);
                UnityEngine.Object.Destroy(close);
            }
            catch (Exception ex)
            {
                NeonMenuPlugin.Logger.LogError($"exception when closing overlay: {ex}");
            }
        }));

        return close;
    }

    private static Sprite GetSolidSprite()
    {
        if (cachedSolidSprite != null) return cachedSolidSprite;

        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        cachedSolidSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return cachedSolidSprite;
    }
}

public class TypewriterDriver : MonoBehaviour
{
    public TextMeshPro textArea;
    public GameObject closeButton;
    public float secondsPerCharacter = 0.06f;
    public float delayBeforeCloseButton = 2f;

    private int totalChars;
    private int shown;
    private float charTimer;
    private float postDelayTimer;
    private bool typingDone;
    private bool started;

    public void Begin()
    {
        try
        {
            textArea.ForceMeshUpdate();
            totalChars = textArea.textInfo.characterCount;
            textArea.maxVisibleCharacters = 0;
            shown = 0;
            charTimer = 0f;
            postDelayTimer = 0f;
            typingDone = totalChars <= 0;
            started = true;
        }
        catch (System.Exception ex)
        {
            NeonMenuPlugin.Logger.LogError($"exception in TypewriterDriver.Begin: {ex}");
            typingDone = true;
            started = true;
        }
    }

    private void Update()
    {
        if (!started) return;

        try
        {
            if (!typingDone)
            {
                charTimer += Time.deltaTime;
                while (charTimer >= secondsPerCharacter && shown < totalChars)
                {
                    charTimer -= secondsPerCharacter;
                    shown++;
                    textArea.maxVisibleCharacters = shown;
                }

                if (shown >= totalChars)
                {
                    typingDone = true;
                    postDelayTimer = 0f;
                }

                return;
            }

            if (closeButton != null && !closeButton.activeSelf)
            {
                postDelayTimer += Time.deltaTime;
                if (postDelayTimer >= delayBeforeCloseButton)
                {
                    closeButton.SetActive(true);
                }
            }
        }
        catch (System.Exception ex)
        {
            NeonMenuPlugin.Logger.LogError($"exception in TypewriterDriver.Update: {ex}");
            started = false;
        }
    }
}