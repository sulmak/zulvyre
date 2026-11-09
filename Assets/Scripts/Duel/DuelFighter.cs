using UnityEngine;

namespace Zulvyre
{
    public class DuelFighter : MonoBehaviour
    {
        const float GroundY = 0.08f;

        public HeroDef Def { get; private set; }
        public bool IsPlayer { get; private set; }
        public DuelScene Scene { get; private set; }
        public DuelFighter Opponent;

        public float Hp { get; private set; }
        public float Stamina { get; private set; }
        public float MaxHp { get { return Def != null ? Def.MaxHp : 100f; } }
        public float MaxStamina { get { return Def != null ? Def.MaxStamina : 100f; } }
        public float HpFraction { get { return MaxHp > 0f ? Hp / MaxHp : 0f; } }
        public float StaminaFraction { get { return MaxStamina > 0f ? Stamina / MaxStamina : 0f; } }
        public int Facing { get; private set; } = 1;
        public int Combo { get; private set; }
        public int BestCombo { get; private set; }
        public float DamageDealt { get; private set; }
        public bool Dead { get; private set; }
        public float FailedTimer { get; private set; }
        public string FailedText { get; private set; }
        public float AbilityCooldown { get { return abilityCooldown; } }
        public float AbilityCooldownTotal { get { return GetAbilityCooldownTotal(); } }
        public float AbilityCost { get { return GetAbilityCost(); } }
        public bool AbilityReady { get { return abilityCooldown <= 0f; } }
        public bool Busy { get { return attackKind != 0 || dodgeTimer > 0f; } }
        public bool IsAttacking { get { return attackKind == 1 || attackKind == 2; } }
        public bool IsWindingHeavy { get { return attackKind == 2 && !attackHitDone && attackTime < attackHitTime; } }
        public Vector2 Position { get { return new Vector2(transform.position.x, transform.position.z); } }
        public Vector2 BoundsMin = new Vector2(-5.6f, -1.15f);
        public Vector2 BoundsMax = new Vector2(5.6f, 1.15f);
        public bool AutoPilot;

        Vector2? clickTarget;

        public bool CanActNow { get { return CanAct(); } }

        public void CommandMove(Vector2 worldPoint)
        {
            clickTarget = worldPoint;
        }

        ZulvyreGame game;
        HumanAnimator animator;
        Transform shield;
        SpriteRenderer shieldRenderer;
        SpriteRenderer auraRenderer;
        Quaternion facingTarget;

        Vector2 moveIntent;
        Vector2 externalMove;
        Vector2 dodgeDirection;
        Vector2 knockbackVelocity;

        int attackKind;
        float attackTime;
        float attackDuration;
        float attackHitTime;
        bool attackHitDone;
        int comboStep;
        float lastLightTime;
        float attackCooldown;
        float dodgeTimer;
        float dodgeReadyTime;
        float invulnTimer;
        float shieldTimer;
        float speedBuffTimer;
        float burnTimer;
        float burnDps;
        float burnTick;
        float flashTimer;
        float staminaDelay;
        float abilityCooldown;
        float afterimageTimer;
        bool announcedWindup;

        public void Initialize(HeroDef def, bool isPlayer, DuelScene scene, Vector3 position)
        {
            Def = def;
            IsPlayer = isPlayer;
            Scene = scene;
            game = ZulvyreGame.Instance;
            Hp = def.MaxHp;
            Stamina = def.MaxStamina;
            Facing = isPlayer ? 1 : -1;
            transform.position = new Vector3(position.x, GroundY, position.z);
            BuildVisual();
        }

        void BuildVisual()
        {
            float yaw = Facing > 0 ? 90f : -90f;
            facingTarget = Quaternion.Euler(0f, yaw, 0f);
            transform.rotation = facingTarget;

            string body = Def != null && !string.IsNullOrEmpty(Def.Body) ? Def.Body : "man";
            var spawned = WorldModels.SpawnHuman(body, transform, transform.position, yaw);
            if (spawned != null)
            {
                animator = spawned.GetComponent<HumanAnimator>();
                WorldModels.Tint(spawned, Def.Color, "cloth");
                WorldModels.Tint(spawned, Def.Accent, "accent");
            }

            var auraObject = new GameObject("Ground Glow");
            auraObject.transform.SetParent(transform, false);
            auraObject.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            auraObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            auraObject.transform.localScale = new Vector3(2.1f, 2.1f, 1f);
            auraRenderer = auraObject.AddComponent<SpriteRenderer>();
            auraRenderer.sprite = ImageAssets.DotSprite;
            auraRenderer.color = new Color(Def.Color.r, Def.Color.g, Def.Color.b, 0.3f);
            auraRenderer.sortingOrder = 90;

            var shieldObject = new GameObject("Shield");
            shieldObject.transform.SetParent(transform, false);
            shieldObject.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            shieldObject.transform.localScale = new Vector3(2.2f, 1.7f, 1f);
            shieldRenderer = shieldObject.AddComponent<SpriteRenderer>();
            shieldRenderer.sprite = ImageAssets.RingSprite;
            shieldRenderer.color = new Color(0.55f, 0.82f, 1f, 0.75f);
            shieldRenderer.sortingOrder = 5200;
            shieldRenderer.enabled = false;
            shield = shieldObject.transform;
        }

        void Update()
        {
            if (Scene == null) return;
            float dt = Time.deltaTime;
            bool combat = Scene.CombatActive;

            if (combat && !Dead)
            {
                TickTimers(dt);

                if (IsPlayer)
                {
                    if (AutoPilot)
                    {
                        moveIntent = externalMove;
                    }
                    else
                    {
                        moveIntent = ReadPlayerIntent();
                        if (moveIntent.sqrMagnitude > 0.01f)
                        {
                            clickTarget = null;
                        }
                        else if (clickTarget.HasValue)
                        {
                            Vector2 delta = clickTarget.Value - Position;
                            if (delta.sqrMagnitude < 0.04f) clickTarget = null;
                            else moveIntent = delta.normalized;
                        }
                        if (Input.GetKeyDown(KeyCode.Space)) TryLightAttack();
                        if (Input.GetKeyDown(KeyCode.F)) TryHeavyAttack();
                        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
                        {
                            TryDodge(moveIntent.sqrMagnitude > 0.01f ? moveIntent : new Vector2(-Facing, 0f));
                        }
                        if (Input.GetKeyDown(KeyCode.Q)) TryAbility();
                    }
                }
                else
                {
                    moveIntent = externalMove;
                }

                UpdateAttack(dt);
                UpdateDodge(dt);
                ApplyMovement(dt);
            }
            else
            {
                moveIntent = Vector2.zero;
                if (!Dead)
                {
                    if (combat) TickTimers(dt);
                }
            }

            ApplyKnockback(dt);

            if (animator != null && !Dead)
            {
                bool walking = moveIntent.sqrMagnitude > 0.01f || dodgeTimer > 0f;
                animator.SetLoop(walking ? "walk" : "idle");
            }

            UpdateVisual(dt);
        }

        public void SetMove(Vector2 intent)
        {
            externalMove = intent.sqrMagnitude > 1f ? intent.normalized : intent;
        }

        Vector2 ReadPlayerIntent()
        {
            Vector2 intent = Vector2.zero;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) intent.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) intent.x += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) intent.y -= 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) intent.y += 1f;
            if (intent.sqrMagnitude > 1f) intent.Normalize();
            if (Mathf.Abs(intent.x) > 0.05f && attackKind == 0) Facing = intent.x > 0f ? 1 : -1;
            return intent;
        }

        void TickTimers(float dt)
        {
            abilityCooldown = Mathf.Max(0f, abilityCooldown - dt);
            invulnTimer = Mathf.Max(0f, invulnTimer - dt);
            attackCooldown = Mathf.Max(0f, attackCooldown - dt);
            staminaDelay = Mathf.Max(0f, staminaDelay - dt);
            flashTimer = Mathf.Max(0f, flashTimer - dt);
            FailedTimer = Mathf.Max(0f, FailedTimer - dt);

            if (staminaDelay <= 0f && attackKind == 0 && dodgeTimer <= 0f)
            {
                Stamina = Mathf.Min(MaxStamina, Stamina + 20f * dt);
            }

            if (shieldTimer > 0f) shieldTimer = Mathf.Max(0f, shieldTimer - dt);
            if (speedBuffTimer > 0f) speedBuffTimer = Mathf.Max(0f, speedBuffTimer - dt);

            if (Combo > 0)
            {
                comboTimer -= dt;
                if (comboTimer <= 0f)
                {
                    Combo = 0;
                    comboStep = 0;
                }
            }

            if (burnTimer > 0f)
            {
                burnTimer = Mathf.Max(0f, burnTimer - dt);
                burnTick -= dt;
                if (burnTick <= 0f)
                {
                    burnTick = 0.5f;
                    BurnTick();
                    if (Dead) return;
                }
            }
        }

        float comboTimer;

        void UpdateAttack(float dt)
        {
            if (attackKind == 0) return;
            attackTime += dt;

            if (!attackHitDone && attackTime >= attackHitTime)
            {
                attackHitDone = true;
                ExecuteHit();
            }

            if (attackTime >= attackDuration)
            {
                attackKind = 0;
                attackCooldown = 0.05f;
            }
        }

        void UpdateDodge(float dt)
        {
            if (dodgeTimer <= 0f) return;
            dodgeTimer = Mathf.Max(0f, dodgeTimer - dt);
            Vector2 step = dodgeDirection * 11.5f * dt;
            MoveWithinBounds(step, false);

            afterimageTimer -= dt;
            if (afterimageTimer <= 0f && game != null)
            {
                afterimageTimer = 0.08f;
                game.Fx.Ring(transform.position + Vector3.up * 0.04f, new Color(Def.Accent.r, Def.Accent.g, Def.Accent.b, 0.35f), 0.5f, 1.9f, 0.28f, 900, true, true);
            }
        }

        void ApplyMovement(float dt)
        {
            if (Dead) return;

            if (dodgeTimer > 0f) return;

            if (attackKind == 0 && moveIntent.sqrMagnitude > 0.01f)
            {
                float speed = Def.Speed * (speedBuffTimer > 0f ? 1.22f : 1f);
                MoveWithinBounds(moveIntent * speed * dt, false);
                if (Mathf.Abs(moveIntent.x) > 0.05f) Facing = moveIntent.x > 0f ? 1 : -1;
            }
            else if (attackKind == 0 && Opponent != null && !Opponent.Dead)
            {
                FaceOpponent();
            }

            if (Opponent != null && !Opponent.Dead && dodgeTimer <= 0f)
            {
                Vector2 delta = Position - Opponent.Position;
                float distance = delta.magnitude;
                if (distance < 0.72f && distance > 0.001f)
                {
                    MoveWithinBounds(delta.normalized * (0.72f - distance) * 0.5f, true);
                }
            }
        }

        void MoveWithinBounds(Vector2 delta, bool noFacing)
        {
            Vector2 next = Position + delta;
            next.x = Mathf.Clamp(next.x, BoundsMin.x, BoundsMax.x);
            next.y = Mathf.Clamp(next.y, BoundsMin.y, BoundsMax.y);
            transform.position = new Vector3(next.x, GroundY, next.y);
            if (!noFacing && dodgeTimer <= 0f && attackKind == 0 && Mathf.Abs(delta.x) > 0.0001f)
            {
                Facing = delta.x > 0f ? 1 : -1;
            }
        }

        void ApplyKnockback(float dt)
        {
            if (knockbackVelocity.sqrMagnitude < 0.001f) return;
            MoveWithinBounds(knockbackVelocity * dt, true);
            knockbackVelocity *= Mathf.Exp(-9f * dt);
            if (knockbackVelocity.sqrMagnitude < 0.01f) knockbackVelocity = Vector2.zero;
        }

        void FaceOpponent()
        {
            if (Opponent == null) return;
            float dx = Opponent.Position.x - Position.x;
            if (Mathf.Abs(dx) > 0.05f) Facing = dx > 0f ? 1 : -1;
        }

        bool CanAct()
        {
            return Scene != null && Scene.CombatActive && !Dead && attackKind == 0 && dodgeTimer <= 0f && attackCooldown <= 0f;
        }

        public bool TryLightAttack()
        {
            if (!CanAct()) return false;
            if (!SpendStamina(11f)) return false;

            if (Time.time - lastLightTime < 0.9f) comboStep = Mathf.Min(3, comboStep + 1);
            else comboStep = 1;
            lastLightTime = Time.time;
            comboTimer = 2.4f;

            FaceOpponent();
            attackKind = 1;
            attackTime = 0f;
            attackDuration = comboStep >= 3 ? 0.4f : 0.33f;
            attackHitTime = 0.14f;
            attackHitDone = false;
            if (animator != null) animator.Trigger("attack", comboStep >= 3 ? 2.6f : 3.1f);
            if (game != null) game.Audio.PlaySwing(false);
            return true;
        }

        public bool TryHeavyAttack()
        {
            if (!CanAct()) return false;
            if (!SpendStamina(26f)) return false;

            comboStep = 0;
            FaceOpponent();
            attackKind = 2;
            attackTime = 0f;
            attackDuration = 0.72f;
            attackHitTime = 0.4f;
            attackHitDone = false;
            announcedWindup = false;
            if (animator != null) animator.Trigger("attack", 1.9f);
            if (game != null) game.Audio.PlaySwing(true);
            return true;
        }

        public bool TryDodge(Vector2 direction)
        {
            if (Scene == null || !Scene.CombatActive || Dead || attackKind != 0 || dodgeTimer > 0f) return false;
            if (Time.time < dodgeReadyTime) return false;
            if (!SpendStamina(19f)) return false;

            dodgeDirection = direction.sqrMagnitude > 0.01f ? direction.normalized : new Vector2(-Facing, 0f);
            dodgeTimer = 0.2f;
            dodgeReadyTime = Time.time + 0.42f;
            invulnTimer = Mathf.Max(invulnTimer, 0.34f);
            afterimageTimer = 0f;
            if (game != null)
            {
                game.Audio.PlayDodge();
                game.Fx.Ring(transform.position + Vector3.up * 0.05f, new Color(Def.Accent.r, Def.Accent.g, Def.Accent.b, 0.5f), 0.4f, 1.7f, 0.3f, 900, true, true);
                game.Fx.Dust(transform.position, new Color(0.4f, 0.36f, 0.3f, 0.35f), 4, 0.7f);
            }
            return true;
        }

        public bool TryAbility()
        {
            if (!CanAct()) return false;
            if (abilityCooldown > 0f) return false;
            if (!SpendStamina(GetAbilityCost())) return false;

            abilityCooldown = GetAbilityCooldownTotal();
            FaceOpponent();
            attackKind = 3;
            attackTime = 0f;
            attackHitDone = false;
            announcedWindup = false;
            if (animator != null) animator.Trigger("attack", 2f);

            switch (Def.Ability)
            {
                case AbilityKind.Grace:
                    attackDuration = 0.52f;
                    attackHitTime = 0.22f;
                    break;
                case AbilityKind.Bulwark:
                    attackDuration = 0.58f;
                    attackHitTime = 0.26f;
                    break;
                case AbilityKind.Emberlash:
                    attackDuration = 0.5f;
                    attackHitTime = 0.2f;
                    break;
                case AbilityKind.Duskstep:
                    attackDuration = 0.66f;
                    attackHitTime = 0.32f;
                    if (!IsPlayer && game != null)
                    {
                        game.Fx.Ring(transform.position + Vector3.up * 0.04f, new Color(0.75f, 0.35f, 0.75f, 0.75f), 0.5f, 3.0f, 0.45f, 2100, true, true);
                    }
                    break;
            }

            if (game != null)
            {
                game.Fx.Text(transform.position + Vector3.up * 2.35f, Def.AbilityName, Def.Accent, 18, 0.55f, 1.1f);
            }
            return true;
        }

        void ExecuteHit()
        {
            switch (attackKind)
            {
                case 1:
                    ResolveWeaponHit(comboStep >= 3 ? 1.9f : 1.8f, (8.5f + comboStep * 1.7f) * Def.Power, false);
                    break;
                case 2:
                    ResolveWeaponHit(2.15f, 20f * Def.Power, true);
                    break;
                case 3:
                    ExecuteAbility();
                    break;
            }
        }

        void ResolveWeaponHit(float reach, float damage, bool heavy)
        {
            if (game != null)
            {
                game.Fx.Slash(transform.position, Facing, new Color(Def.Accent.r, Def.Accent.g, Def.Accent.b, 0.95f), heavy ? 1.28f : 1f);
            }
            if (CanHit(reach))
            {
                Opponent.TakeDamage(damage, Position, heavy, this);
                if (game != null)
                {
                    game.Fx.Shake(heavy ? 0.17f : 0.07f, heavy ? 0.34f : 0.16f);
                }
            }
        }

        bool CanHit(float reach)
        {
            if (Opponent == null || Opponent.Dead) return false;
            Vector2 delta = Opponent.Position - Position;
            if (Mathf.Abs(delta.y) > 1.08f) return false;
            if (Mathf.Abs(delta.x) > reach) return false;
            if (Mathf.Abs(delta.x) > 0.32f && Mathf.Sign(delta.x) != Facing) return false;
            return true;
        }

        void ExecuteAbility()
        {
            switch (Def.Ability)
            {
                case AbilityKind.Grace:
                    Heal(22f);
                    speedBuffTimer = 4f;
                    if (game != null)
                    {
                        game.Audio.PlayHeal();
                        game.Fx.Ring(transform.position + Vector3.up * 0.04f, new Color(0.7f, 0.95f, 0.65f, 0.8f), 0.4f, 3.2f, 0.55f, 2100, true, true);
                        game.Fx.Sparks(transform.position + Vector3.up * 1.1f, new Color(0.8f, 1f, 0.7f), 16, 0.8f);
                    }
                    break;
                case AbilityKind.Bulwark:
                    shieldTimer = 5f;
                    if (game != null)
                    {
                        game.Audio.PlayShield();
                        game.Fx.Ring(transform.position + Vector3.up * 1.0f, new Color(0.55f, 0.82f, 1f, 0.9f), 0.5f, 3.0f, 0.5f, 2200);
                    }
                    if (CanHit(2.25f))
                    {
                        Opponent.TakeDamage(10f * Def.Power, Position, true, this);
                        if (game != null) game.Fx.Shake(0.14f, 0.25f);
                    }
                    break;
                case AbilityKind.Emberlash:
                    if (Scene != null) Scene.CastFirebolt(this);
                    if (game != null) game.Audio.PlayBolt();
                    break;
                case AbilityKind.Duskstep:
                    if (Opponent != null && game != null)
                    {
                        game.Fx.Sparks(transform.position + Vector3.up * 1.1f, new Color(0.7f, 0.35f, 0.8f), 18, 1.2f);
                        game.Fx.Ring(transform.position + Vector3.up * 0.04f, new Color(0.6f, 0.3f, 0.7f, 0.6f), 0.4f, 2.6f, 0.4f, 1200, true, true);
                        Vector2 target = Opponent.Position - new Vector2(Opponent.Facing * 1.05f, -0.12f);
                        target.x = Mathf.Clamp(target.x, BoundsMin.x, BoundsMax.x);
                        target.y = Mathf.Clamp(target.y, BoundsMin.y, BoundsMax.y);
                        transform.position = new Vector3(target.x, GroundY, target.y);
                        FaceOpponent();
                        game.Fx.Sparks(transform.position + Vector3.up * 1.1f, new Color(0.75f, 0.4f, 0.9f), 16, 1f);
                        game.Fx.Ring(transform.position + Vector3.up * 0.04f, new Color(0.75f, 0.4f, 0.9f, 0.6f), 0.4f, 2.6f, 0.4f, 1200, true, true);
                        if (CanHit(2.0f))
                        {
                            Opponent.TakeDamage(12f * Def.Power, Position, false, this);
                            game.Fx.Slash(transform.position, Facing, new Color(0.9f, 0.55f, 0.95f), 1.2f);
                        }
                    }
                    break;
            }
        }

        public void TakeDamage(float amount, Vector2 sourcePosition, bool heavy, DuelFighter attacker)
        {
            if (Dead) return;
            if (invulnTimer > 0f)
            {
                if (game != null)
                {
                    game.Fx.Text(transform.position + Vector3.up * 1.8f, "EVADED", new Color(0.7f, 0.9f, 1f), 20, 0.7f, 0.7f);
                }
                return;
            }

            bool blocked = shieldTimer > 0f;
            float actual = blocked ? amount * 0.4f : amount;
            Hp = Mathf.Max(0f, Hp - actual);
            flashTimer = 0.16f;
            attackKind = 0;
            dodgeTimer = 0f;
            Combo = 0;
            comboStep = 0;
            if (animator != null) animator.Trigger("hurt", 1.4f);

            Vector2 away = Position - sourcePosition;
            if (away.sqrMagnitude < 0.001f) away = new Vector2(-Facing, 0f);
            knockbackVelocity = away.normalized * (heavy ? 5.8f : 2.9f) * (blocked ? 0.5f : 1f);

            if (game != null)
            {
                Color sparkColor = blocked ? new Color(0.6f, 0.85f, 1f) : new Color(1f, 0.72f, 0.4f);
                game.Fx.Sparks(transform.position + Vector3.up * 1.15f, sparkColor, heavy ? 22 : 12, heavy ? 1.4f : 1f);
                game.Fx.Text(transform.position + Vector3.up * 2.0f,
                    blocked ? "-" + Mathf.RoundToInt(actual) + " BLOCK" : "-" + Mathf.RoundToInt(actual),
                    blocked ? new Color(0.65f, 0.88f, 1f) : new Color(1f, 0.48f, 0.42f),
                    heavy ? 36f : 28f, 1.25f, 0.9f);
                game.Fx.Ring(transform.position + Vector3.up * 0.04f, blocked ? new Color(0.6f, 0.85f, 1f, 0.5f) : new Color(1f, 0.6f, 0.35f, 0.45f), 0.4f, 1.8f, 0.25f, 900, true, true);
                game.Fx.Shake(heavy ? 0.26f : 0.12f, heavy ? 0.42f : 0.2f);
                game.Fx.Freeze(heavy ? 0.09f : 0.05f);
                game.Audio.PlayHit(heavy);
                if (blocked) game.Audio.PlayShield();
            }

            if (attacker != null)
            {
                attacker.RegisterHit(actual);
            }

            if (IsPlayer && Scene != null) Scene.NotifyPlayerDamaged();
            if (Hp <= 0f) Die();
        }

        void RegisterHit(float amount)
        {
            DamageDealt += amount;
            Combo++;
            BestCombo = Mathf.Max(BestCombo, Combo);
            comboTimer = 2.4f;
        }

        public void ApplyBurn(float totalDamage, float duration)
        {
            if (Dead) return;
            burnTimer = duration;
            burnDps = totalDamage / Mathf.Max(0.1f, duration);
            burnTick = 0.5f;
        }

        void BurnTick()
        {
            if (Dead) return;
            float amount = burnDps * 0.5f;
            Hp = Mathf.Max(0f, Hp - amount);
            if (game != null)
            {
                game.Fx.Text(transform.position + Vector3.up * 1.6f, Mathf.CeilToInt(amount).ToString(), new Color(1f, 0.62f, 0.32f), 20f, 0.8f, 0.6f);
                game.Fx.Sparks(transform.position + Vector3.up * 1.1f, new Color(1f, 0.5f, 0.2f), 3, 0.4f);
            }
            if (IsPlayer && Scene != null) Scene.NotifyPlayerDamaged();
            if (Hp <= 0f) Die();
        }

        public void Heal(float amount)
        {
            if (Dead) return;
            float healed = Mathf.Min(MaxHp - Hp, amount);
            Hp += healed;
            if (game != null && healed > 0f)
            {
                game.Fx.Text(transform.position + Vector3.up * 2.0f, "+" + Mathf.RoundToInt(healed), new Color(0.65f, 1f, 0.7f), 30f, 1.2f, 0.9f);
            }
        }

        public void ForceDefeat()
        {
            if (Dead) return;
            invulnTimer = 0f;
            Die();
        }

        void Die()
        {
            if (Dead) return;
            Dead = true;
            Hp = 0f;
            attackKind = 0;
            dodgeTimer = 0f;
            shieldTimer = 0f;
            speedBuffTimer = 0f;
            knockbackVelocity = Vector2.zero;
            if (animator != null) animator.Trigger("die", 1f, true);
            if (game != null)
            {
                game.Fx.Sparks(transform.position + Vector3.up * 1.1f, new Color(1f, 0.72f, 0.4f), 26, 1.5f);
                game.Fx.Ring(transform.position + Vector3.up * 0.04f, new Color(1f, 0.7f, 0.45f, 0.5f), 0.5f, 3.4f, 0.5f, 900, true, true);
                game.Fx.Shake(0.28f, 0.5f);
            }
            if (Scene != null) Scene.OnFighterDied(this);
        }

        bool SpendStamina(float cost)
        {
            if (Stamina + 0.001f < cost)
            {
                FailedTimer = 1.0f;
                FailedText = "NO BREATH";
                return false;
            }
            Stamina -= cost;
            staminaDelay = 0.55f;
            return true;
        }

        float GetAbilityCost()
        {
            switch (Def.Ability)
            {
                case AbilityKind.Grace: return 32f;
                case AbilityKind.Bulwark: return 38f;
                case AbilityKind.Emberlash: return 34f;
                case AbilityKind.Duskstep: return 35f;
                default: return 30f;
            }
        }

        float GetAbilityCooldownTotal()
        {
            switch (Def.Ability)
            {
                case AbilityKind.Grace: return 8f;
                case AbilityKind.Bulwark: return 9f;
                case AbilityKind.Emberlash: return 7.5f;
                case AbilityKind.Duskstep: return 8.5f;
                default: return 8f;
            }
        }

        void UpdateVisual(float dt)
        {
            facingTarget = Quaternion.Euler(0f, Facing > 0 ? 90f : -90f, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, facingTarget, 1f - Mathf.Exp(-14f * dt));

            if (!announcedWindup && !IsPlayer && attackKind == 2 && attackTime < attackHitTime && game != null)
            {
                announcedWindup = true;
                game.Fx.Ring(transform.position + Vector3.up * 0.04f, new Color(1f, 0.55f, 0.3f, 0.8f), 0.7f, 2.7f, 0.4f, 2150, true, true);
                game.Fx.Text(transform.position + Vector3.up * 2.7f, "!", new Color(1f, 0.85f, 0.55f), 36f, 0.4f, 0.5f);
            }

            if (auraRenderer != null)
            {
                Color auraColor = speedBuffTimer > 0f
                    ? new Color(0.6f, 1f, 0.6f, 0.4f)
                    : new Color(Def.Color.r, Def.Color.g, Def.Color.b, 0.3f);
                if (invulnTimer > 0f && dodgeTimer <= 0f) auraColor.a *= 0.55f;
                auraColor.a = Mathf.Clamp01(auraColor.a + flashTimer * 1.8f);
                if (burnTimer > 0f) auraColor = Color.Lerp(auraColor, new Color(1f, 0.6f, 0.25f, 0.45f), 0.4f);
                if (Dead) auraColor.a *= 0.3f;
                auraRenderer.color = auraColor;
            }

            if (shieldRenderer != null)
            {
                bool shielded = shieldTimer > 0f && !Dead;
                shieldRenderer.enabled = shielded;
                if (shielded)
                {
                    shield.localRotation = Quaternion.Euler(0f, -transform.eulerAngles.y, 0f);
                    float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 5.5f);
                    shieldRenderer.color = new Color(0.55f, 0.85f, 1f, 0.34f + pulse * 0.24f);
                    shield.localScale = new Vector3(2.2f + pulse * 0.1f, 1.7f + pulse * 0.08f, 1f);
                }
            }
        }
    }
}
