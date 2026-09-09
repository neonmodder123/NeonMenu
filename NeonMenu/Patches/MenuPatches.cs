using System;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NeonMenu.Patches;

[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
public static class MainMenuPatch
{
    private const string Explanation = """
                                       Hello! So you're probably wondering why this didn't open the menu. Well, to be honest, it was a lie. This mod was made to spread awareness that cheats and hacks will not be coming to Starlight, and neither would custom DLLs. Cheating is honestly bad, and I get why you would want to cheat: it would be funny knowing where everyone is in Hide and Seek, getting Impostor every time, trolling other people and much more. However, all of those violate the Among Us Modding Policy, and Innersloth is kind enough to still partially support modding in the first place, so we are kindly asking you to refrain from questions related to cheats and if they will be added to Starlight, and to not bring hate to this mod as I simply wanted to spread awareness. 
                                       Thank you for reading this :)
                                       -neon
                                       """;

    [HarmonyPostfix]
    private static void StartPatch(MainMenuManager __instance)
    {
        try
        {
            if (__instance.newsButton == null) return;

            var neonButton = UnityEngine.Object.Instantiate(__instance.newsButton, null);
            neonButton.name = "NeonMenuToggle";

            var newsCount = neonButton.GetComponent<NewsCountButton>();
            if (newsCount != null)
            {
                newsCount.enabled = false;
                UnityEngine.Object.Destroy(newsCount);
            }

            neonButton.transform.localScale = new Vector3(0.44f, 0.84f, 1f);

            var passive = neonButton.GetComponent<PassiveButton>();
            if (passive == null)
            {
                passive = neonButton.gameObject.AddComponent<PassiveButton>();
            }

            passive.OnClick = new Button.ButtonClickedEvent();
            passive.OnClick.AddListener((Action)(() =>
            {
                var text = neonButton.transform.GetChild(0).GetChild(0).GetComponent<TextMeshPro>();

                TypewriterOverlay.Show(__instance, Explanation, text);

                if (text != null)
                {
                    text.text = "Open Menu";
                }
            }));

            var rightPanel = GameObject.Find("RightPanel");
            if (rightPanel != null)
            {
                neonButton.gameObject.transform.SetParent(rightPanel.transform);
            }
            else
            {
                neonButton.gameObject.transform.SetParent(__instance.transform);
            }

            var position = neonButton.gameObject.AddComponent<AspectPosition>();
            position.Alignment = AspectPosition.EdgeAlignments.LeftBottom;
            position.DistanceFromEdge = new Vector3(2.1f, 2.4f, 8f);

            var textComp = neonButton.transform.GetChild(0).GetChild(0).GetComponent<TextMeshPro>();
            if (textComp != null)
            {
                __instance.StartCoroutine(Effects.Lerp(0.1f, (Action<float>)(p =>
                {
                    textComp.text = "Open Menu";
                    position.AdjustPosition();
                })));
            }

            neonButton.transform.GetChild(0).transform.localScale = new Vector3(neonButton.transform.localScale.x + 1f, 1f, 1f);
            neonButton.transform.GetChild(0).transform.localPosition += new Vector3(-1.5f, 0f, 0f);

            try
            {
                var child1 = neonButton.transform.GetChild(1).GetChild(0).GetComponent<SpriteRenderer>();
                if (child1 != null) child1.sprite = null;
            }
            catch
            {
            }

            try
            {
                var child2 = neonButton.transform.GetChild(2).GetChild(0).GetComponent<SpriteRenderer>();
                if (child2 != null) child2.sprite = null;
            }
            catch
            {
            }

            if (neonButton.transform.childCount > 3)
            {
                try
                {
                    UnityEngine.Object.Destroy(neonButton.transform.GetChild(3).gameObject);
                }
                catch
                {
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Log($"exception while adding Neon button: {ex}");
        }
    }
}