using System.Collections.Generic;
using UnityEngine;

namespace Slice
{
    public class Upgrade
    {
        public string Name;
        public string Desc;
        public System.Action Apply;
    }

    public static class UpgradeSystem
    {
        static PlayerController Player => Object.FindFirstObjectByType<PlayerController>();

        public static List<Upgrade> Pool() => new List<Upgrade>
        {
            new Upgrade { Name = "SWIFT BLADE", Desc = "+35% fire rate", Apply = () => Player.Weapon.FireRate *= 1.35f },
            new Upgrade { Name = "HEAVY ROUNDS", Desc = "+40% damage", Apply = () => Player.Weapon.Damage *= 1.4f },
            new Upgrade { Name = "TWIN SHOT", Desc = "+1 projectile", Apply = () => Player.Weapon.ProjectileCount += 1 },
            new Upgrade { Name = "LIGHTSTEP", Desc = "+20% move speed", Apply = () => Player.MoveSpeed *= 1.2f },
            new Upgrade { Name = "VITAL SURGE", Desc = "restore 40 HP now", Apply = () => Player.Hp.Heal(40) },
            new Upgrade { Name = "IRON SKIN", Desc = "+30 max HP & heal 30", Apply = () => { Player.Hp.Max += 30; Player.Hp.Heal(30); } },
            new Upgrade { Name = "VELOCITY", Desc = "+30% projectile speed", Apply = () => Player.Weapon.ProjectileSpeed *= 1.3f },
            new Upgrade { Name = "MAGNET", Desc = "+60% pickup radius", Apply = () => Player.PickupRadius *= 1.6f },
            new Upgrade { Name = "PHASE", Desc = "-30% dash cooldown", Apply = () => Player.DashCooldown *= 0.7f },
        };

        public static List<Upgrade> DrawThree()
        {
            var pool = Pool();
            var pick = new List<Upgrade>();
            for (int i = 0; i < 3 && pool.Count > 0; i++)
            {
                int k = Random.Range(0, pool.Count);
                pick.Add(pool[k]);
                pool.RemoveAt(k);
            }
            return pick;
        }
    }
}
