using FistVR;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace H3VRMod
{
	public class Patch
	{
		[HarmonyPatch(typeof(FVRMovementManager), "UpdateMovementWithHand")]
		[HarmonyPrefix]
		public static bool Patch_TwinStickSwinger(FVRMovementManager __instance, ref FVRViveHand hand)
		{
			if (__instance.Mode == FVRMovementManager.MovementMode.Dash)
			{
				__instance.AXButtonCheck(hand);
				var tssts = GM.Options.MovementOptions.TwinStickSnapturnState;
				GM.Options.MovementOptions.TwinStickSnapturnState = MovementOptions.TwinStickSnapturnMode.Disabled;
				__instance.HandUpdateTwinstick(hand);
				GM.Options.MovementOptions.TwinStickSnapturnState = tssts;
				return false;
			}
			return true;
		}

		[HarmonyPatch(typeof(FVRMovementManager), "FU")]
		[HarmonyPrefix]
		public static bool Patch_TwinStickSwinger2(FVRMovementManager __instance, out bool __state)
		{
			if (__instance.Mode == FVRMovementManager.MovementMode.Dash)
			{
				//set gravmode to 0 so that twinstick doesn't also simulate gravity again
				var gravmode = GM.Options.SimulationOptions.PlayerGravityMode;
				GM.Options.SimulationOptions.PlayerGravityMode = SimulationOptions.GravityMode.None;
				__instance.UpdateModeTwoAxis(true);
				GM.Options.SimulationOptions.PlayerGravityMode = gravmode;
				//once it's done, set mode for armswinger and let the original method do its calcs
				__instance.Mode = FVRMovementManager.MovementMode.Armswinger;
				__state = true;
			}
			else
			{
				__state = false;
			}
			return true;
		}
		
		[HarmonyPatch(typeof(FVRMovementManager), "FU")]
		[HarmonyPostfix]
		public static void Patch_TwinStickSwinger2andAHalf(FVRMovementManager __instance, bool __state)
		{
			if (__state)
			{
				__instance.Mode = FVRMovementManager.MovementMode.Dash;
			}
		}
		
		/*[HarmonyPatch(typeof(FVRPointableButton), "Awake")]
		[HarmonyPrefix]
		public static bool Patch_FixDashName(FVRPointableButton __instance)
		{
			//WristMenu/MenuGo/Canvas/Button_LocoSet_0 (1)/Text
			if (__instance.Text.text == "Dash") __instance.Text.text = "Twinstick Swinger";
			return true;
		}*/
	}
}