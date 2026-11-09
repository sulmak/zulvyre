using UnityEngine;

namespace Zulvyre
{
    public enum AbilityKind
    {
        Grace,
        Bulwark,
        Emberlash,
        Duskstep
    }

    public class HeroDef
    {
        public string Id;
        public string Name;
        public string Title;
        public string Blurb;
        public string AbilityName;
        public string AbilityBlurb;
        public string SpriteName;
        public string Body = "man";
        public Color Color;
        public Color Accent;
        public float MaxHp = 100f;
        public float MaxStamina = 100f;
        public float Speed = 3.8f;
        public float Power = 1f;
        public int Blade;
        public int Guard;
        public int Spirit;
        public AbilityKind Ability;
    }

    public static class HeroRoster
    {
        public static readonly HeroDef[] Heroes =
        {
            new HeroDef
            {
                Id = "rowan",
                Name = "Rowan",
                Title = "The Wayfarer",
                Blurb = "Raised on the river roads, Rowan mends what the road breaks. Balanced, patient, and hard to corner.",
                AbilityName = "Wayfarer's Grace",
                AbilityBlurb = "Mend 22 health and run swift for four seconds.",
                SpriteName = "rowan",
                Color = new Color32(111, 143, 95, 255),
                Accent = new Color32(207, 224, 168, 255),
                MaxHp = 100f,
                MaxStamina = 100f,
                Speed = 3.9f,
                Power = 1f,
                Blade = 3,
                Guard = 3,
                Spirit = 5,
                Ability = AbilityKind.Grace
            },
            new HeroDef
            {
                Id = "ilsa",
                Name = "Ilsa",
                Title = "The Shieldbearer",
                Blurb = "Sworn to the riverfolk, Ilsa holds the line when everyone else has fallen back.",
                AbilityName = "Bulwark Oath",
                AbilityBlurb = "Halve damage for five seconds, then shield bash.",
                SpriteName = "ilsa",
                Body = "woman",
                Color = new Color32(184, 92, 68, 255),
                Accent = new Color32(240, 176, 128, 255),
                MaxHp = 120f,
                MaxStamina = 90f,
                Speed = 3.35f,
                Power = 1.06f,
                Blade = 4,
                Guard = 5,
                Spirit = 2,
                Ability = AbilityKind.Bulwark
            },
            new HeroDef
            {
                Id = "corvin",
                Name = "Corvin",
                Title = "The Emberkith",
                Blurb = "Cinder-blooded and quick. Corvin trades certainty for fire, and fire always answers.",
                AbilityName = "Emberlash",
                AbilityBlurb = "Hurl a cinder bolt for 16 damage and lingering burn.",
                SpriteName = "corvin",
                Color = new Color32(142, 108, 196, 255),
                Accent = new Color32(224, 179, 255, 255),
                MaxHp = 90f,
                MaxStamina = 110f,
                Speed = 4.1f,
                Power = 0.96f,
                Blade = 5,
                Guard = 2,
                Spirit = 4,
                Ability = AbilityKind.Emberlash
            }
        };

        public static readonly HeroDef Rival = new HeroDef
        {
            Id = "vesper",
            Name = "Vesper",
            Title = "The Masked Duelist",
            Blurb = "A stranger in a porcelain mask who has never lost a duel beneath the lanterns.",
            AbilityName = "Duskstep",
            AbilityBlurb = "Vanish through the shadows and cut from behind.",
            SpriteName = "rival",
            Color = new Color32(116, 74, 96, 255),
            Accent = new Color32(226, 132, 148, 255),
            MaxHp = 115f,
            MaxStamina = 100f,
            Speed = 3.8f,
            Power = 1.02f,
            Blade = 5,
            Guard = 4,
            Spirit = 4,
            Ability = AbilityKind.Duskstep
        };

        public static HeroDef ByIndex(int index)
        {
            return Heroes[Mathf.Clamp(index, 0, Heroes.Length - 1)];
        }

        public static int IndexOf(HeroDef hero)
        {
            for (int i = 0; i < Heroes.Length; i++)
            {
                if (Heroes[i] == hero) return i;
            }
            return 0;
        }

        public static HeroDef OtherThan(HeroDef hero, int offset = 1)
        {
            int index = IndexOf(hero);
            return Heroes[(index + offset) % Heroes.Length];
        }
    }
}
