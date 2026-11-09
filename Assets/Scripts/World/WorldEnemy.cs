using UnityEngine;

namespace Zulvyre
{
    public class WorldEnemy : MonoBehaviour
    {
        public WorldScene Scene;
        public string Kind = "orc";
        public string Display = "Orc Raider";
        public bool Boss;
        public bool Friendly;
        public float MaxHp = 60f;
        public float Hp;
        public float Damage = 8f;
        public float Speed = 2.6f;
        public float AggroRadius = 10f;
        public float AttackRange = 2f;
        public float Xp = 40f;
        public bool Dead { get; private set; }
        public float HpFraction => MaxHp <= 0f ? 0f : Mathf.Clamp01(Hp / MaxHp);

        Transform model;
        HumanAnimator animator;
        Renderer[] renderers;
        MaterialPropertyBlock block;
        Vector3 home;
        Vector3 lastStep;
        Vector3 wanderTarget;
        float wanderTimer;
        float attackCooldown;
        float flashTimer;
        float spawnGrace;
        float bobPhase;

        public void Setup(WorldScene scene, string kind, Vector3 homePosition, string display = null,
            Color? tint = null, string body = null)
        {
            Scene = scene;
            Kind = kind;
            home = homePosition;
            wanderTarget = homePosition;
            transform.position = homePosition;

            string modelId = kind;
            float modelHeight = 0f;
            bool human = false;

            switch (kind)
            {
                case "wolf":
                    Display = "Greywood Wolf"; MaxHp = 45f; Damage = 7f; Speed = 3.5f; AggroRadius = 11f; AttackRange = 1.8f; Xp = 40f;
                    break;
                case "orc":
                    Display = "Durnmar Raider"; MaxHp = 95f; Damage = 11f; Speed = 2.6f; AggroRadius = 10f; AttackRange = 2.1f; Xp = 70f;
                    break;
                case "chief":
                    Display = "Warchief Grok"; MaxHp = 340f; Damage = 19f; Speed = 2.9f; AggroRadius = 15f; AttackRange = 2.4f; Xp = 520f;
                    Boss = true; modelId = "orc"; modelHeight = 2.5f;
                    break;
                case "vampire":
                    Display = "Nocturne Bloodsworn"; MaxHp = 115f; Damage = 13f; Speed = 3.2f; AggroRadius = 11f; AttackRange = 2f; Xp = 90f;
                    break;
                case "countess":
                    Display = "Countess Velyra"; MaxHp = 480f; Damage = 21f; Speed = 3.3f; AggroRadius = 16f; AttackRange = 2.3f; Xp = 680f;
                    Boss = true; modelId = "vampire"; modelHeight = 2.5f;
                    break;
                case "skeleton":
                    Display = "Crypt Boneguard"; MaxHp = 65f; Damage = 9f; Speed = 2.7f; AggroRadius = 10f; AttackRange = 2f; Xp = 55f;
                    break;
                case "warden":
                    Display = "The Undying Warden"; MaxHp = 650f; Damage = 24f; Speed = 2.9f; AggroRadius = 17f; AttackRange = 2.5f; Xp = 1200f;
                    Boss = true; modelId = "skeleton"; modelHeight = 2.55f;
                    break;
                case "villager":
                    Display = display ?? "Alderwick Villager";
                    Friendly = true;
                    MaxHp = 1f; Damage = 0f; Speed = 1.35f; AggroRadius = 0f; AttackRange = 0f; Xp = 0f;
                    modelId = body ?? "man";
                    human = true;
                    break;
            }

            if (!string.IsNullOrEmpty(display) && !Friendly) Display = display;
            Hp = MaxHp;

            float yaw = Random.Range(0f, 360f);
            var spawned = human
                ? WorldModels.SpawnHuman(modelId, transform, transform.position, yaw)
                : WorldModels.Spawn(modelId, transform, transform.position, yaw, modelHeight);
            model = spawned.transform;
            model.localPosition = Vector3.zero;
            if (human) animator = spawned.GetComponent<HumanAnimator>();
            lastStep = transform.position;

            if (tint.HasValue)
            {
                WorldModels.Tint(spawned, tint.Value, "cloth", "hood", "fur", "vampcloth", "blood");
                WorldModels.Tint(spawned, tint.Value * 0.8f + Color.white * 0.2f, "accent");
            }

            if (Boss) AddBossAura();

            renderers = spawned.GetComponentsInChildren<Renderer>();
            block = new MaterialPropertyBlock();
            wanderTimer = Random.Range(1f, 4f);
            spawnGrace = 0.5f;
            bobPhase = Random.Range(0f, 12f);
        }

        void AddBossAura()
        {
            Color auraColor;
            float range;
            switch (Kind)
            {
                case "countess": auraColor = new Color(0.66f, 0.34f, 0.95f); range = 6f; break;
                case "warden": auraColor = new Color(0.4f, 0.85f, 0.62f); range = 6.5f; break;
                case "chief": auraColor = new Color(1f, 0.52f, 0.24f); range = 5.5f; break;
                default: auraColor = new Color(1f, 0.7f, 0.4f); range = 5f; break;
            }

            var glow = new GameObject("Boss Aura");
            glow.transform.SetParent(transform, false);
            glow.transform.localPosition = Vector3.up * 1.15f;
            var light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = auraColor;
            light.range = range;
            light.intensity = 1.15f;
            light.shadows = LightShadows.None;
        }

        void Update()
        {
            if (Dead || Scene == null || Scene.Paused || Scene.MapOpen || Scene.TourMode) return;

            float dt = Time.deltaTime;
            attackCooldown = Mathf.Max(0f, attackCooldown - dt);
            spawnGrace = Mathf.Max(0f, spawnGrace - dt);
            if (flashTimer > 0f)
            {
                flashTimer -= dt;
                if (flashTimer <= 0f) ApplyFlash(false);
            }

            if (Friendly)
            {
                Wander(dt, 0.5f);
                AnimateStep(dt);
                return;
            }

            var player = Scene.Player;
            if (player == null || player.Dead) return;

            Vector3 flat = transform.position;
            flat.y = 0f;
            Vector3 target = player.transform.position;
            target.y = 0f;
            float distance = Vector3.Distance(flat, target);

            if (distance < AggroRadius)
            {
                FaceDirection(target - flat);
                if (distance > AttackRange * 0.82f)
                {
                    MoveToward(target, Speed);
                }
                else if (attackCooldown <= 0f && spawnGrace <= 0f)
                {
                    Attack(player);
                }
            }
            else if (Vector3.Distance(flat, home) > AggroRadius * 2.2f)
            {
                MoveToward(home, Speed * 0.9f);
            }
            else
            {
                Wander(dt, 0.55f);
            }

            if (model != null)
            {
                float bob = Mathf.Sin(Time.time * 5.5f + bobPhase) * 0.015f;
                model.localPosition = new Vector3(0f, bob, 0f);
            }
        }

        void AnimateStep(float dt)
        {
            if (animator == null) return;
            Vector3 step = transform.position - lastStep;
            step.y = 0f;
            lastStep = transform.position;
            float speed = step.magnitude / Mathf.Max(0.0001f, dt);
            animator.SetLoop(speed > 0.25f ? "walk" : "idle");
        }

        void Wander(float dt, float speedFactor)
        {
            wanderTimer -= dt;
            if (wanderTimer <= 0f)
            {
                wanderTimer = Random.Range(2.2f, 5.5f);
                Vector2 offset = Random.insideUnitCircle * (Friendly ? 6f : 7f);
                wanderTarget = home + new Vector3(offset.x, 0f, offset.y);
            }

            Vector3 flat = transform.position;
            flat.y = 0f;
            if (Vector3.Distance(flat, wanderTarget) > 0.5f)
            {
                MoveToward(wanderTarget, Speed * speedFactor);
            }
        }

        void MoveToward(Vector3 target, float speed)
        {
            Vector3 delta = target - transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude < 0.0004f) return;
            Vector3 direction = delta.normalized;
            transform.position += direction * speed * Time.deltaTime;
            FaceDirection(direction);
        }

        void FaceDirection(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0004f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 9f * Time.deltaTime);
        }

        void Attack(WorldPlayer player)
        {
            attackCooldown = Boss ? 1.25f : 1.5f;
            Scene.EnemyStrike(this, player);
        }

        public void TakeDamage(float amount, Vector3 source, bool heavy)
        {
            if (Dead || Friendly) return;
            Hp = Mathf.Max(0f, Hp - amount);
            flashTimer = 0.14f;
            ApplyFlash(true);
            Scene.OnEnemyDamaged(this, amount, heavy);
            if (Hp <= 0f) Die();
        }

        void ApplyFlash(bool on)
        {
            if (renderers == null) return;
            if (on) block.SetColor("_Color", new Color(1f, 0.55f, 0.45f, 1f));
            else block.Clear();
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null) renderers[i].SetPropertyBlock(block);
            }
        }

        void Die()
        {
            if (Dead) return;
            Dead = true;
            Vector3 at = transform.position + Vector3.up * 0.9f;
            Color color = Boss ? new Color(1f, 0.6f, 0.25f) : new Color(0.85f, 0.5f, 0.35f);
            WorldFx.Burst(Scene, at, color, Boss ? 28 : 16, Boss ? 7f : 5f);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null) renderers[i].enabled = false;
            }
            Scene.OnEnemyKilled(this);
            Destroy(gameObject, 0.4f);
        }

        public void ForceKill()
        {
            if (!Dead) Die();
        }
    }
}
