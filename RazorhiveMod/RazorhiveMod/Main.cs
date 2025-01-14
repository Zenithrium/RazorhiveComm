using BepInEx;
using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

//using vanillaVoid.Equipment.EliteEquipment;
using RazorwireMod.Items;
using RoR2;
using HarmonyLib;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using RoR2.Projectile;
using RazorwireMod.Utils;
using MonoMod.Cil;
using RoR2.EntitlementManagement;
using On.RoR2.Items;
using RoR2.ContentManagement;
using AK.Wwise;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace RazorwireMod {
    [BepInPlugin(ModGuid, ModName, ModVer)]

    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.content_management", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.items", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.language", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.prefab", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.recalculatestats", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.director", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.networking", BepInDependency.DependencyFlags.HardDependency)]

    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    public class RazorwireModPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.Zenithrium.RazorhiveMod";
        public const string ModName = "RazorhiveMod";
        public const string ModVer = "1.0.0";

        public static ExpansionDef sotvDLC;
        public static ExpansionDef sotvDLC2;
        public static AssetBundle MainAssets;

        //public List<ArtifactBase> Artifacts = new List<ArtifactBase>();
        public List<ItemBase> Items = new List<ItemBase>();
        //public List<EquipmentBase> Equipments = new List<EquipmentBase>();
        //public List<InteractableBase> Interactables = new List<InteractableBase>();
        //public List<EliteEquipmentBase> EliteEquipments = new List<EliteEquipmentBase>();

        //Provides a direct access to this plugin's logger for use in any of your other classes.
        public static BepInEx.Logging.ManualLogSource ModLogger;

        public static List<ItemDef.Pair> corruptibleItems = new List<ItemDef.Pair>();

        public Xoroshiro128Plus genericRng;

        public static ConfigEntry<bool> lockVoidsBehindPair;

        private void Awake(){
            //orreryCompat = Config.Bind<bool>("Mod Compatability", "Enable Lost Seers Buff", true, "Should generally stay on, but if you're having a strange issue (ex. health bars not showing up on enemies) edit this to be false.");

            ModLogger = Logger;

            var harm = new Harmony(Info.Metadata.GUID);
            //new PatchClassProcessor(harm, typeof(ModdedDamageColors)).Patch();

            //sotvDLC = ExpansionCatalog.expansionDefs.FirstOrDefault(x => x.nameToken == "DLC1_NAME");  //learn what sotv is 

            //sotvDLC2 = LegacyResourcesAPI.Load<ExpansionDef>("ExpansionDefs/DLC1");
            sotvDLC = Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC1/Common/DLC1.asset").WaitForCompletion();
            //expansionDef.enabledChoice
            //EntitlementDef dlc1Entitlemnt = LegacyResourcesAPI.Load<EntitlementDef>("EntitlementDefs/entitlementDLC1");

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("RazorhiveMod.razorhiveassets"))
            {
                MainAssets = AssetBundle.LoadFromStream(stream);
            }
            Swapallshaders(MainAssets);

            On.RoR2.Items.ContagiousItemManager.Init += AddVoidItemsToDict;
            //On.RoR2.ItemCatalog.Init += AddUnlocksToVoidItems;

            //On.RoR2.Projectile.SlowDownProjectiles.OnTriggerEnter += fuck;

            //On.RoR2.ItemCatalog.Init += ItemCatalog_Init;

            //IL.RoR2.GenericSkill.RunRecharge += Ah;
            //On.RoR2.GenericSkill.RunRecharge += Ah2;

            On.RoR2.Language.GetLocalizedStringByToken += (orig, self, token) => {
                if (ItemBase.TokenToVoidPair.ContainsKey(token))
                {
                    ItemIndex idx = ItemCatalog.FindItemIndex(ItemBase.TokenToVoidPair[token]);
                    if (idx != ItemIndex.None) return orig(self, token).Replace("{CORRUPTION}", MiscUtils.GetPlural(orig(self, ItemCatalog.GetItemDef(idx).nameToken)));
                }
                return orig(self, token);
            };


            //var triple = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidTriple/VoidTriple.prefab").WaitForCompletion();
            //if (triple){
            //    var pi = triple.GetComponent<PurchaseInteraction>();
            //    if (pi){
            //        pi.saleStarCompatible =  true;
            //    }
            //}
            //bark.GetComponent<PurchaseInteraction>().saleStarCompatible = true;
            // Don't know how to create/use an asset bundle, or don't have a unity project set up?
            // Look here for info on how to set these up: https://github.com/KomradeSpectre/AetheriumMod/blob/rewrite-master/Tutorials/Item%20Mod%20Creation.md#unity-project


            //This section automatically scans the project for all artifacts
            //var ArtifactTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(ArtifactBase)));
            //
            //foreach (var artifactType in ArtifactTypes)
            //{
            //    ArtifactBase artifact = (ArtifactBase)Activator.CreateInstance(artifactType);
            //    if (ValidateArtifact(artifact, Artifacts))
            //    {
            //        artifact.Init(Config);
            //    }
            //}

            //var voidtier1def = ItemTierCatalog.GetItemTierDef(ItemTier.VoidTier1);
            //GameObject prefab = voidtier1def.highlightPrefab;


            //This section automatically scans the project for all items
            var ItemTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(ItemBase)));

            List<ItemDef.Pair> newVoidPairs = new List<ItemDef.Pair>();

            foreach (var itemType in ItemTypes){
                ItemBase item = (ItemBase)System.Activator.CreateInstance(itemType);
                if (ValidateItem(item, Items)){

                    item.Init(Config);

                    var tags = item.ItemTags;
                    bool aiValid = true;
                    bool aiBlacklist = false;
                    if (item.ItemDef.deprecatedTier == ItemTier.NoTier){
                        aiBlacklist = true;
                        aiValid = false;
                    }
                    string name = item.ItemName;
                    //Debug.Log("prename " + name);
                    name = name.Replace("'", "");
                    //Debug.Log("postname " + name);

                    foreach (var tag in tags){
                        if (tag == ItemTag.AIBlacklist){
                            aiBlacklist = true;
                            aiValid = false;
                            break;
                        }
                    }
                    if (aiValid){
                        aiBlacklist = Config.Bind<bool>("Item: " + name, "Blacklist Item from AI Use?", false, "Should the AI not be able to obtain this item?").Value;
                    }else{
                        aiBlacklist = true;
                    }

                    if (aiBlacklist){
                        item.AIBlacklisted = true;
                    }
                }
            }

            ///this section automatically scans the project for all elite equipment
            //var EliteEquipmentTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(EliteEquipmentBase)));
            //
            //foreach (var eliteEquipmentType in EliteEquipmentTypes)
            //{
            //    EliteEquipmentBase eliteEquipment = (EliteEquipmentBase)System.Activator.CreateInstance(eliteEquipmentType);
            //    if (ValidateEliteEquipment(eliteEquipment, EliteEquipments))
            //    {
            //        eliteEquipment.Init(Config);
            //
            //    }
            //}

        }

        private void AddUnlocksToVoidItems(On.RoR2.ItemCatalog.orig_Init orig)
        {
            orig();
            foreach (var voidpair in ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem])
            {
                //corruptibleItems.Add(voidpair);
                if (lockVoidsBehindPair.Value)
                {
                    if (voidpair.itemDef1.unlockableDef != null && voidpair.itemDef2.unlockableDef == null)
                    {
                        Debug.Log("Updating unlock condition for " + voidpair.itemDef2.nameToken + " to " + voidpair.itemDef1.nameToken + "'s.");
                        voidpair.itemDef2.unlockableDef = voidpair.itemDef1.unlockableDef;
                    }
                }
                //Debug.Log("voidpair: " + voidpair.itemDef1 + " | " + voidpair.itemDef2 + " | " + voidpair.ToString());

            }
        }

        private void AddVoidItemsToDict(ContagiousItemManager.orig_Init orig)
        {
            List<ItemDef.Pair> newVoidPairs = new List<ItemDef.Pair>();
            Debug.Log("Adding VanillaVoid item transformations...");
            foreach (var item in Items)
            {
                if (item.ItemDef.deprecatedTier != ItemTier.NoTier) //safe assumption i think
                {
                    //Debug.Log("adding pair " + item);
                    Debug.Log("Item Name: " + item.ItemName);
                    item.AddVoidPair(newVoidPairs);
                }
                else
                {
                    Debug.Log("Skipping " + item.ItemName);
                }
            }
            var key = DLC1Content.ItemRelationshipTypes.ContagiousItem;
            Debug.Log(key);
            var voidPairs = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem];
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = voidPairs.Union(newVoidPairs).ToArray();
            Debug.Log("Finishing appending Razorhive transformation");

            orig();

        }

        /// <summary>
        /// A helper to easily set up and initialize an artifact from your artifact classes if the user has it enabled in their configuration files.
        /// </summary>
        /// <param name="artifact">A new instance of an ArtifactBase class."</param>
        /// <param name="artifactList">The list you would like to add this to if it passes the config check.</param>
        //public bool ValidateArtifact(ArtifactBase artifact, List<ArtifactBase> artifactList)
        //{
        //    var enabled = Config.Bind<bool>("Artifact: " + artifact.ArtifactName, "Enable Artifact?", true, "Should this artifact appear for selection?").Value;
        //
        //    if (enabled)
        //    {
        //        artifactList.Add(artifact);
        //    }
        //    return enabled;
        //}

        /// <summary>
        /// A helper to easily set up and initialize an item from your item classes if the user has it enabled in their configuration files.
        /// <para>Additionally, it generates a configuration for each item to allow blacklisting it from AI.</para>
        /// </summary>
        /// <param name="item">A new instance of an ItemBase class."</param>
        /// <param name="itemList">The list you would like to add this to if it passes the config check.</param>
        /// string name = item.name == “your item name here” ? “Your item name here without apostrophe” : itemDef.name
        public bool ValidateItem(ItemBase item, List<ItemBase> itemList)
        {
            string name = item.ItemName.Replace("'", string.Empty);
            //string name = item.ItemName == "Lens-Maker's Orrery" ? "Lens-Makers Orrery" : item.ItemName;
            bool enabled = false;
            //if (name.Equals("Empty Vials") || name.Equals("Broken Mess"))
            //{
            //    //enabled = true; //override config option
            //    //aiBlacklist = true;
            //    Debug.Log("Disabling config for " + name);
            //}
            //else
            //{

            //Debug.Log("stats on: " + item.ItemName + " | tier: " + item.Tier + " | icon:" + item.ItemIcon + " | tags: " + item.ItemTags);


            if (item.Tier == ItemTier.NoTier)
            {
                enabled = true;
                item.AIBlacklisted = true;
                //aiBlacklist = true;
                //Debug.Log("Adding Broken Item: " + item.ItemName);
            }
            else
            {
                //Debug.Log("ignoring config for: " + item.ItemName);
                //enabled = true;
                //aiBlacklist = true;
                //Debug.Log("Adding Normal Item: " + item.ItemName);
                enabled = Config.Bind<bool>("Item: " + name, "Enable Item?", true, "Should this item appear in runs?").Value;
                //var tags = item.ItemTags;
                //bool aiValid = true;
                //foreach(var tag in tags)
                //{
                //    if(tag == ItemTag.AIBlacklist)
                //    {
                //        aiBlacklist = true;
                //        break;
                //    }
                //}
                //if (aiValid)
                //{
                //    aiBlacklist = Config.Bind<bool>("Item: " + name, "Blacklist Item from AI Use?", false, "Should the AI not be able to obtain this item?").Value;
                //}
                //else
                //{
                //    aiBlacklist = true;
                //}
                //aiBlacklist = Config.Bind<bool>("Item: " + name, "Blacklist Item from AI Use?", false, "Should the AI not be able to obtain this item?").Value;
            }

            //enabled = Config.Bind<bool>("Item: " + name, "Enable Item?", true, "Should this item appear in runs?").Value;
            //aiBlacklist = Config.Bind<bool>("Item: " + name, "Blacklist Item from AI Use?", false, "Should the AI not be able to obtain this item?").Value;

            //}
            //var enabled = Config.Bind<bool>("Item: " + name, "Enable Item?", true, "Should this item appear in runs?").Value;
            //var aiBlacklist = Config.Bind<bool>("Item: " + name, "Blacklist Item from AI Use?", false, "Should the AI not be able to obtain this item?").Value;

            if (enabled)
            {
                itemList.Add(item);
                //if (aiBlacklist)
                //{
                //    item.AIBlacklisted = true;
                //}
            }
            return enabled;
        }
       

        public void Swapallshaders(AssetBundle bundle)
        {
            //Debug.Log("beginning test");
            Material[] allMaterials = bundle.LoadAllAssets<Material>();
            foreach (Material mat in allMaterials)
            {
                //D//ebug.Log("material: " + mat.name + " | with shader: " + mat.shader.name);
                switch (mat.shader.name)
                {
                    case "Stubbed Hopoo Games/Deferred/Standard":
                        mat.shader = Resources.Load<Shader>("shaders/deferred/hgstandard");
                        break;
                    case "Stubbed Hopoo Games/Deferred/Snow Topped":
                        mat.shader = Resources.Load<Shader>("shaders/deferred/hgsnowtopped");
                        break;
                    case "Stubbed Hopoo Games/FX/Cloud Remap":
                        //Debug.Log("Switching material: " + mat.name);
                        mat.shader = Resources.Load<Shader>("shaders/fx/hgcloudremap");
                        //Debug.Log("Swapped: " + mat.shader);
                        break;

                    case "Stubbed Hopoo Games/FX/Cloud Intersection Remap":
                        mat.shader = Resources.Load<Shader>("shaders/fx/hgintersectioncloudremap");
                        break;
                    case "Stubbed Hopoo Games/FX/Opaque Cloud Remap":
                        //Debug.Log("Switching material: " + mat.name);
                        mat.shader = Resources.Load<Shader>("shaders/fx/hgopaquecloudremap");
                        //Debug.Log("Swapped: " + mat.shader);
                        break;
                    case "Stubbed Hopoo Games/FX/Distortion":
                        mat.shader = Resources.Load<Shader>("shaders/fx/hgdistortion");
                        break;
                    case "Stubbed Hopoo Games/FX/Solid Parallax":
                        mat.shader = Resources.Load<Shader>("shaders/fx/hgsolidparallax");
                        break;
                    case "Stubbed Hopoo Games/Environment/Distant Water":
                        mat.shader = Resources.Load<Shader>("shaders/environment/hgdistantwater");
                        break;
                    case "StubbedRoR2/Base/Shaders/HGCloudRemap":
                        //Debug.Log("Switching material: " + mat.name);
                        mat.shader = Resources.Load<Shader>("shaders/fx/hgcloudremap");
                        break;
                    default:
                        break;
                }

            }
        }



    }
}