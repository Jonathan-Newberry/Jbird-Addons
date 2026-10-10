using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent;

namespace Jbird_testmod.Content.Projectiles
{
	// This Example show how to implement simple homing projectile
	// Can be tested with ExampleCustomAmmoGun
	public class TestTomeProjectile : ModProjectile
	{
		// Store the target NPC using Projectile.ai[0]
		private NPC HomingTarget {
			get => Projectile.ai[0] == 0 ? null : Main.npc[(int)Projectile.ai[0] - 1];
			set {
				Projectile.ai[0] = value == null ? 0 : value.whoAmI + 1;
			}
		}

        

		public ref float DelayTimer => ref Projectile.ai[1];
		private NPC[] invalidTargets = {null, null, null};
		/* Checks if target is in invalidTargets array
		* Returns true if the target has been hit, otherwise false. 
		*/

		/*
		public bool hasNotBeenHit(NPC target) {
				
				for (int i =0; i < 3; i++) {

					Main.NewText(invalidTargets[i].ToString(), new Color(255, 255, 255));
					
					if (invalidTargets[i].whoAmI == target.whoAmI) {
						Main.NewText("Boolean", new Color(255, 255, 255));
						return false;
					}
					else {
						Main.NewText("BadBoolean", new Color(255, 255, 255));
					}

				}
			return true;
		} */

		public override void SetStaticDefaults() {
			ProjectileID.Sets.CultistIsResistantTo[Type] = true; // Make the cultist resistant to this projectile, as it's resistant to all homing projectiles.
		}

		public override void SetDefaults() {
			Projectile.width = 8; // The width of projectile hitbox
			Projectile.height = 8; // The height of projectile hitbox

			Projectile.DamageType = DamageClass.Magic; // What type of damage does this projectile affect?
			Projectile.friendly = true; // Can the projectile deal damage to enemies?
			Projectile.hostile = false; // Can the projectile deal damage to the player?
			Projectile.ignoreWater = true; // Does the projectile's speed be influenced by water?
			Projectile.light = 1f; // How much light emit around the projectile
			Projectile.timeLeft = 150; // The live time for the projectile (60 = 1 second, so 600 is 10 seconds)
            //Makes projectile piercing
            Projectile.penetrate = 3;
			
			//Makes it so the projectile only hits each enemy once
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
			//For animation
			ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5; // The length of old position to be recorded
			ProjectileID.Sets.TrailingMode[Projectile.type] = 0; // The recording mode
		}

		//From ExampleBullet: 
		public override bool PreDraw(ref Color lightColor) {
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			Vector2 drawOrigin = new Vector2(texture.Width * 0.5f, Projectile.height * 0.5f);
			for (int k = Projectile.oldPos.Length - 1; k > 0; k--) {
				Vector2 drawPos = (Projectile.oldPos[k] - Main.screenPosition) + drawOrigin + new Vector2(0f, Projectile.gfxOffY);
				Color color = Projectile.GetAlpha(lightColor) * ((Projectile.oldPos.Length - k) / (float)Projectile.oldPos.Length);
				Main.EntitySpriteDraw(texture, drawPos, null, color, Projectile.rotation, drawOrigin, Projectile.scale, SpriteEffects.None, 0);
			}

			return true;
		}


        // From ExamplePiercingProjectile
		//public bool hasBeenHit = false;
		
		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			
			// 3a: target.immune[Projectile.owner] = 20;
			// 3b: target.immune[Projectile.owner] = 5;
			
			
			target.AddBuff(BuffID.OnFire, 100);
			
			for (int i =0; i < 3; i++) {
					
					if (invalidTargets[i] == null && !invalidTargets.Contains(target))
					{
						invalidTargets[i] = target;
					}
			}
			
        }
		
		// Custom AI
		public override void AI() {
			float maxDetectRadius = 400f; // The maximum radius at which a projectile can detect a target

			// A short delay to homing behavior after being fired
			/*
			if (DelayTimer < 5) {
				DelayTimer += 1;
				return;
			} */

			// First, we find a homing target if we don't have one
			if (HomingTarget == null ) {
				HomingTarget = FindClosestNPC(maxDetectRadius);
			}

			// If we have a homing target, make sure it is still valid. If the NPC dies or moves away, we'll want to find a new target
			if (HomingTarget != null && !IsValidTarget(HomingTarget) && FindClosestNPC(maxDetectRadius) != null) {
				HomingTarget = null;
			} 
			
			// If we don't have a target, don't adjust trajectory
			if (HomingTarget == null) {
				return;
			}
			
			
			// If found, we rotate the projectile velocity in the direction of the target.
			// We only rotate by 3 degrees an update to give it a smooth trajectory. Increase the rotation speed here to make tighter turns
			float length = Projectile.velocity.Length();
			float targetAngle = Projectile.AngleTo(HomingTarget.Center);
            // Increasing x in ToRadians(x) tightens the homing curve - JBN, 360 means it will stick on them until it dies
			//Projectile.velocity = Projectile.velocity.ToRotation().AngleTowards(targetAngle, MathHelper.ToRadians(3)).ToRotationVector2() * length;
			Projectile.velocity = Projectile.velocity.ToRotation().AngleTowards(targetAngle, MathHelper.ToRadians(720)).ToRotationVector2() * length;
			Projectile.rotation = Projectile.velocity.ToRotation();
			//Main.NewText("Text", new Color(0, 0, 0));	

			//Dust
			if (Main.rand.NextBool(5)) // only spawn 20% of the time 
			{
				int choice = Main.rand.Next(3); // choose a random number: 0, 1, or 2
				if (choice == 0) // use that number to select dustID: 15, 57, or 58
				{
					choice = 15;
				}
				else if (choice == 1)
				{
					choice = 57;
				}
				else
				{
					choice = 58;
				}
				// Spawn the dust
				Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, choice, Projectile.velocity.X * 0.25f, Projectile.velocity.Y * 0.25f, 150, default(Color), 0.7f);
			}
			//Light
			Lighting.AddLight(Projectile.Center, 0.9f, 0.1f, 0.3f);
			
		}

		
		// Finding the closest NPC to attack within maxDetectDistance range
		// If not found then returns null
		public NPC FindClosestNPC(float maxDetectDistance) {
			NPC closestNPC = null;

			// Using squared values in distance checks will let us skip square root calculations, drastically improving this method's speed.
			float sqrMaxDetectDistance = maxDetectDistance * maxDetectDistance;

			// Loop through all NPCs
			foreach (var target in Main.ActiveNPCs) {
				// Check if NPC able to be targeted.
				if (IsValidTarget(target)) {
					// The DistanceSquared function returns a squared distance between 2 points, skipping relatively expensive square root calculations
					float sqrDistanceToTarget = Vector2.DistanceSquared(target.Center, Projectile.Center);

					// Check if it is within the radius
					if (sqrDistanceToTarget < sqrMaxDetectDistance) {
						sqrMaxDetectDistance = sqrDistanceToTarget;
						closestNPC = target;
					}
				}
			}

			return closestNPC;
		}

		public bool IsValidTarget(NPC target) {
			// This method checks that the NPC is:
			// 1. active (alive)
			// 2. chaseable (e.g. not a cultist archer)
			// 3. max life bigger than 5 (e.g. not a critter)
			// 4. can take damage (e.g. moonlord core after all it's parts are downed)
			// 5. hostile (!friendly)
			// 6. not immortal (e.g. not a target dummy)
			// 7. doesn't have solid tiles blocking a line of sight between the projectile and NPC
			return target.CanBeChasedBy() 
			&& Collision.CanHit(Projectile.Center, 1, 1, target.position, target.width, target.height) 
			&& !invalidTargets.Contains(target);
		}
	}
}