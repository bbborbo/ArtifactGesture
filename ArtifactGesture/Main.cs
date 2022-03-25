using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;
using System;
using System.Security;
using System.Security.Permissions;
using UnityEngine;
using UnityEngine.Networking;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]
#pragma warning disable 
namespace ArtifactGesture
{
    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [BepInPlugin("com.Borbo.ArtifactGesture", "This Mod Turns Gesture Into An Artifact So That People Can Stop Telling Me Removing Gesture In BalanceOverhaulRBO Was Unnecessary", "1.0.0")]
    [R2APISubmoduleDependency(nameof(ArtifactAPI), nameof(LoadoutAPI), nameof(LanguageAPI))]
    public class Main : BaseUnityPlugin
    {
        public static AssetBundle iconBundle = LoadAssetBundle(Properties.Resources.misc);
        public static AssetBundle LoadAssetBundle(Byte[] resourceBytes)
        {
            if (resourceBytes == null) throw new ArgumentNullException(nameof(resourceBytes));
            return AssetBundle.LoadFromMemory(resourceBytes);
        }

        ArtifactDef Gesture = ScriptableObject.CreateInstance<ArtifactDef>();
        public static float artifactCdr = 0.5f;

        public void Awake()
        {
            LanguageAPI.Add("ARTIFACT_GESTURE_NAME", "Artifact of the Drowned");
            LanguageAPI.Add("ARTIFACT_GESTURE_DESC", "Dramatically reduce Equipment cooldown... <style=cIsHealth>BUT it automatically activates.</style>");

            Gesture.nameToken = "ARTIFACT_GESTURE_NAME";
            Gesture.descriptionToken = "ARTIFACT_GESTURE_DESC";
            Gesture.smallIconDeselectedSprite = iconBundle.LoadAsset<Sprite>("Assets/gesturedeactivated.png");
            Gesture.smallIconSelectedSprite = iconBundle.LoadAsset<Sprite>("Assets/gesture.png");
            Gesture.unlockableDef = UnlockableCatalog.GetUnlockableDef("SuicideHermitCrabs");

            On.RoR2.EquipmentSlot.FixedUpdate += GestureArtifactLogic;
            On.RoR2.ItemCatalog.Init += RemoveGestureItem;
            On.RoR2.Inventory.CalculateEquipmentCooldownScale += GestureArtifactCdr;
            ArtifactAPI.Add(Gesture);
        }

        private float GestureArtifactCdr(On.RoR2.Inventory.orig_CalculateEquipmentCooldownScale orig, Inventory self)
        {
            float f = orig(self);
            if (RunArtifactManager.instance.IsArtifactEnabled(Gesture.artifactIndex))
            {
                f *= (1 - artifactCdr);
            }
            return f;
        }

        private void GestureArtifactLogic(On.RoR2.EquipmentSlot.orig_FixedUpdate orig, EquipmentSlot self)
        {
            orig(self);

			bool flag = false;
            if(self.equipmentIndex != RoR2Content.Equipment.GoldGat.equipmentIndex)
            {
                if (!self.inputBank.activateEquipment.justPressed && RunArtifactManager.instance.IsArtifactEnabled(Gesture.artifactIndex))
                {
                    flag = true;
                }
            }

            bool isEquipmentActivationAllowed = self.characterBody.isEquipmentActivationAllowed;
            if (flag && isEquipmentActivationAllowed && self.hasEffectiveAuthority)
            {
                if (NetworkServer.active)
                {
                    self.ExecuteIfReady();
                    return;
                }
                self.CallCmdExecuteIfReady();
            }
        }

        private void RemoveGestureItem(On.RoR2.ItemCatalog.orig_Init orig)
        {
            orig();
            RoR2Content.Items.AutoCastEquipment.tier = ItemTier.NoTier;
        }
    }
}
