// Nice Stats -- 69 chargen attribute points and a skill-setting modal for Pillars of Eternity 1.
//
// Internal hook identity: LoomNiceStats / LoomNiceStats.dll /
// LoomNiceStats.Bootstrap.Tick().
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoomNiceStats
{
    public static class Bootstrap
    {
        private const string AbilityName = "Loom_Nice_Stats_Modal";
        private const int NiceNumber = 69;
        private const int NiceAttributeCap = 99;

        private static UICharacterCreationManager s_seenCreationManager;
        private static int s_originalPointBuy;
        private static int s_originalStatHardMaximum;
        private static bool s_hasOriginalCharacterCreationValues;
        private static readonly Dictionary<CharacterStats, int[]> s_appliedSkillBonuses =
            new Dictionary<CharacterStats, int[]>();

        public static void Tick()
        {
            try
            {
                HandleCharacterCreation();

                if (GameState.IsLoading || PartyMemberAI.PartyMembers == null)
                {
                    return;
                }

                HashSet<CharacterStats> seen = new HashSet<CharacterStats>();
                for (int i = 0; i < PartyMemberAI.PartyMembers.Length; i++)
                {
                    PartyMemberAI partyMember = PartyMemberAI.PartyMembers[i];
                    if (partyMember == null || partyMember.Secondary)
                    {
                        continue;
                    }

                    CharacterStats stats = partyMember.GetComponent<CharacterStats>();
                    if (stats == null)
                    {
                        continue;
                    }

                    seen.Add(stats);
                    GenericAbility modal = EnsureModal(stats);
                    if (modal != null && modal.Activated)
                    {
                        ApplyNiceSkills(stats);
                    }
                    else
                    {
                        RemoveNiceSkills(stats);
                    }
                }

                CleanupMissingPartyMembers(seen);
            }
            catch (Exception ex)
            {
                Debug.LogError("[LoomNiceStats] " + ex);
            }
        }

        private static void HandleCharacterCreation()
        {
            UICharacterCreationManager manager = UICharacterCreationManager.Instance;
            if (manager == null)
            {
                if (s_seenCreationManager != null)
                {
                    RestorePointBuy();
                }
                return;
            }

            if (manager != s_seenCreationManager)
            {
                RestorePointBuy();
                s_seenCreationManager = manager;
                s_originalPointBuy = manager.TotalPointBuy;
                s_originalStatHardMaximum = manager.StatHardMaximum;
                s_hasOriginalCharacterCreationValues = true;
            }

            if (manager.CreationType == UICharacterCreationManager.CharacterCreationType.NewPlayer
                || manager.CreationType == UICharacterCreationManager.CharacterCreationType.NewCompanion)
            {
                manager.TotalPointBuy = NiceNumber;
                manager.StatHardMaximum = NiceAttributeCap;
            }
            else
            {
                RestorePointBuy();
            }
        }

        private static void RestorePointBuy()
        {
            if (s_seenCreationManager != null && s_hasOriginalCharacterCreationValues)
            {
                s_seenCreationManager.TotalPointBuy = s_originalPointBuy;
                s_seenCreationManager.StatHardMaximum = s_originalStatHardMaximum;
            }
            s_seenCreationManager = null;
            s_originalPointBuy = 0;
            s_originalStatHardMaximum = 0;
            s_hasOriginalCharacterCreationValues = false;
        }

        private static GenericAbility EnsureModal(CharacterStats stats)
        {
            GenericAbility existing = stats.FindAbilityInstance(AbilityName);
            if (existing != null)
            {
                ConfigureAbility(existing, stats.gameObject);
                return existing;
            }

            GameObject templateObject = new GameObject(AbilityName);
            try
            {
                NiceStatsAbility template = templateObject.AddComponent<NiceStatsAbility>();
                ConfigureAbility(template, stats.gameObject);

                GenericAbility instance = stats.InstantiateAbility(template, GenericAbility.AbilityType.Ability);
                if (instance == null)
                {
                    Debug.LogError("[LoomNiceStats] Failed to add Nice Stats to " + stats.name + ".");
                    return null;
                }

                instance.name = AbilityName;
                ConfigureAbility(instance, stats.gameObject);
                instance.ForceInit();
                return instance;
            }
            finally
            {
                UnityEngine.Object.Destroy(templateObject);
            }
        }

        private static void ConfigureAbility(GenericAbility ability, GameObject owner)
        {
            ability.OverrideName = "Nice Stats";
            ability.Owner = owner;
            ability.Cooldown = 0f;
            ability.CooldownType = GenericAbility.CooldownMode.None;
            ability.Passive = false;
            ability.Modal = true;
            ability.CombatOnly = false;
            ability.NonCombatOnly = false;
            ability.HideFromUi = false;
            ability.HideFromCombatLog = true;
            ability.Grouping = GenericAbility.ActivationGroup.None;
            ability.EffectType = GenericAbility.AbilityType.Ability;
            ability.DurationOverride = 0f;
            ability.AppliedViaMod = true;
            ability.IsVisibleOnUI = true;
        }

        private static void ApplyNiceSkills(CharacterStats stats)
        {
            int[] applied = GetAppliedArray(stats);
            CharacterStats.SkillType[] skills = Skills;
            for (int i = 0; i < skills.Length; i++)
            {
                CharacterStats.SkillType skill = skills[i];
                if (applied[i] != 0)
                {
                    stats.AdjustSkillBonus(skill, -applied[i]);
                    applied[i] = 0;
                }

                int current = stats.CalculateSkill(skill);
                int delta = NiceNumber - current;
                if (delta != 0)
                {
                    stats.AdjustSkillBonus(skill, delta);
                    applied[i] = delta;
                }
            }
        }

        private static void RemoveNiceSkills(CharacterStats stats)
        {
            int[] applied;
            if (!s_appliedSkillBonuses.TryGetValue(stats, out applied))
            {
                return;
            }

            CharacterStats.SkillType[] skills = Skills;
            for (int i = 0; i < skills.Length; i++)
            {
                if (applied[i] != 0)
                {
                    stats.AdjustSkillBonus(skills[i], -applied[i]);
                    applied[i] = 0;
                }
            }
        }

        private static void CleanupMissingPartyMembers(HashSet<CharacterStats> seen)
        {
            List<CharacterStats> remove = null;
            foreach (CharacterStats stats in s_appliedSkillBonuses.Keys)
            {
                if (stats == null || !seen.Contains(stats))
                {
                    if (stats != null)
                    {
                        RemoveNiceSkills(stats);
                    }
                    if (remove == null)
                    {
                        remove = new List<CharacterStats>();
                    }
                    remove.Add(stats);
                }
            }

            if (remove == null)
            {
                return;
            }

            for (int i = 0; i < remove.Count; i++)
            {
                s_appliedSkillBonuses.Remove(remove[i]);
            }
        }

        private static int[] GetAppliedArray(CharacterStats stats)
        {
            int[] applied;
            if (!s_appliedSkillBonuses.TryGetValue(stats, out applied))
            {
                applied = new int[Skills.Length];
                s_appliedSkillBonuses[stats] = applied;
            }
            return applied;
        }

        private static CharacterStats.SkillType[] Skills
        {
            get
            {
                return new CharacterStats.SkillType[]
                {
                    CharacterStats.SkillType.Stealth,
                    CharacterStats.SkillType.Athletics,
                    CharacterStats.SkillType.Lore,
                    CharacterStats.SkillType.Mechanics,
                    CharacterStats.SkillType.Survival,
                    CharacterStats.SkillType.Crafting
                };
            }
        }
    }

    public class NiceStatsAbility : GenericAbility
    {
        protected override void ReportActivation(bool overridePassive)
        {
            try
            {
                Console.AddMessage("Nice Stats: all skills are now 69. This is scholarship.", Color.magenta);
            }
            catch
            {
            }
        }

        protected override void ReportDeactivation(bool overridePassive)
        {
            try
            {
                Console.AddMessage("Nice Stats: the scholarship has left the building.", Color.gray);
            }
            catch
            {
            }
        }
    }
}
