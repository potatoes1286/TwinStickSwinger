using BepInEx;
using HarmonyLib;

namespace H3VRMod
{
	[BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
	[BepInProcess("h3vr.exe")]
	public class Plugin : BaseUnityPlugin
	{
		public void Start()
		{
			Harmony.CreateAndPatchAll(typeof(Patch));
		}
	}
}