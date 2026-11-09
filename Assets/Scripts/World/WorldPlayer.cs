using UnityEngine;

namespace Zulvyre
{
    public class WorldPlayer : MonoBehaviour
    {
        public WorldScene Scene;
        public HeroDef Hero;
        public float Hp = 100f;
        public float Stamina = 100f;
        public bool Dead;
        public bool God;
        public float ArtCooldown { get; private set; }
        public float ArtCooldownTotal => Scene != null && Scene.ArtCooldownTotal > 0f ? Scene.ArtCooldownTotal : 14f;
        public bool Shielded => shieldTimer > 0f;
        public bool Hasted => speedBuffTimer > 0f;

        CharacterController controller;
        Transform model;
        HumanAnimator animator;
        Vector3 modelHome;
        Vector3 modelBaseScale;
        Vector3 groundVelocity;
        Vector3 knockback;
        Vector3 dodgeDirection;
        Vector3 lastPosition;
        float planarSpeed;

        float attackCooldown;
        float heavyCooldown;
        float dodgeCooldown;
        float dodgeTimer;
        float invulnTimer;
        float shieldTimer;
        float speedBuffTimer;
        float staminaDelay;
        float swingTimer;
        float swingLength;

        public void Initialize(WorldScene scene, HeroDef hero)
        {
            Scene = scene;
            Hero = hero;
            controller = GetComponent<CharacterController>();
            if (controller == null)
            {
                controller = gameObject.AddComponent<CharacterController>();
                controller.height = 1.7f;
                controller.radius = 0.34f;
                controller.center = new Vector3(0f, 0.9f, 0f);
            }

            if (model != null) Destroy(model.gameObject);
            string body = hero != null && !string.IsNullOrEmpty(hero.Body) ? hero.Body : "man";
            var spawned = WorldModels.SpawnHuman(body, transform, transform.position, 0f);
            model = spawned.transform;
            model.localPosition = Vector3.zero;
            model.localRotation = Quaternion.identity;
            modelHome = model.localPosition;
            modelBaseScale = model.localScale;
            animator = spawned.GetComponent<HumanAnimator>();
            WorldModels.Tint(spawned, hero.Color, "cloth");
            WorldModels.Tint(spawned, hero.Accent, "accent");
            lastPosition = transform.position;

            Hp = Scene.MaxHp;
            Stamina = 100f;
            Dead = false;
            attackCooldown = heavyCooldown = dodgeCooldown = 0f;
            dodgeTimer = invulnTimer = shieldTimer = speedBuffTimer = 0f;
            ArtCooldown = 0f;
            knockback = Vector3.zero;
        }

        public void ResetVitals()
        {
            Hp = Scene.MaxHp;
            Stamina = 100f;
            Dead = false;
            dodgeTimer = invulnTimer = shieldTimer = speedBuffTimer = 0f;
            knockback = Vector3.zero;
            if (animator != null) animator.ResetPose();
        }

        public void Teleport(Vector3 position)
        {
            if (controller != null) controller.enabled = false;
            transform.position = position;
            if (controller != null) controller.enabled = true;
            groundVelocity = Vector3.zero;
            knockback = Vector3.zero;
        }

        void Update()
        {
            if (Scene == null) return;
            float dt = Time.deltaTime;

            attackCooldown = Mathf.Max(0f, attackCooldown - dt);
            heavyCooldown = Mathf.Max(0f, heavyCooldown - dt);
            dodgeCooldown = Mathf.Max(0f, dodgeCooldown - dt);
            invulnTimer = Mathf.Max(0f, invulnTimer - dt);
            shieldTimer = Mathf.Max(0f, shieldTimer - dt);
            speedBuffTimer = Mathf.Max(0f, speedBuffTimer - dt);
            ArtCooldown = Mathf.Max(0f, ArtCooldown - dt);
            staminaDelay = Mathf.Max(0f, staminaDelay - dt);
            if (swingTimer > 0f) swingTimer = Mathf.Max(0f, swingTimer - dt);

            Vector3 travel = transform.position - lastPosition;
            travel.y = 0f;
            lastPosition = transform.position;
            planarSpeed = Mathf.Lerp(planarSpeed, travel.magnitude / Mathf.Max(0.0001f, dt), 10f * dt);
            if (animator != null)
            {
                animator.SetLoop(planarSpeed > 5.1f ? "run" : planarSpeed > 0.5f ? "walk" : "idle");
            }

            if (swingTimer > 0f && model != null)
            {
                float k = 1f - swingTimer / Mathf.Max(0.01f, swingLength);
                model.localPosition = modelHome + new Vector3(0f, 0f, Mathf.Sin(k * Mathf.PI) * 0.16f);
            }
            else if (model != null && model.localPosition != modelHome)
            {
                model.localPosition = Vector3.Lerp(model.localPosition, modelHome, 12f * dt);
            }

            if (Scene.Paused || Scene.MapOpen || Dead)
            {
                if (Dead && Scene.AllowRespawn && Input.GetKeyDown(KeyCode.R)) Scene.RespawnPlayer();
                return;
            }

            if (Scene.TourMode) return;

            if (staminaDelay <= 0f) Stamina = Mathf.Min(100f, Stamina + 24f * dt);

            Vector3 move = ReadMove();
            bool run = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if ((Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)) && dodgeCooldown <= 0f && Stamina > 16f)
            {
                dodgeDirection = move.sqrMagnitude > 0.01f ? move.normalized : transform.forward;
                dodgeTimer = 0.22f;
                invulnTimer = 0.34f;
                dodgeCooldown = 0.9f;
                Stamina -= 16f;
                staminaDelay = 0.5f;
                if (Scene.Audio != null) Scene.Audio.PlayDodge();
            }

            float speed = (run ? 6.3f : 4.15f) * (speedBuffTimer > 0f ? 1.25f : 1f);
            if (dodgeTimer > 0f)
            {
                dodgeTimer -= dt;
                controller.Move((dodgeDirection * 13.5f + Vector3.up * groundVelocity.y) * dt);
            }
            else
            {
                groundVelocity.y = controller.isGrounded ? -2f : groundVelocity.y - 24f * dt;
                controller.Move((move * speed + Vector3.up * groundVelocity.y + knockback) * dt);
            }
            knockback = Vector3.Lerp(knockback, Vector3.zero, 6f * dt);

            if (move.sqrMagnitude > 0.01f && dodgeTimer <= 0f)
            {
                var target = Quaternion.LookRotation(move, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, 14f * dt);
            }

            if (Input.GetKeyDown(KeyCode.Space)) TryAttack(false);
            if (Input.GetKeyDown(KeyCode.F)) TryAttack(true);
            if (Input.GetKeyDown(KeyCode.Q)) TryArt();
            if (Input.GetKeyDown(KeyCode.E)) Scene.Interact(this);
        }

        Vector3 ReadMove()
        {
            float x = 0f;
            float z = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) z += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) z -= 1f;
            if (x == 0f && z == 0f) return Vector3.zero;

            var yaw = Quaternion.Euler(0f, Scene.CameraYaw, 0f);
            var move = (yaw * Vector3.forward * z + yaw * Vector3.right * x).normalized;
            return move;
        }

        void TryAttack(bool heavy)
        {
            if (heavy)
            {
                if (heavyCooldown > 0f || Stamina < 22f) return;
                heavyCooldown = 0.95f;
                staminaDelay = 0.6f;
                Stamina -= 22f;
                swingTimer = swingLength = 0.34f;
                if (animator != null) animator.Trigger("attack", 2.2f);
                Scene.PlayerAttack(this, true);
            }
            else
            {
                if (attackCooldown > 0f || Stamina < 9f) return;
                attackCooldown = 0.42f;
                staminaDelay = 0.55f;
                Stamina -= 9f;
                swingTimer = swingLength = 0.22f;
                if (animator != null) animator.Trigger("attack", 3.4f);
                Scene.PlayerAttack(this, false);
            }
        }

        void TryArt()
        {
            if (ArtCooldown > 0f) return;
            ArtCooldown = Scene.ArtCooldownTotal;
            Scene.PlayerArt(this);
        }

        public void CommandLightAttack()
        {
            TryAttack(false);
        }

        public void CommandHeavyAttack()
        {
            TryAttack(true);
        }

        public void CommandArt()
        {
            TryArt();
        }

        public void Heal(float amount)
        {
            Hp = Mathf.Min(Scene.MaxHp, Hp + amount);
        }

        public void GrantShield(float seconds)
        {
            shieldTimer = Mathf.Max(shieldTimer, seconds);
        }

        public void GrantHaste(float seconds)
        {
            speedBuffTimer = Mathf.Max(speedBuffTimer, seconds);
        }

        public void TakeDamage(float amount, Vector3 source)
        {
            if (Dead || God || invulnTimer > 0f) return;
            float actual = shieldTimer > 0f ? amount * 0.45f : amount;
            Hp = Mathf.Max(0f, Hp - actual);
            Vector3 away = transform.position - source;
            away.y = 0f;
            if (away.sqrMagnitude > 0.01f) knockback = away.normalized * 3.2f;
            Scene.OnPlayerDamaged(actual, shieldTimer > 0f);
            if (animator != null) animator.Trigger("hurt", 1.3f);
            if (Hp <= 0f)
            {
                Dead = true;
                if (animator != null) animator.Trigger("die", 1f, true);
                Scene.OnPlayerDied();
            }
        }

        public void FullHeal()
        {
            Hp = Scene.MaxHp;
            Stamina = 100f;
        }

        public void RefreshLook()
        {
            if (model == null || Hero == null) return;
            model.localScale = modelBaseScale;
        }
    }
}
