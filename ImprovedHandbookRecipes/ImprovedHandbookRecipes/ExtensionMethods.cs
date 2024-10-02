using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;

namespace ImprovedHandbookRecipes;
public static class ExtensionMethods {
    private static int[] shift = { (int) GlKeys.ShiftLeft,   (int) GlKeys.ShiftRight };
    private static int[] ctrl  = { (int) GlKeys.ControlLeft, (int) GlKeys.ControlRight };
    private static int[] alt   = { (int) GlKeys.AltLeft,     (int) GlKeys.AltRight };

    public static bool HasCraftingGridOpened(this ICoreClientAPI api)
        => api.Gui.OpenedGuis.OfType<GuiDialogInventory>().Any();

    public static IEnumerable<ItemSlot> NonEmpty(this IEnumerable<ItemSlot> self)
        => self.Where(x => !x.Empty);

    public static bool ShiftHeld(this IInputAPI input)
        => shift.Any(key => input.KeyboardKeyStateRaw[key]);

    public static bool CtrlHeld(this IInputAPI input)
        => ctrl.Any(key => input.KeyboardKeyStateRaw[key]);

    public static bool AltHeld(this IInputAPI input)
        => alt.Any(key => input.KeyboardKeyStateRaw[key]);
}
