using FistVR;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace H3VRMod
{
	public class Patch {

		//Hacky.
		public static bool IsTwinStickSwinger = false;
		
		[HarmonyPatch(typeof(FVRMovementManager), "UpdateMovementWithHand")]
		[HarmonyPrefix]
		public static bool Patch_HandMovementUpdate(FVRMovementManager __instance, ref FVRViveHand hand)
		{
			if (__instance.Mode == FVRMovementManager.MovementMode.Dash) {
				//Save some states, set some necessary states
				//Prevent turning by TwinStick when pressing Armswinger buttons
				var tssts = GM.Options.MovementOptions.TwinStickSnapturnState;
				GM.Options.MovementOptions.TwinStickSnapturnState = MovementOptions.TwinStickSnapturnMode.Disabled;
				//Jumping for TwinStick is broken. I have NO fucking idea why.
				//It just sorta pushes you down. To prevent this, just disable twinstick jump. You can still armswing jump.
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

		public class FUState {
			public bool IsTwinStickSwinger;
			public int  BaseSpeedRight;
			public int  BaseSpeedLeft;
		}
		
		[HarmonyPatch(typeof(FVRMovementManager), "FU")]
		[HarmonyPrefix]
		public static bool Patch_MovementMathUpdate(FVRMovementManager __instance, out FUState __state) {
			__state = new FUState();
			if (__instance.Mode == FVRMovementManager.MovementMode.Dash) {
				IsTwinStickSwinger = true;
				//Okay so this is a little fucky!
				
				//Set gravity to zero. Gravity is calculated at UpdateSmoothLocomotion, and we're running it twice, so
				//if we don't set grav to 0 we will simulate grav twice.
				var gravmode = GM.Options.SimulationOptions.PlayerGravityMode;
				GM.Options.SimulationOptions.PlayerGravityMode = SimulationOptions.GravityMode.None;
				
				
				//Set to Twinstick and run USL. USL here wont sim grav but will sim Twinstick.
				__instance.Mode = FVRMovementManager.MovementMode.TwinStick;

				__instance.UpdateSmoothLocomotion();
				
				//Reset gravity.
				GM.Options.SimulationOptions.PlayerGravityMode = gravmode;
				
				
				
				//Set to Armswinger.
				__instance.Mode = FVRMovementManager.MovementMode.Armswinger;
				
				//Set base speed of Armswinger to zero, or else the base speed of armswinger when pressing down will occur
				//bc armswinger is auto-activated whenever you press on the joystick, every time u press on the joystick itd just kinda
				//slowly move u forward
				//so dont do that
				__state.BaseSpeedRight = GM.Options.MovementOptions.ArmSwingerBaseSpeed_Right;
				__state.BaseSpeedLeft = GM.Options.MovementOptions.ArmSwingerBaseSpeed_Left;
				GM.Options.MovementOptions.ArmSwingerBaseSpeed_Right = 0;
				GM.Options.MovementOptions.ArmSwingerBaseSpeed_Left = 0;

				
				//If walking via twinstick is activated, also activate armswinger via manipulating inputs.
				//Fucky, i know! But it works and it's easy. Fight me.
				if (__instance.Hands[0].Input.Secondary2AxisInputAxes.magnitude > 0.9
				 && !__instance.Hands[0].Input.Secondary2AxisSouthPressed) { //Prevent armswinger causing fuckery when trying to pedal back.
					__instance.Hands[0].Input.BYButtonPressed = true;
					__instance.Hands[1].Input.BYButtonPressed = true;
				}
				//Inform state that we're going to reset it back to Dash
				__state.IsTwinStickSwinger = true;
			}
			else {
				__state.IsTwinStickSwinger = false;
				IsTwinStickSwinger = false;
				//I think i'm repeating myself. Fix this later, i guess
			} //We're not Dash (TSS). Just ignore everything and move on.

			//If set to armswinger, it'll run USL again in the regular FU function, and simulate gravity properly.
			return true;
		}
		
		[HarmonyPatch(typeof(FVRMovementManager), "FU")]
		[HarmonyPostfix]
		public static void Patch_MovementMathUpdateEnd(FVRMovementManager __instance, FUState __state)
		{
			//Afterwards, reset the movement style to Dash so we can repeat the entire process.
			if (__state.IsTwinStickSwinger) {
				__instance.Mode = FVRMovementManager.MovementMode.Dash;
				//Reset base speed.
				GM.Options.MovementOptions.ArmSwingerBaseSpeed_Right = __state.BaseSpeedRight;
				GM.Options.MovementOptions.ArmSwingerBaseSpeed_Left = __state.BaseSpeedLeft;
			}
		}
		
		//Bafflingly, for some random ass reason, jumping in TwinStickSwinger gives you inhumane strength.
		//Like, you jump twice the height as regular armswinger.
		//i have no fucking clue why.
		[HarmonyPatch(typeof(FVRMovementManager), "Jump")]
		[HarmonyPrefix]
		public static bool Patch_JumpingTooFuckingMuch(FVRMovementManager __instance)
		{
			if ((__instance.Mode == FVRMovementManager.MovementMode.Armswinger
			  || __instance.Mode == FVRMovementManager.MovementMode.SingleTwoAxis
			  || __instance.Mode == FVRMovementManager.MovementMode.TwinStick)
			 && !__instance.m_isGrounded)
				return false;

			__instance.DelayGround(0.1f);
			float num = 0f;
			switch (GM.Options.SimulationOptions.PlayerGravityMode)
			{
				case SimulationOptions.GravityMode.Realistic:
					num = 7.1f;
					break;
				case SimulationOptions.GravityMode.Playful:
					num = 5f;
					break;
				case SimulationOptions.GravityMode.OnTheMoon:
					num = 3f;
					break;
				case SimulationOptions.GravityMode.None:
					num = 0.001f;
					break;
			}
			num *= 0.65f;
			if (IsTwinStickSwinger) {
				num *= 0.7f;
			}
			if (__instance.Mode == FVRMovementManager.MovementMode.Armswinger
			 || __instance.Mode == FVRMovementManager.MovementMode.SingleTwoAxis
			 || __instance.Mode == FVRMovementManager.MovementMode.TwinStick)
			{
				__instance.DelayGround(0.25f);
				__instance.m_smoothLocoVelocity.y = Mathf.Clamp(__instance.m_smoothLocoVelocity.y, 0f, __instance.m_smoothLocoVelocity.y);
				__instance.m_smoothLocoVelocity.y = num;
				__instance.m_isGrounded = false;
			}

			return false;
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