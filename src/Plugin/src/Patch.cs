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
				if (hand.Input.AXButtonDown)
				{
					if (hand.IsThisTheRightHand)
					{
						__instance.TurnClockWise();
					}
					else
					{
						__instance.TurnCounterClockWise();
					}
				}
				__instance.HandUpdateTwinstick(hand);
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
				var gravmode = GM.Options.SimulationOptions.PlayerGravityMode;
				GM.Options.SimulationOptions.PlayerGravityMode = SimulationOptions.GravityMode.None;
				__instance.UpdateModeTwoAxis(true);
				GM.Options.SimulationOptions.PlayerGravityMode = gravmode;
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