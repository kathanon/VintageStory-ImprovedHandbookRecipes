using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace ImprovedHandbookRecipes;
public class ModSys : ModSystem {
    private const string ID = "improvedhandbookrecipes";

    private Harmony harmony;

    public override void StartPre(ICoreAPI api) 
        => (harmony ??= new Harmony(ID)).PatchAll();

    public override void Dispose() 
        => harmony?.UnpatchAll(ID);

    public override void StartClientSide(ICoreClientAPI api) {
        Handbook_Patch.SetAPI(api);

        Textures.Load(api);
    }

}
