using BepInEx.Configuration;
using R2API;
using RoR2;
using RazorwireMod.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using HarmonyLib;
using static RazorwireMod.RazorwireModPlugin;
using RoR2.Orbs;
using RoR2.Items;
using System.Linq;
using HG;

namespace RazorwireMod.Items
{
    public class Razorhive : ItemBase<Razorhive>
    {
        public ConfigEntry<float> baseDamage;
        public ConfigEntry<float> stackingDamage;

        public ConfigEntry<int> baseTargets;
        public ConfigEntry<int> stackingTargets;

        public ConfigEntry<float> baseRange;
        public ConfigEntry<float> stackingRange;

        public ConfigEntry<float> baseDuration;
        public ConfigEntry<float> stackingDuration;

        public ConfigEntry<float> frequency;
        public ConfigEntry<bool> atkScale;

        public ConfigEntry<float> procCoeff;


        public override string ItemName => "Razorhive";

        public override string ItemLangTokenName => "BEES_ITEM";

        public override string ItemPickupDesc => $"Retaliate with a swarm of Void Bees on taking damage. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";

        public override string ItemFullDescription => $"Receiving damage <style=cIsDamage>angers</style> a swarm of Void Bees for <style=cIsUtility>{baseDuration.Value}s</style>" + (stackingDuration.Value != 0 ? $" <style=cStack>(+{stackingDuration.Value}s per stack)</style>" : "") + $". For the duration, <style=cIsDamage>{baseTargets.Value}</style>" + (stackingTargets.Value != 0 ? $" <style=cStack>(+{stackingTargets.Value} per stack)</style>" : "") + $" enemies within <style=cIsDamage>{baseRange.Value}m</style>" + (stackingRange.Value != 0 ? $" <style=cStack>(+{stackingRange.Value}m per stack)</style>" : "") + $" are stung for <style=cIsDamage>{baseDamage.Value * 100}%</style>" + (stackingDamage.Value != 0 ? $" <style=cStack>(+{stackingDamage.Value * 100}% per stack)</style>" : "") + $" damage every <style=cIsUtility>{frequency.Value}</style> seconds. Each sting <style=cIsUtility>refreshes</style> the duration. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";

        public override string ItemLore => "<style=cMono>Lab Dissection Analysis File</style>\r\n\r\nSubject: Void Hive [Additionally referred to as 'Razorhives,' 'hives,' or 'nests']\r\nTechnician: C. Foreman \r\nTable Spec: Mineral Analysis BFC-5\r\nNotes:\r\n\r\n> Seem to be a naturally occurring facet of void terrane. A small area near the surface begins to harden, and given an undetermined amount of time a razorhive seems to form.\r\n> It seems the presence of a single hive encourages more to 'grow' - even on terrain that would not originally be suitable for hive growth, as there have been hives found on both organic and metallic substances. Their appearance and general stability is somewhat dependant on their source material.\r\n> Some develop with full walls, some without them - the ones without clearly have some kind of orb residing in the center, though it is theorized that all hives contain them.\r\n> Proper dissection has been unsuccessful so far - and as such, the exact properties of these orbs are unknown. Any attempt to scan or probe them leads to swift destruction of the offending device.\r\n> The substance the orb is made of has been seen to 'spread' along open hives thin walls.\r\n> It seems these nests hold some kind of life within them, though as previously stated a detailed scan has not yet been successful.\r\n> The sounds the life within makes when angered resemble, as a scientist put it, \"the idea of a buzz.\" As such, the unfortunately unscientific title of 'Void Bees' has been granted until further notice."; //$"Void Nests [Additionally referred to as 'Razorhives' or 'hives'] seem to be a naturally occurring facet of void terrane. A small area near the surface begins to harden, and given an undertermined amount of time a razorhive seems to form. It seems the presence of a single hive encourages more to 'grow' - even on terrain that would not originally be suitable for hive growth, as there have been hives found on both organic and metallic substances. Some grow with full walls, some growing without them - the ones without clearly have some kind of orb residing in the center, though it is theorized that all hives contain them. The exact properties of these orbs are unknown - any attempt to scan or probe them leads to swift destruction of the offending device. The material - if it is indeed a typical physical object - an orb is made of has been seen to 'spread' along open hives thin walls. It seems these nests hold some kind of life within them, though as previously stated a detailed scan has not yet been successful. The sounds the life within makes when angered resemeble, as a scientist put it, \"the idea of a buzz.\" As such, the unfortunately unscientific title of 'Void Bees' has been granted until further notice.";
        //<style=cMono>Lab Research File</style>\r\n\r\nSubject: Void Hive [Additionally referred to as 'Razorhives,' 'hives,' or 'nests']\r\nTechnician: C. Foreman \r\nTable Spec: Mineral Analysis BFC-5\r\nNotes:\r\n\r\n> Seem to be a naturally occurring facet of void terrane. A small area near the surface begins to harden, and given an undertermined amount of time a razorhive seems to form. Using UB-2 due to temperatures above safe levels.\r\n> Removing molten enamel and placing aside for substance analysis. It\u2019s solid, yet swimming.\r\n> Upon structural investigation, found cavities and internal chambers\r\n> Reduce lab temperatures by 10 degrees\r\n> Heat generating veins present - fire is being supplied to the tooth?\r\n> Removed my lab coat, very hot\r\n> Heat generation is still occurring in the severed object\r\n> Put some more ice in my drink WOW it's hot\r\n> Timestamping for break\r\n",

        public override ItemTier Tier => ItemTier.VoidTier1;

        public override GameObject ItemModel => RazorwireModPlugin.MainAssets.LoadAsset<GameObject>("RazorhivePickupFinal.prefab");

        public override Sprite ItemIcon => RazorwireModPlugin.MainAssets.LoadAsset<Sprite>("RazorhiveIcon512.png");


        public static GameObject ItemBodyModelPrefab;

        public static DamageAPI.ModdedDamageType BeesType = DamageAPI.ReserveDamageType();
        public static BuffDef beesActive { get; private set; }
        public static GameObject beesAura;
        public static GameObject beesOrb;
        public override ItemTag[] ItemTags => new ItemTag[2] { ItemTag.Damage, ItemTag.AIBlacklist };

        public override void Init(ConfigFile config)
        {
            CreateConfig(config);
            CreateLang();
            CreateItem();
            ItemDef.requiredExpansion = RazorwireModPlugin.sotvDLC;
            //VoidItemAPI.VoidTransformation.CreateTransformation(ItemDef, voidPair.Value);
            CreateBuff();

            beesAura = RazorwireModPlugin.MainAssets.LoadAsset<GameObject>("HiveDamageBonus.prefab");//Addressables.LoadAssetAsync<GameObject>("RoR2/Base/NearbyDamageBonus/NearbyDamageBonusIndicator.prefab").WaitForCompletion();
            beesAura.AddComponent<NetworkIdentity>();
            beesAura.AddComponent<NetworkedBodyAttachment>().shouldParentToAttachedBody = true;
            //beesAura = PrefabAPI.InstantiateClone(beesAuraTemp, "beesFocusAura");
            // var ps = beesAura.AddComponent<ParticleSystem>();

            beesAura.AddComponent<RazorhiveNetBehavior>();

            PrefabAPI.RegisterNetworkPrefab(beesAura);

            //matVoidInfestorTrail RoR2/DLC1/EliteVoid/matVoidInfestorTrail.mat
            //var tempThorns = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Thorns/RazorwireOrbEffect.prefab").WaitForCompletion();
            //GameObject tempThorns = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/OrbEffects/HealthOrbEffect");
            //Legac RoR2/DLC1/ChainLightningVoid/VoidLightningStrikeImpact.prefab

            var impact = RazorwireModPlugin.MainAssets.LoadAsset<GameObject>("HiveImpactVFX.prefab");

            impact.AddComponent<EffectComponent>();
            var vfxatri = impact.AddComponent<VFXAttributes>();
            vfxatri.vfxPriority = VFXAttributes.VFXPriority.Medium;
            vfxatri.vfxIntensity = VFXAttributes.VFXIntensity.Low;
            impact.AddComponent<DestroyOnParticleEnd>();

            ContentAddition.AddEffect(impact);

            beesOrb = RazorwireModPlugin.MainAssets.LoadAsset<GameObject>("RazorhiveOrbEffect.prefab");

            var tparent = beesOrb.transform.Find("TrailParent");
            var trail1 = tparent.transform.Find("Trailtail");
            var trail2 = tparent.transform.Find("Trailhead");

            beesOrb.AddComponent<EffectComponent>();


            var orbef = beesOrb.AddComponent<OrbEffect>();
            orbef.startVelocity1 = new Vector3(-2, -5, -2);
            orbef.startVelocity2 = new Vector3(2, 6, 2);
            orbef.endVelocity = Vector3.zero;

            var curve = new AnimationCurve();
            curve.AddKey(0, 0);
            curve.AddKey(1, 1);
            orbef.movementCurve = curve;

            orbef.faceMovement = true;

            orbef.endEffect = impact; //RazorwireModPlugin.MainAssets.LoadAsset<GameObject>("HiveImpactVFX.prefab"); //Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/ChainLightningVoid/VoidLightningStrikeImpact.prefab").WaitForCompletion();

            orbef.endEffectScale = .05f;

            var vfxatr = beesOrb.AddComponent<VFXAttributes>();
            vfxatr.vfxPriority = VFXAttributes.VFXPriority.Medium;
            vfxatr.vfxIntensity = VFXAttributes.VFXIntensity.Medium;

            var detach = beesOrb.AddComponent<DetachTrailOnDestroy>();
            detach.targetTrailRenderers = new TrailRenderer[2];
            detach.targetTrailRenderers[0] = trail1.GetComponent<TrailRenderer>();
            detach.targetTrailRenderers[1] = trail2.GetComponent<TrailRenderer>();

            ContentAddition.AddEffect(beesOrb);
            OrbAPI.AddOrb(typeof(HiveOrb));




            var mpp = ItemModel.AddComponent<ModelPanelParameters>();
            mpp.focusPointTransform = ItemModel.transform.Find("Target");
            mpp.cameraPositionTransform = ItemModel.transform.Find("Source");
            mpp.minDistance = 1f;
            mpp.maxDistance = 3f;
            mpp.modelRotation = Quaternion.Euler(new Vector3(0, -7.5f, 0));


            Hooks();
        }

        public void CreateBuff()
        {
            beesActive = ScriptableObject.CreateInstance<BuffDef>();
            beesActive.buffColor = Color.white;
            beesActive.canStack = false;
            beesActive.isDebuff = false;
            beesActive.name = "ZnRH" + "beesActive";
            beesActive.iconSprite = RazorwireModPlugin.MainAssets.LoadAsset<Sprite>("BeeSwarmBuff");
            ContentAddition.AddBuffDef(beesActive);
        }

        public override void CreateConfig(ConfigFile config)
        {
            baseDamage = config.Bind<float>("Item: " + ItemName, "Base Percent Damage", 1.6f, "Adjust the percent damage dealt to each target when active. (1 = 100% base damage)");
            stackingDamage = config.Bind<float>("Item: " + ItemName, "Stacking Percent Damage", 0, "Adjust the percent damage dealt to each target when active. (1 = 100% base damage)");

            baseTargets = config.Bind<int>("Item: " + ItemName, "Base Targets", 3, "Adjust number of targets on the first stack.");
            stackingTargets = config.Bind<int>("Item: " + ItemName, "Stacking Targets", 3, "Adjust number of additional targets per stack.");

            baseRange = config.Bind<float>("Item: " + ItemName, "Base Range", 20, "Adjust the base range of the item, in meters.");
            stackingRange = config.Bind<float>("Item: " + ItemName, "Stacking Range", 0, "Adjust the range gained per stack, in meters.");

            baseDuration = config.Bind<float>("Item: " + ItemName, "Base Duration", 7, "Adjust the duration the item remains active without any targets in seconds.");
            stackingDuration = config.Bind<float>("Item: " + ItemName, "Stacking Duration", 0, "Adjust the additional duration gained per stack that the item remains active without any targets, in seconds.");

            frequency = config.Bind<float>("Item: " + ItemName, "Hit Frequency", .3f, "Adjust the delay between hits in seconds.");
            atkScale = config.Bind<bool>("Item: " + ItemName, "Scale With Attack Speed", true, "Adjust if the item should scale how fast it hits with the player's current attack speed.");

            procCoeff = config.Bind<float>("Item: " + ItemName, "Proc Coefficient", .5f, "Adjust the item's proc coefficient.");
            voidPair = config.Bind<string>("Item: " + ItemName, "Item to Corrupt", "Thorns", "Adjust which item this is the void pair of.");
        }

        public override ItemDisplayRuleDict CreateItemDisplayRules() {

            ItemBodyModelPrefab = RazorwireModPlugin.MainAssets.LoadAsset<GameObject>("RazorhiveDisplay.prefab");

            var itemDisplay = ItemBodyModelPrefab.AddComponent<ItemDisplay>();
            itemDisplay.rendererInfos = ItemHelpers.ItemDisplaySetup(ItemBodyModelPrefab);

            ItemDisplayRuleDict rules = new ItemDisplayRuleDict();
            rules.Add("mdlCommandoDualies", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(0.08986F, 0.18178F, 0.02625F),
                    localAngles = new Vector3(39.39381F, 143.9695F, 175.1217F),
                    localScale = new Vector3(0.08F, 0.08F, 0.08F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(-0.05322F, 0.35144F, 0.02444F),
                    localAngles = new Vector3(15.41176F, 253.8848F, 214.7516F),
                    localScale = new Vector3(0.08F, 0.08F, 0.08F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmL",
                    localPos = new Vector3(0.07663F, 0.20013F, -0.01311F),
                    localAngles = new Vector3(70.11452F, 117.7668F, 97.04749F),
                    localScale = new Vector3(0.07F, 0.07F, 0.07F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmL",
                    localPos = new Vector3(-0.03855F, 0.03795F, -0.01967F),
                    localAngles = new Vector3(72.23871F, 320.5557F, 96.59081F),
                    localScale = new Vector3(0.07F, 0.07F, 0.07F)
                }
            });
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(0.0757F, 0.09909F, -0.02803F),
                    localAngles = new Vector3(20.04494F, 182.959F, 171.0799F),
                    localScale = new Vector3(0.09f, 0.09f, 0.09f)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(0.0471F, 0.1914F, 0.03421F),
                    localAngles = new Vector3(27.82059F, 313.7263F, 221.7217F),
                    localScale = new Vector3(0.075F, 0.075F, 0.075F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(-0.00706F, 0.37075F, -0.03099F),
                    localAngles = new Vector3(20.27856F, 325.2484F, 178.8349F),
                    localScale = new Vector3(0.065F, 0.065F, 0.065F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmL",
                    localPos = new Vector3(0.03639F, 0.13245F, -0.0151F),
                    localAngles = new Vector3(280.8907F, 273.2887F, 36.06231F),
                    localScale = new Vector3(0.06F, 0.06F, 0.06F)
                }
            });
            rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmR",
                    localPos = new Vector3(0.00763F, 0.14973F, -0.0994F),
                    localAngles = new Vector3(341.27F, 183.3672F, 353.9589F),
                    localScale = new Vector3(0.09F, 0.09F, 0.09F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmR",
                    localPos = new Vector3(-0.0489F, 0.27524F, -0.021F),
                    localAngles = new Vector3(0.78262F, 152.1891F, 237.8484F),
                    localScale = new Vector3(0.08F, 0.08F, 0.08F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmR",
                    localPos = new Vector3(0.05215F, 0.08754F, -0.00862F),
                    localAngles = new Vector3(278.3737F, 226.3813F, 143.9754F),
                    localScale = new Vector3(0.07F, 0.07F, 0.07F)
                }
            });
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmR",
                    localPos = new Vector3(0.5124F, 1.68144F, 0.06765F),
                    localAngles = new Vector3(48.88622F, 167.6275F, 168.4566F),
                    localScale = new Vector3(0.8F, 0.8F, 0.8F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmR",
                    localPos = new Vector3(0.3696F, 2.98811F, -0.01846F),
                    localAngles = new Vector3(344.0563F, 358.3774F, 188.01F),
                    localScale = new Vector3(0.7F, 0.7F, 0.7F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmR",
                    localPos = new Vector3(-0.01485F, 2.71368F, 0.59185F),
                    localAngles = new Vector3(333.8765F, 65.14996F, 95.80524F),
                    localScale = new Vector3(0.7F, 0.7F, 0.7F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmR",
                    localPos = new Vector3(-0.34639F, 0.83177F, -0.04555F),
                    localAngles = new Vector3(335.9776F, 48.92803F, 291.1535F),
                    localScale = new Vector3(0.6F, 0.6F, 0.6F)
                }
            });
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(0.09384F, 0.03343F, 0.01398F),
                    localAngles = new Vector3(3.09016F, 180.1063F, 157.2299F),
                    localScale = new Vector3(0.1F, 0.1F, 0.1F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(0.06905F, 0.17195F, -0.05532F),
                    localAngles = new Vector3(310.6565F, 224.6546F, 82.55707F),
                    localScale = new Vector3(0.08F, 0.08F, 0.08F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(-0.02069F, 0.20277F, 0.09556F),
                    localAngles = new Vector3(272.6252F, 89.88843F, 81.50568F),
                    localScale = new Vector3(0.07F, 0.07F, 0.07F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.35076F, 0.31487F, 0.06408F),
                    localAngles = new Vector3(53.89389F, 243.982F, 72.66495F),
                    localScale = new Vector3(0.08F, 0.08F, 0.08F)
                }
            });
            rules.Add("mdlEngiTurret", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule // turret
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
					localPos = new Vector3(-0.45185F, 0.72382F, 0.87907F),
					localAngles = new Vector3(352.6458F, 127.7709F, 81.96054F),
					localScale = new Vector3(0.25F, 0.25F, 0.25F)
                },
                new RoR2.ItemDisplayRule // turret
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(-0.2733F, 0.68926F, 1.30412F),
                    localAngles = new Vector3(28.60338F, 35.61071F, 96.84904F),
                    localScale = new Vector3(0.2F, 0.2F, 0.2F)
                },
                new RoR2.ItemDisplayRule // turret
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(-0.71675F, 0.68918F, 1.0976F),
                    localAngles = new Vector3(14.94149F, 172.3343F, 23.53251F),
                    localScale = new Vector3(0.2F, 0.2F, 0.2F)
                }

            });
            rules.Add("mdlMage", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(0.05004F, 0.03356F, 0.00382F),
                    localAngles = new Vector3(343.842F, 185.3361F, 177.7416F),
                    localScale = new Vector3(0.09F, 0.09F, 0.09F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(-0.00539F, 0.2342F, -0.02849F),
                    localAngles = new Vector3(7.06941F, 345.5822F, 278.2354F),
                    localScale = new Vector3(0.08F, 0.08F, 0.08F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(0.01251F, 0.25233F, 0.03876F),
                    localAngles = new Vector3(318.816F, 97.06329F, 170.963F),
                    localScale = new Vector3(0.06F, 0.06F, 0.06F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmL",
                    localPos = new Vector3(0.09566F, 0.09873F, -0.00181F),
                    localAngles = new Vector3(0.22826F, 94.4492F, 234.2892F),
                    localScale = new Vector3(0.06F, 0.06F, 0.06F)
                },

            });
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(0.13782F, 0.06581F, 0.0138F),
                    localAngles = new Vector3(26.42609F, 182.1727F, 190.2872F),
                    localScale = new Vector3(0.125F, 0.125F, 0.125F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(0.09427F, 0.17192F, 0.12581F),
                    localAngles = new Vector3(2.36075F, 23.61575F, 213.6566F),
                    localScale = new Vector3(0.1F, 0.1F, 0.1F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(-0.01339F, 0.30965F, 0.08011F),
                    localAngles = new Vector3(290.3721F, 87.95385F, 238.7799F),
                    localScale = new Vector3(0.1F, 0.1F, 0.1F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmL",
                    localPos = new Vector3(-0.02969F, 0.13737F, -0.05604F),
                    localAngles = new Vector3(4.11562F, 208.083F, 112.9552F),
                    localScale = new Vector3(0.1F, 0.1F, 0.1F)
                }
            });
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ClavicleL",
                    localPos = new Vector3(-0.2679F, 0.26969F, -0.41859F),
                    localAngles = new Vector3(82.89082F, 37.39894F, 114.1391F),
                    localScale = new Vector3(0.2F, 0.2F, 0.2F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ClavicleL",
                    localPos = new Vector3(-0.21587F, 0.0674F, -0.47721F),
                    localAngles = new Vector3(300.752F, 205.4889F, 330.8239F),
                    localScale = new Vector3(0.15F, 0.15F, 0.15F)
                }
            });
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(-0.02879F, 0.19696F, -0.05396F),
                    localAngles = new Vector3(11.34382F, 216.7536F, 68.44799F),
                    localScale = new Vector3(0.1F, 0.1F, 0.1F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(0.07723F, 0.2307F, 0.01101F),
                    localAngles = new Vector3(73.48827F, 174.5852F, 68.18921F),
                    localScale = new Vector3(0.095F, 0.095F, 0.095F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmL",
                    localPos = new Vector3(-0.02505F, 0.12124F, 0.04146F),
                    localAngles = new Vector3(56.26754F, 236.1188F, 216.1578F),
                    localScale = new Vector3(0.085F, 0.085F, 0.085F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmL",
                    localPos = new Vector3(0.03084F, 0.15522F, -0.05163F),
                    localAngles = new Vector3(16.44967F, 268.6152F, 165.7163F),
                    localScale = new Vector3(0.085F, 0.085F, 0.085F)
                }
            });
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(-1.14985F, 1.07899F, 1.07231F),
                    localAngles = new Vector3(7.14667F, 56.14653F, 166.3786F),
                    localScale = new Vector3(0.8F, 0.8F, 0.8F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(0.05578F, 2.20736F, 1.17745F),
                    localAngles = new Vector3(68.18668F, 86.93457F, 350.1264F),
                    localScale = new Vector3(0.6F, 0.6F, 0.6F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(-0.94171F, 2.95327F, -0.49731F),
                    localAngles = new Vector3(16.09822F, 285.5328F, 61.17276F),
                    localScale = new Vector3(0.8F, 0.8F, 0.8F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(-0.56295F, 1.05344F, -1.21502F),
                    localAngles = new Vector3(291.1883F, 262.609F, 209.3737F),
                    localScale = new Vector3(0.7F, 0.7F, 0.7F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(-1.60764F, 0.0609F, 0.10433F),
                    localAngles = new Vector3(298.7346F, 294.2642F, 152.4184F),
                    localScale = new Vector3(0.9F, 0.9F, 0.9F)
                }
            });
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(0.13782F, 0.10748F, 0.00096F),
                    localAngles = new Vector3(358.9753F, 164.1295F, 82.6639F),
                    localScale = new Vector3(0.115F, 0.115F, 0.115F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(0.00559F, 0.15013F, -0.10298F),
                    localAngles = new Vector3(52.27562F, 263.8302F, 179.5048F),
                    localScale = new Vector3(0.1F, 0.1F, 0.1F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ClavicleL",
                    localPos = new Vector3(0.06155F, 0.2221F, -0.19381F),
                    localAngles = new Vector3(21.18334F, 317.7809F, 241.8752F),
                    localScale = new Vector3(0.09F, 0.09F, 0.09F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(-0.12687F, 0.2532F, 0.00001F),
                    localAngles = new Vector3(305.3853F, 348.1187F, 83.4795F),
                    localScale = new Vector3(0.115F, 0.115F, 0.115F)
                }
            });
            rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(-0.00268F, 0.13079F, 0.05398F),
                    localAngles = new Vector3(344.431F, 88.94653F, 169.9597F),
                    localScale = new Vector3(0.08F, 0.08F, 0.08F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(0.03427F, 0.23088F, 0.02999F),
                    localAngles = new Vector3(4.90808F, 94.87518F, 15.17073F),
                    localScale = new Vector3(0.06F, 0.06F, 0.06F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Backpack",
                    localPos = new Vector3(-0.19333F, 0.26408F, 0.14482F),
                    localAngles = new Vector3(351.0388F, 353.7107F, 19.23048F),
                    localScale = new Vector3(0.08F, 0.08F, 0.08F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmL",
                    localPos = new Vector3(0.03851F, 0.07709F, 0.00584F),
                    localAngles = new Vector3(12.32488F, 81.77503F, 357.7062F),
                    localScale = new Vector3(0.04F, 0.04F, 0.04F)
                }
            });
            rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(0.02674F, 0.25116F, -0.06141F),
                    localAngles = new Vector3(338.2305F, 134.6885F, 147.2895F),
                    localScale = new Vector3(0.075F, 0.075F, 0.075F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(0.11404F, 0.11637F, -0.02319F),
                    localAngles = new Vector3(343.0203F, 186.139F, 85.96654F),
                    localScale = new Vector3(0.075F, 0.075F, 0.075F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(-0.02789F, 0.11207F, 0.09168F),
                    localAngles = new Vector3(353.486F, 347.7922F, 203.7489F),
                    localScale = new Vector3(0.075F, 0.075F, 0.075F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ForeArmL",
                    localPos = new Vector3(-0.02589F, 0.10316F, 0.09087F),
                    localAngles = new Vector3(3.01333F, 344.5153F, 152.3761F),
                    localScale = new Vector3(0.075F, 0.075F, 0.075F)
                }
            });
            rules.Add("mdlSeeker", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmL",
                    localPos = new Vector3(0.04638F, 0.10998F, -0.00353F),
                    localAngles = new Vector3(339.6131F, 0.05956F, 359.1189F),
                    localScale = new Vector3(0.095F, 0.095F, 0.095F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(-0.02921F, 0.24584F, -0.02023F),
                    localAngles = new Vector3(344.4747F, 283.7318F, 178.2722F),
                    localScale = new Vector3(0.095F, 0.095F, 0.095F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(0.04234F, 0.03658F, -0.05114F),
                    localAngles = new Vector3(301.0244F, 39.3749F, 6.70467F),
                    localScale = new Vector3(0.08F, 0.08F, 0.08F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(0.06768F, 0.13859F, 0.02601F),
                    localAngles = new Vector3(283.4507F, 12.57916F, 183.4329F),
                    localScale = new Vector3(0.06F, 0.06F, 0.06F)
                }
            });
            rules.Add("mdlChef", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(-0.03961F, 0.04705F, -0.04035F),
                    localAngles = new Vector3(76.79949F, 347.1697F, 89.22562F),
                    localScale = new Vector3(0.08F, 0.08F, 0.08F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmL",
                    localPos = new Vector3(0.07338F, -0.0517F, 0.00144F),
                    localAngles = new Vector3(80.67005F, 5.52209F, 107.935F),
                    localScale = new Vector3(0.08F, 0.08F, 0.08F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(0.163F, 0.00551F, 0.04729F),
                    localAngles = new Vector3(68.19553F, 294.1007F, 20.23775F),
                    localScale = new Vector3(0.08F, 0.08F, 0.08F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmL",
                    localPos = new Vector3(0.3094F, -0.01941F, -0.23793F),
                    localAngles = new Vector3(44.7071F, 10.25678F, 148.7954F),
                    localScale = new Vector3(0.06F, 0.06F, 0.06F)
                }
            });
            rules.Add("mdlFalseSon", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(0.12798F, 0.21855F, -0.03702F),
                    localAngles = new Vector3(357.5815F, 218.2983F, 176.6901F),
                    localScale = new Vector3(0.15F, 0.15F, 0.15F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(-0.03311F, 0.09891F, -0.12537F),
                    localAngles = new Vector3(28.33201F, 276.9496F, 357.165F),
                    localScale = new Vector3(0.1F, 0.1F, 0.1F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(-0.13396F, 0.30026F, -0.01178F),
                    localAngles = new Vector3(352.822F, 95.60686F, 26.6053F),
                    localScale = new Vector3(0.175F, 0.175F, 0.175F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(0.02462F, 0.43308F, -0.0947F),
                    localAngles = new Vector3(337.3108F, 244.002F, 27.51051F),
                    localScale = new Vector3(0.085F, 0.085F, 0.085F)
                }
            });
            rules.Add("mdlScav", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(-0.60854F, 1.05444F, 1.30963F),
                    localAngles = new Vector3(345.5005F, 45.90775F, 181.4016F),
                    localScale = new Vector3(2.25F, 2.25F, 2.25F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(0.71505F, 3.82353F, 1.17874F),
                    localAngles = new Vector3(8.39063F, 266.299F, 11.57934F),
                    localScale = new Vector3(1.5F, 1.5F, 1.5F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(-0.72679F, 5.38846F, -0.15173F),
                    localAngles = new Vector3(300.3888F, 14.89523F, 344.2299F),
                    localScale = new Vector3(1.15F, 1.15F, 1.15F)
                }
            });

            //Modded Chars 
            //rules.Add("EnforcerBody", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName =  "Shield",
            //        localPos =   new Vector3(0.1429525f, 0.009444445f, -0.231735f),
            //        localAngles = new Vector3(0.949871f, 227.3962f, 30.76947f),
            //        localScale = new Vector3(0.085f, 0.085f, 0.085f)
            //    }
            //});
            //rules.Add("NemesisEnforcerBody", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Chest",
            //        localPos = new Vector3(-0.001040328f, 0.0004106277f, 0.001044341f),
            //        localAngles = new Vector3(350.0445f, 351.373f, 112.076f),
            //        localScale = new Vector3(0.003f, 0.004f, 0.0035f)
            //    }
            //});
            //rules.Add("mdlPaladin", new RoR2.ItemDisplayRule[] //these ones don't work for some reason!
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Chest",
            //        localPos = new Vector3(0.2842848f, -0.1576135f, 0.01475417f),
            //        localAngles = new Vector3(4.996761f, 302.908f, 315.8754f),
            //        localScale = new Vector3(0.1f, 0.1f, 0.1f)
            //    }
            //});
            //rules.Add("mdlCHEF", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Door",
            //        localPos = new Vector3(0F, 0.00347F, -0.00126F),
            //        localAngles = new Vector3(0F, 90F, 0F),
            //        localScale = new Vector3(0.01241F, 0.01241F, 0.01241F)
            //    }
            //});
            //rules.Add("mdlMiner", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "PickL",
            //        localPos = new Vector3(-0.003641347f, 0.001164402f, 0.000302475f),
            //        localAngles = new Vector3(352.0699f, 17.21215f, 12.00122f),
            //        localScale = new Vector3(0.001f, 0.001f, 0.001f)
            //    }
            //});
            //rules.Add("mdlSniper", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "AntennaL",
            //        localPos = new Vector3(0.001760989f, 0.64437f, -0.01953437f),
            //        localAngles = new Vector3(357.1667f, 183.6886f, 21.44564f),
            //        localScale = new Vector3(.1f, .1f, .1f)
            //    },
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "AntennaR",
            //        localPos = new Vector3(-0.005021699f, 0.6448061f, -0.01700162f),
            //        localAngles = new Vector3(355.0474f, 8.001678f, 17.49146f),
            //        localScale = new Vector3(.1f, .1f, .1f)
            //    },
            //});
            //rules.Add("DancerBody", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Pelvis",
            //        localPos = new Vector3(-0.1678362f, 0.2800805f, -0.1426394f),
            //        localAngles = new Vector3(5.870443f, 265.1015f, 331.878f),
            //        localScale = new Vector3(0.07f, 0.07f, 0.07f)
            //    }
            //});
            //rules.Add("JavangleMystBody", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "UpperTorso",
            //        localPos = new Vector3(-0.0004795195f, 0.03674114f, -0.1753576f),
            //        localAngles = new Vector3(4.924208f, 197.0649f, 346.5102f),
            //        localScale = new Vector3(0.075f, 0.075f, 0.075f)
            //    }
            //});
            //rules.Add("mdlExecutioner", new RoR2.ItemDisplayRule[]
            //
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Chest",
            //        localPos = new Vector3(-0.002098578f, -0.0005844539f, 0.0005783288f),
            //        localAngles = new Vector3(3.540956f, 305.3824f, 5.553184f),
            //        localScale = new Vector3(0.00035f, 0.00035f, 0.00035f)
            //    }
            //
            //rules.Add("mdlNemmando", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Chest",
            //        localPos = new Vector3(-0.002061183f, -0.0009356125f, 0.0005527574f),
            //        localAngles = new Vector3(0.4865296f, 272.5422f, 17.22349f),
            //        localScale = new Vector3(0.00035f, 0.00035f, 0.00035f)
            //    }
            //});
            //rules.Add("mdlDeputy", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "CalfR",
            //        localPos = new Vector3(-0.06148292f, 0.2916591f, -0.001293976f),
            //        localAngles = new Vector3(357.6156f, 101.2189f, 24.66546f),
            //        localScale = new Vector3(.05f, .05f, .05f)
            //    }
            //});
            //rules.Add("mdlPathfinder", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "ShaftBone",
            //        localPos = new Vector3(0.01210994f, -0.6902985f, -0.0345431f),
            //        localAngles = new Vector3(4.054749f, 1.397443f, 199.0344f),
            //        localScale = new Vector3(.07f, .07f, .07f)
            //    }
            //});
            //rules.Add("mdlHANDOverclocked", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Pelvis",
            //        localPos = new Vector3(0.1192084f, 0.7092713f, 0.783208f),
            //        localAngles = new Vector3(0.006802655f, 4.527812f, 296.3758f),
            //        localScale = new Vector3(.25f, .25f, .25f)
            //    }
            //});
            //rules.Add("mdlRocket", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Chest",
            //        localPos = new Vector3(-0.1326108f, -0.4136848f, -0.00008926541f),
            //        localAngles = new Vector3(8.369913f, 264.0656f, 113.7217f),
            //        localScale = new Vector3(.07f, .07f, .07f)
            //    }
            //});
            ////rules.Add("mdlDaredevil", new RoR2.ItemDisplayRule[]
            ////{
            ////    new RoR2.ItemDisplayRule
            ////    {
            ////        ruleType = ItemDisplayRuleType.ParentedPrefab,
            ////        followerPrefab = ItemBodyModelPrefab,
            ////        childName = "Pelvis",
            ////        localPos = new Vector3(0, 0, 0),
            ////        localAngles = new Vector3(0, 0, 0),
            ////        localScale = new Vector3(1, 1, 1)
            ////    }
            ////});
            //rules.Add("mdlRMOR", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Pelvis",
            //        localPos = new Vector3(0.03966F, -0.03058F, 0.50333F),
            //        localAngles = new Vector3(8.94536F, 4.4423F, 294.5855F),
            //        localScale = new Vector3(0.25F, 0.25F, 0.25F)
            //
            //    }
            //});
            ////rules.Add("Spearman", new RoR2.ItemDisplayRule[]
            ////{
            ////    new RoR2.ItemDisplayRule
            ////    {
            ////        ruleType = ItemDisplayRuleType.ParentedPrefab,
            ////        followerPrefab = ItemBodyModelPrefab,
            ////        childName = "chest",
            ////        localPos = new Vector3(-0.00024F, 0.0037F, -0.01021F),
            ////        localAngles = new Vector3(340.1255F, 350.399F, 26.45361F),
            ////        localScale = new Vector3(0.00313F, 0.00313F, 0.00313F)
            ////    }
            ////});
            //rules.Add("mdlAssassin", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "arm_bone2.L",
            //        localPos = new Vector3(0.11975F, 0.45131F, -0.05435F),
            //        localAngles = new Vector3(358.8266F, 275.5731F, 23.64475F),
            //        localScale = new Vector3(0.08F, 0.08F, 0.08F)
            //    }
            //});
            //
            //rules.Add("mdlExecutioner2", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Pelvis",
            //        localPos = new Vector3(-0.18266F, -0.01516F, -0.01783F),
            //        localAngles = new Vector3(10.3122F, 262.8282F, 195.2796F),
            //        localScale = new Vector3(0.04F, 0.04F, 0.04F)
            //    }
            //});
            //rules.Add("mdlNemCommando", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Chest",
            //        localPos = new Vector3(-1.19996F, 0.90711F, -1.12132F),
            //        localAngles = new Vector3(357.6834F, 281.3561F, 359.9238F),
            //        localScale = new Vector3(0.3F, 0.3F, 0.3F)
            //    }
            //});
            //rules.Add("mdlNemMerc", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "UpperLegR",
            //        localPos = new Vector3(-0.0524275f, 0.2012022f, 0.1319876f),
            //        localAngles = new Vector3(1.080175f, 320.7132f, 198.739f),
            //        localScale = new Vector3(.05f, .05f, .05f)
            //    }
            //});
            //rules.Add("mdlChirr", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "LowerArmL",
            //        localPos = new Vector3(0.11695F, 0.27295F, -0.2554F),
            //        localAngles = new Vector3(9.18332F, 342.9477F, 200.6805F),
            //        localScale = new Vector3(0.15F, 0.15F, 0.15F)
            //    }
            //});
            //rules.Add("RobDriverBody", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Chest",
            //        localPos = new Vector3(0.0168156f, 0.2143276f, -0.1997456f),
            //        localAngles = new Vector3(345.8327f, 0.3895659f, 24.89436f),
            //        localScale = new Vector3(.08f, .08f, .08f)
            //    }
            //});
            //rules.Add("mdlTeslaTrooper", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Head",
            //        localPos = new Vector3(-0.38881F, 0.00609F, -0.00278F),
            //        localAngles = new Vector3(8.12494F, 8.49186F, 323.0006F),
            //        localScale = new Vector3(0.075F, 0.075F, 0.075F)
            //    }
            //});
            //rules.Add("mdlDesolator", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Head",
            //        localPos = new Vector3(-0.34154F, 0.10486F, 0.29819F),
            //        localAngles = new Vector3(344.7588F, 34.78439F, 316.3205F),
            //        localScale = new Vector3(0.075F, 0.075F, 0.075F)
            //    }
            //});
            //rules.Add("mdlArsonist", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Chest",
            //        localPos = new Vector3(0.23592F, 0.28636F, -0.43217F),
            //        localAngles = new Vector3(359.7161F, 227.8724F, 10.74877F),
            //        localScale = new Vector3(0.05F, 0.05F, 0.05F)
            //    }
            //});
            return rules;
        }

        public override void Hooks(){ }
    }

    public class HiveBehavior : BaseItemBodyBehavior, IOnDamageDealtServerReceiver, IOnIncomingDamageServerReceiver {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        private static ItemDef GetItemDef() { return ItemBase<Razorhive>.instance?.ItemDef; }

        public GameObject aura;
        public float atkSpeed;
        public float duration;
        private float timer = 0;
        public DamageAPI.ModdedDamageType damageType;
        public BuffDef buff;
        public GameObject indicator;

        public RazorhiveNetBehavior netb;
        bool active = false;

        private void OnEnable() {
            active = false;
            aura = UnityEngine.Object.Instantiate<GameObject>(Razorhive.beesAura, body.corePosition, Quaternion.identity);
            aura.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(body.gameObject, null);

            netb = aura.GetComponent<RazorhiveNetBehavior>();
            netb.RpcSetActive(false);

            //Debug.Log("Game Bar Enable ");

            var hc = body.healthComponent;
            if (hc && Array.IndexOf(hc.onIncomingDamageReceivers, this) < 0)
            {
                ArrayUtils.ArrayAppend(ref hc.onIncomingDamageReceivers, this);
                //Debug.Log("appended");
            }
            //body.healthComponent.TakeDamage
        }

        public void FixedUpdate(){
            if (active){
                duration -= Time.deltaTime;
                timer += Time.deltaTime;
                if (duration >= 0 && timer >= ItemBase<Razorhive>.instance.frequency.Value / (ItemBase<Razorhive>.instance.atkScale.Value ? body.attackSpeed : 1)) {
                    timer = 0;
                    Vector3 pos = body.corePosition;
                    TeamIndex teamInd = body.teamComponent.teamIndex;
                    HurtBox[] hurtBoxes = new SphereSearch {
                        origin = pos,
                        radius = ItemBase<Razorhive>.instance.baseRange.Value + (ItemBase<Razorhive>.instance.stackingRange.Value * (stack - 1)),
                        mask = LayerIndex.entityPrecise.mask,
                        queryTriggerInteraction = QueryTriggerInteraction.UseGlobal
                    }.RefreshCandidates().FilterCandidatesByHurtBoxTeam(TeamMask.GetEnemyTeams(teamInd)).FilterCandidatesByDistinctHurtBoxEntities().GetHurtBoxes();

                    int totalHits = ItemBase<Razorhive>.instance.baseTargets.Value + (ItemBase<Razorhive>.instance.stackingTargets.Value * (stack - 1));
                    //Debug.Log("bees: " + totalHits + " | " + hurtBoxes.Length + " | " + stack);

                    if (totalHits > hurtBoxes.Length){
                        totalHits = hurtBoxes.Length;
                    }

                    for (int i = 0; i < totalHits; ++i){
                        HiveOrb lightningOrb = new HiveOrb();
                        lightningOrb.attacker = base.gameObject;
                        lightningOrb.damageColorIndex = DamageColorIndex.Item;
                        lightningOrb.damageValue = body.damage * (ItemBase<Razorhive>.instance.baseDamage.Value + (ItemBase<Razorhive>.instance.stackingDamage.Value * (stack - 1)));
                        lightningOrb.isCrit = body.RollCrit();
                        lightningOrb.origin = pos;
                        lightningOrb.procChainMask = default(ProcChainMask);
                        lightningOrb.procChainMask.AddProc(ProcType.Thorns);
                        lightningOrb.procCoefficient = (body.isPlayerControlled ? ItemBase<Razorhive>.instance.procCoeff.Value : 0);
                        lightningOrb.teamIndex = teamInd;
                        lightningOrb.target = hurtBoxes[i];

                        OrbManager.instance.AddOrb(lightningOrb);
                    }
                }
                else if(duration < 0)
                {
                    active = false;
                    body.RemoveBuff(Razorhive.beesActive);
                    //Destroy(aura);
                    //aura = null;
                    netb.RpcSetActive(false);
                    timer = 0;
                }
            }      
        }

        private void OnDisable(){
            var hc = body.healthComponent;
            if (hc && Array.IndexOf(hc.onIncomingDamageReceivers, this) is var index && index >= 0)
            {
                ArrayUtils.ArrayRemoveAtAndResize(ref hc.onIncomingDamageReceivers, index);
                //Debug.Log("reshmoved");
            }
            if (body.HasBuff(Razorhive.beesActive)) { body.RemoveBuff(Razorhive.beesActive); }
            netb.RpcSetActive(false);
            active = false;
            Destroy(aura);
            aura = null;
            //Debug.Log("Death");
        }

        public void OnDamageDealtServer(DamageReport damageReport){
            //Debug.Log("danage dealt");
            if (damageReport.damageInfo.HasModdedDamageType(Razorhive.BeesType)){
                if (!body.HasBuff(Razorhive.beesActive)) { body.AddBuff(Razorhive.beesActive); }
                //if (!aura){
                //    aura = UnityEngine.Object.Instantiate<GameObject>(Razorhive.beesAura, body.corePosition, Quaternion.identity);
                //    aura.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(body.gameObject, null);
                //}
                var scale = (ItemBase<Razorhive>.instance.baseRange.Value + (ItemBase<Razorhive>.instance.stackingRange.Value * (stack - 1))) / 13;
                aura.transform.localScale = new Vector3(scale, scale, scale);
                active = true;
                duration = Razorhive.instance.baseDuration.Value + (Razorhive.instance.stackingDuration.Value * (stack - 1));
                //Debug.Log("refreshed via damage");
                netb.RpcSetActive(true);
            }
        }

        public void OnIncomingDamageServer(DamageInfo damageInfo)
        {
            //Debug.Log("OnIncomingDamageServer");
            if (!body.HasBuff(Razorhive.beesActive)) { body.AddBuff(Razorhive.beesActive); }
            //if (!aura){
            //    aura = UnityEngine.Object.Instantiate<GameObject>(Razorhive.beesAura, body.corePosition, Quaternion.identity);
            //    aura.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(body.gameObject, null);
            //}
            
            var scale = (ItemBase<Razorhive>.instance.baseRange.Value + (ItemBase<Razorhive>.instance.stackingRange.Value * (stack - 1))) / 13;
            aura.transform.localScale = new Vector3(scale, scale, scale);
            active = true;
            duration = Razorhive.instance.baseDuration.Value + (Razorhive.instance.stackingDuration.Value * (stack - 1));
            //Debug.Log("recieved");
            netb.RpcSetActive(true);
        }
    }


    //public class BeesToken : MonoBehaviour {
    //    public int itemCount;
    //    public float atkSpeed;
    //    public float duration;
    //    private float timer = 0;
    //    public CharacterBody body;
    //    public DamageAPI.ModdedDamageType damageType;
    //    public BuffDef buff;
    //    public GameObject indicator;
    //
    //    public void FixedUpdate() {
    //        duration -= Time.deltaTime;
    //        if (duration <= -.5f) { //let a late projectile refresh itself 
    //            body.RemoveBuff(buff);
    //            Destroy(indicator);
    //            Destroy(this);
    //        }
    //        //ItemBase<Bees>.instance.atkScale.Value
    //        timer += Time.deltaTime;
    //        if (duration >= 0 && timer >= ItemBase<Razorhive>.instance.frequency.Value / (ItemBase<Razorhive>.instance.atkScale.Value ? body.attackSpeed : 1)) {
    //            itemCount = body.inventory.GetItemCount(ItemBase<Razorhive>.instance.ItemDef);
    //            timer = 0;
    //            Vector3 pos = body.corePosition;
    //            TeamIndex teamInd = body.teamComponent.teamIndex;
    //            HurtBox[] hurtBoxes = new SphereSearch {
    //                origin = pos,
    //                radius = ItemBase<Razorhive>.instance.baseRange.Value + (ItemBase<Razorhive>.instance.stackingRange.Value * (itemCount - 1)),
    //                mask = LayerIndex.entityPrecise.mask,
    //                queryTriggerInteraction = QueryTriggerInteraction.UseGlobal
    //            }.RefreshCandidates().FilterCandidatesByHurtBoxTeam(TeamMask.GetEnemyTeams(teamInd)).FilterCandidatesByDistinctHurtBoxEntities().GetHurtBoxes();
    //
    //            int totalHits = ItemBase<Razorhive>.instance.baseTargets.Value + (ItemBase<Razorhive>.instance.stackingTargets.Value * (itemCount - 1));
    //            Debug.Log("bees: " + totalHits + " | " + hurtBoxes.Length  + " | " + itemCount);
    //            
    //            if (totalHits > hurtBoxes.Length){
    //                totalHits = hurtBoxes.Length;
    //            }
    //
    //
    //            for (int i = 0; i < totalHits; ++i)
    //            {
    //                HiveOrb lightningOrb = new HiveOrb();
    //                lightningOrb.attacker = base.gameObject;
    //                lightningOrb.damageColorIndex = DamageColorIndex.Item;
    //                lightningOrb.damageValue = body.damage * (ItemBase<Razorhive>.instance.baseDamage.Value + (ItemBase<Razorhive>.instance.stackingDamage.Value * (itemCount - 1)));
    //                lightningOrb.isCrit = body.RollCrit();
    //                lightningOrb.origin = pos;
    //                lightningOrb.procChainMask = default(ProcChainMask);
    //                lightningOrb.procChainMask.AddProc(ProcType.Thorns);
    //                lightningOrb.procCoefficient = (body.isPlayerControlled ? ItemBase<Razorhive>.instance.procCoeff.Value : 0);
    //                lightningOrb.teamIndex = teamInd;
    //                lightningOrb.target = hurtBoxes[i];
    //
    //                OrbManager.instance.AddOrb(lightningOrb);
    //
    //            }
    //        }
    //    }
    //}

    public class HiveOrb : GenericDamageOrb
    {
        public override void Begin()
        {
            this.speed = 75f;
            this.damageType.AddModdedDamageType(Razorhive.BeesType);
            base.Begin();
        }

        public override GameObject GetOrbEffect()
        { 
            return Razorhive.beesOrb; // Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Thorns/RazorwireOrbEffect.prefab").WaitForCompletion(); //Razorhive.beesOrb;
        }
    }

    public class RazorhiveNetBehavior : NetworkBehaviour
    {

        [ClientRpc]
        public void RpcSetActive(bool b)
        {
            gameObject.SetActive(b);
        }

    }
}


