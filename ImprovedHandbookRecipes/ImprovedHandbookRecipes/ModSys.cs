using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace ImprovedHandbookRecipes;
public class ModSys : ModSystem {

    public override void StartClientSide(ICoreClientAPI api) {
        Handbook_Patch.SetAPI(api);

        new Harmony("improvedhandbookrecipes").PatchAll();
        
        Textures.Load(api);
    }

}
