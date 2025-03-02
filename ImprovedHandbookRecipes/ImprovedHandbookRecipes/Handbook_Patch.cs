using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace ImprovedHandbookRecipes;
[HarmonyPatch]
public static class Handbook_Patch {
    private static GridRecipeIngredient current = null;
    private static bool interceptScroll = false;
    private static int indexChange = 0;
    private static ICoreClientAPI api;
    private static CollectibleObject currentObject = null;
    private static int durabilityCost = 0;
    private static bool mouseMoveLast = false;

    public static void SetAPI(ICoreClientAPI api)
        => Handbook_Patch.api = api;


    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SlideshowGridRecipeTextComponent), nameof(SlideshowGridRecipeTextComponent.RenderInteractiveElements))]
    public static IEnumerable<CodeInstruction> SlideshowGrid_Transpiler(IEnumerable<CodeInstruction> original) 
        => Slideshow_Transpiler(original, typeof(SlideshowGridRecipeTextComponent));

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SlideshowItemstackTextComponent), nameof(SlideshowItemstackTextComponent.RenderInteractiveElements))]
    public static IEnumerable<CodeInstruction> SlideshowStack_Transpiler(IEnumerable<CodeInstruction> original) 
        => Slideshow_Transpiler(original, typeof(SlideshowItemstackTextComponent));

    private static IEnumerable<CodeInstruction> Slideshow_Transpiler(IEnumerable<CodeInstruction> original, Type type) {
        var getElement = AccessTools.Method(typeof(GridRecipe),
                                            nameof(GridRecipe.GetElementInGrid),
                                            generics: new Type[] { typeof(GridRecipeIngredient) });
        var pointInside = AccessTools.Method(typeof(Rectangled),
                                             nameof(Rectangled.PointInside));
        bool isGrid = type == typeof(SlideshowGridRecipeTextComponent);
        bool lookElement = isGrid, lookPointInside = true;
        string secondName = isGrid ? "secondCounter" : "curItemIndex";
        current = null;

        foreach (var instruction in original) {
            yield return instruction;

            if (lookElement && instruction.Calls(getElement)) {
                yield return CodeInstruction.StoreField(typeof(Handbook_Patch), nameof(current));
                yield return CodeInstruction.LoadField(typeof(Handbook_Patch), nameof(current));
                lookElement = false;
            }
            if (lookPointInside && instruction.Calls(pointInside)) {
                yield return new(OpCodes.Dup);
                yield return new(OpCodes.Ldarg_0);
                yield return CodeInstruction.LoadField(type, "curItemIndex", true);
                yield return new(OpCodes.Ldarg_0);
                yield return CodeInstruction.LoadField(type, secondName, true);
                yield return new(OpCodes.Ldarg_0);
                yield return CodeInstruction.Call(typeof(Handbook_Patch), nameof(UpdateIndex));
                lookPointInside = false;
            }
            if (isGrid && instruction.opcode == OpCodes.Ret) {
                yield return new(OpCodes.Ldnull);
                yield return CodeInstruction.StoreField(typeof(Handbook_Patch), nameof(current));
                lookElement = false;
            }
        }
    }

    public static void UpdateIndex(bool mouseOver, ref int index, ref int counter, ItemstackComponentBase component) {
        if (!mouseOver) return;
        int length;
        GridRecipeAndUnnamedIngredients[] list = null;
        if (component is SlideshowGridRecipeTextComponent grid) {
            list = grid.GridRecipesAndUnIn;
            length = list.Length;
        } else {
            length = (component as SlideshowItemstackTextComponent).Itemstacks.Length;
        }
        if (length <= 1 && !(list?.Any(AnyUnnamed) ?? false)) return;

        interceptScroll = mouseMoveLast;
        if (indexChange != 0) {
            index = (index + indexChange + length) % length;
            if (list != null) {
                UpdateSecondCounter(index, ref counter, list);
            }
            indexChange = 0;
        }


        static bool AnyUnnamed(GridRecipeAndUnnamedIngredients x)
            => x.unnamedIngredients?.Any(y => y.Value.Length > 1) ?? false;
    }

    private static void UpdateSecondCounter(int index, ref int counter, GridRecipeAndUnnamedIngredients[] list) {
        counter += indexChange;

        if (counter < 0) {
            var ingredient = (index >= 0 && index < list.Length) ? list[index].unnamedIngredients : null;
            if (ingredient == null) {
                counter = 0;
                return;
            }
            int[] lengths = ingredient.Values
                    .Select(x => x.Length)
                    .ToArray();
            bool[] use = new bool[lengths.Length];
            for (int i = 0; i < lengths.Length; i++) {
                use[i] = true;
                for (int j = 0; j < i; j++) {
                    if (!use[j]) continue;
                    if (lengths[j] % lengths[i] == 0) {
                        use[i] = false;
                        break;
                    } else if (lengths[i] % lengths[j] == 0) {
                        use[j] = false;
                    }
                }
            }
            int prod = lengths
                .Where((_, i) => use[i])
                .Aggregate((x, y) => x * y);
            while (counter < 0) {
                counter += prod;
            }
        }
    }


    [HarmonyPrefix]
    [HarmonyPatch(typeof(GuiDialog), nameof(GuiDialog.OnMouseWheel))]
    public static void OnMouseWheel(MouseWheelEventArgs args, GuiDialog __instance) { 
        if (__instance is GuiDialogHandbook) {
            if (interceptScroll) {
                indexChange += -Math.Sign(args.delta);
                args.SetHandled();
            } else {
                mouseMoveLast = false;

            }
        }
    }


    public static GuiDialogHandbook dialog;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GuiDialog), nameof(GuiDialog.OnMouseMove))]
    public static void OnMouseMove(GuiDialog __instance) { 
        if (__instance is GuiDialogHandbook handbook) {
            dialog = handbook;
            mouseMoveLast = true;
        }
    }


    [HarmonyPrefix]
    [HarmonyPatch(typeof(GuiDialogHandbook), nameof(GuiDialogHandbook.OnRenderGUI))]
    public static void OnRenderGUI() { 
        interceptScroll = false;
        currentObject = null;
        durabilityCost = 0;
    }


    [HarmonyPostfix]
    [HarmonyPatch(typeof(CollectibleObject), nameof(CollectibleObject.OnHandbookRecipeRender))]
    public static void OnHandbookRecipeRender(double x, double y, double size, CollectibleObject __instance) {
        if (current?.IsTool ?? false) {
            var texture = Textures.Wrench.Tex;
            float width = (float) (size * 0.4);
            float height = width / texture.Width * texture.Height;
            double margin = size * 0.05;
            float xpos = (float) (x - size / 2 + margin);
            float ypos = (float) (y - size / 2 + margin);
            api.Render.Render2DTexture(texture.TextureId, xpos, ypos, width, height, 110.0f);

            currentObject = __instance;
            durabilityCost = current.ToolDurabilityCost;
        }
    }


    [HarmonyPostfix]
    [HarmonyPatch(typeof(CollectibleObject), nameof(CollectibleObject.GetHeldItemInfo))]
    public static void GetHeldItemInfo_post(StringBuilder dsc, CollectibleObject __instance) {
        if (!__instance.IsLiquid()) {
            dsc.Append(Lang.Get("improvedhandbookrecipes:stackSize", __instance.MaxStackSize));
        }
        if (durabilityCost > 0 && ReferenceEquals(__instance, currentObject)) {
            dsc.Append(Lang.Get("improvedhandbookrecipes:durability", durabilityCost));
        }
    }


    [HarmonyPostfix]
    [HarmonyPatch(typeof(GuiDialogInventory), nameof(GuiDialogInventory.OnGuiOpened))]
    public static void OnOpened() { 
        FillGridButton.inventoryOpen = true;

    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GuiDialogInventory), nameof(GuiDialogInventory.OnGuiClosed))]
    public static void OnClosed() { 
        FillGridButton.inventoryOpen = false;
    }


    private static readonly RichTextComponentBase[] buttons = new RichTextComponentBase[2];

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CollectibleBehaviorHandbookTextAndExtraInfo), "addCreatedByInfo")]
    public static void CreatedByInfo(List<RichTextComponentBase> components) {
        if (components == null) return;
        for (int i = 0; i < components.Count; i++) {
            var component = components[i];
            if (component is SlideshowGridRecipeTextComponent prev) {
                var recipes = prev.GridRecipesAndUnIn;
                buttons[0] = new FillGridButton(api, false, recipes);
                buttons[1] = new FillGridButton(api, true,  recipes);
                components.InsertRange(i + 1, buttons);
            }
        }
    }
}
