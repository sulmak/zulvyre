using UnityEngine;

namespace Zulvyre
{
    public class RivalBrain : MonoBehaviour
    {
        DuelFighter self;
        DuelFighter foe;
        DuelScene scene;

        float attackCooldown;
        float heavyCooldown;
        float dodgeReaction;
        float abilityCooldown;
        float retreatTimer;
        float retreatCooldown;
        float strafeSign = 1f;
        float strafeTimer;
        float hesitate;

        public void Initialize(DuelFighter self, DuelFighter foe)
        {
            this.self = self;
            this.foe = foe;
            scene = self.Scene;
            attackCooldown = 0.65f;
            heavyCooldown = 4.2f;
            strafeSign = Random.value < 0.5f ? -1f : 1f;
            strafeTimer = Random.Range(0.8f, 1.7f);
        }

        void Update()
        {
            if (self == null || foe == null) return;
            if (scene == null || !scene.CombatActive || self.Dead || foe.Dead)
            {
                self.SetMove(Vector2.zero);
                return;
            }

            float dt = Time.deltaTime;
            attackCooldown -= dt;
            heavyCooldown -= dt;
            dodgeReaction -= dt;
            abilityCooldown -= dt;
            strafeTimer -= dt;
            retreatTimer -= dt;
            retreatCooldown -= dt;
            hesitate -= dt;

            if (strafeTimer <= 0f)
            {
                strafeSign = -strafeSign;
                strafeTimer = Random.Range(0.9f, 2.3f);
            }

            if (hesitate > 0f)
            {
                self.SetMove(Vector2.zero);
                return;
            }

            Vector2 toFoe = foe.Position - self.Position;
            float distance = toFoe.magnitude;
            Vector2 toward = distance > 0.001f ? toFoe / distance : new Vector2(1f, 0f);
            Vector2 sideways = new Vector2(-toward.y, toward.x) * strafeSign;

            if (dodgeReaction <= 0f && self.Stamina > 24f && distance < 2.9f)
            {
                bool threatened = foe.IsWindingHeavy || (foe.IsAttacking && distance < 2.1f);
                if (threatened && Random.value < 0.72f)
                {
                    if (self.TryDodge(-toward * 0.5f + sideways * 0.95f))
                    {
                        dodgeReaction = 1.45f;
                        attackCooldown = Mathf.Max(attackCooldown, 0.3f);
                        return;
                    }
                    dodgeReaction = 0.45f;
                }
            }

            if (abilityCooldown <= 0f && self.AbilityReady && self.Stamina >= self.AbilityCost && distance < 4.6f)
            {
                abilityCooldown = 1.4f;
                if (Random.value < 0.7f && self.TryAbility()) return;
            }

            if (retreatTimer <= 0f && retreatCooldown <= 0f && self.StaminaFraction < 0.24f && distance < 3.2f)
            {
                retreatTimer = Random.Range(0.9f, 1.3f);
                retreatCooldown = 2.6f;
            }

            if (retreatTimer > 0f)
            {
                self.SetMove(-toward * 0.95f + sideways * 0.22f);
                return;
            }

            if (attackCooldown <= 0f && distance < 1.88f && self.StaminaFraction > 0.28f)
            {
                bool heavy = heavyCooldown <= 0f && self.Stamina > 34f && Random.value < 0.5f;
                bool struck = heavy ? self.TryHeavyAttack() : self.TryLightAttack();
                if (struck)
                {
                    attackCooldown = heavy ? Random.Range(1.15f, 1.65f) : Random.Range(0.52f, 0.92f);
                    if (heavy) heavyCooldown = Random.Range(4.5f, 6.8f);
                    return;
                }
            }

            if (Random.value < dt * 0.28f) hesitate = Random.Range(0.06f, 0.18f);

            Vector2 move;
            if (distance > 2.0f)
            {
                move = toward + sideways * 0.2f;
            }
            else if (distance < 1.02f)
            {
                move = -toward * 0.7f + sideways * 0.5f;
            }
            else
            {
                move = sideways * 0.6f + toward * ((distance - 1.45f) * 0.9f);
            }

            self.SetMove(move);
        }
    }
}
