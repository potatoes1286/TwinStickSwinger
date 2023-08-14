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
		public static bool Patch_HandMovementUpdate(FVRMovementManager __instance, ref FVRViveHand hand)
		{
			if (__instance.Mode == FVRMovementManager.MovementMode.Dash)
			{
				//Save some states, set some necessary states
				//Prevent turning by TwinStick when pressing Armswinger buttons
				var tssts = GM.Options.MovementOptions.TwinStickSnapturnState;
				GM.Options.MovementOptions.TwinStickSnapturnState = MovementOptions.TwinStickSnapturnMode.Disabled;
				//Jumping for TwinStick is broken. I have NO fucking idea why.
				var jumpstate = GM.Options.MovementOptions.TwinStickJumpState;
				GM.Options.MovementOptions.TwinStickJumpState = MovementOptions.TwinStickJumpMode.Disabled;
				
				__instance.HandUpdateTwinstick(hand);
				
				//Re-set those states
				GM.Options.MovementOptions.TwinStickJumpState = jumpstate;
				GM.Options.MovementOptions.TwinStickSnapturnState = tssts;
				
				__instance.AXButtonCheck(hand);
				return false;
			}
			return true;
		}

		[HarmonyPatch(typeof(FVRMovementManager), "FU")]
		[HarmonyPrefix]
		public static bool Patch_MovementMathUpdate(FVRMovementManager __instance, out bool __state)
		{
			if (__instance.Mode == FVRMovementManager.MovementMode.Dash)
			{
				//Okay so this is a little fucky!
				
				//Set gravity to zero. Gravity is calculated at UpdateSmoothLocomotion, and we're running it twice, so
				//if we don't set grav to 0 we will simulate grav twice.
				var gravmode = GM.Options.SimulationOptions.PlayerGravityMode;
				GM.Options.SimulationOptions.PlayerGravityMode = SimulationOptions.GravityMode.None;
				
				//Set to twinstick and run USL. USL here wont sim grav + will sim twinstick
				__instance.Mode = FVRMovementManager.MovementMode.TwinStick;
				__instance.UpdateSmoothLocomotion();
				
				//Reset gravity.
				GM.Options.SimulationOptions.PlayerGravityMode = gravmode;
				
				//Set to armswinger.
				__instance.Mode = FVRMovementManager.MovementMode.Armswinger;
				//Inform state that we're going to reset it back to Dash
				__state = true;
			}
			else
				__state = false; //We're not Dash (TSS). Just ignore everything and move on.
			//If set to armswinger, it'll run USL again in the regular FU function, and simulate gravity properly.
			return true;
		}
		
		[HarmonyPatch(typeof(FVRMovementManager), "FU")]
		[HarmonyPostfix]
		public static void Patch_MovementMathUpdateEnd(FVRMovementManager __instance, bool __state)
		{
			//Afterwards, reset the movement style to Dash so we can repeat the entire process.
			if (__state)
				__instance.Mode = FVRMovementManager.MovementMode.Dash;
		}
		
		[HarmonyPatch(typeof(FVRPointableButton), "Awake")]
		[HarmonyPostfix]
		//this just renames the Dash in the menu to TSS (Twin Stick Swinger)
		public static void Patch_FixDashName(FVRPointableButton __instance) {
			var text = __instance.GetComponent<Text>();
			if (text != null) {
				if (text.text == "Dash Teleport")
					text.text = "Twin-Stick Swinger";
			}
		}
	}
}