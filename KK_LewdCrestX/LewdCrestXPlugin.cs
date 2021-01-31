﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using ActionGame;
using ActionGame.Chara;
using ActionGame.Communication;
using BepInEx;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using HarmonyLib;
using Illusion.Extensions;
using JetBrains.Annotations;
using KK_Pregnancy;
using KKABMX.Core;
using KKAPI;
using KKAPI.Chara;
using KKAPI.MainGame;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using KKAPI.Studio;
using KKAPI.Utilities;
using KoiSkinOverlayX;
using StrayTech;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace KK_LewdCrestX
{
    public sealed class CrestInfo
    {
        private readonly AssetBundle _bundle;
        public readonly string Description;
        public readonly CrestType Id;
        public readonly string Name;
        private Texture2D _tex;

        public CrestInfo(string id, string name, string description, AssetBundle bundle)
        {
            Id = (CrestType)Enum.Parse(typeof(CrestType), id);
            Name = name;
            Description = description;
            _bundle = bundle;
        }

        public Texture2D GetTexture()
        {
            if (_tex == null && Id > CrestType.None)
            {
                _tex = _bundle.LoadAsset<Texture2D>(Id.ToString()) ??
                       throw new Exception("Crest tex asset not found - " + Id);
                Object.DontDestroyOnLoad(_tex);
            }

            return _tex;
        }
    }

    [BepInPlugin(GUID, "LewdCrestX", Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency(KKABMX_Core.GUID, "4.0")]
    [BepInDependency(KoiSkinOverlayMgr.GUID, "5.2")]
    [BepInDependency(PregnancyPlugin.GUID, PregnancyPlugin.Version)]
    public class LewdCrestXPlugin : BaseUnityPlugin
    {
        public const string GUID = "LewdCrestX";
        public const string Version = "1.0";

        internal static new ManualLogSource Logger;
        //private MakerText _descControl;
        private Harmony _hi;

        public static Dictionary<CrestType, CrestInfo> CrestInfos { get; } = new Dictionary<CrestType, CrestInfo>();

        private void OnDestroy()
        {
            _hi?.UnpatchSelf();
        }
        private void Start()
        {
            Logger = base.Logger;

            _hi = Harmony.CreateAndPatchAll(typeof(TalkHooks), GUID);
            _hi.PatchAll(typeof(HsceneHooks));

            ////var mat = new Material(Shader.Find("Standard"));
            ////ChaControl.rendBody.materials = ChaControl.rendBody.materials.Where(x => x != mat).AddItem(mat).ToArray();
            //
            //var resource = ResourceUtils.GetEmbeddedResource("crests");
            //var bundle = AssetBundle.LoadFromMemory(resource);
            //DontDestroyOnLoad(bundle);
            //var textAsset = bundle.LoadAsset<TextAsset>("crestinfo");
            //var infoText = textAsset.text;
            //Destroy(textAsset);
            //
            //var xd = XDocument.Parse(infoText);
            //// ReSharper disable PossibleNullReferenceException
            //var infoElements = xd.Root.Elements("Crest");
            //var crestInfos = infoElements
            //    .Where(x => bool.Parse(x.Element("Implemented").Value))
            //    .Select(x => new CrestInfo(
            //        x.Element("ID").Value,
            //        x.Element("Name").Value,
            //        x.Element("Description").Value,
            //        bundle));
            //// ReSharper restore PossibleNullReferenceException
            //foreach (var crestInfo in crestInfos)
            //{
            //    Logger.LogDebug("Added implemented crest - " + crestInfo.Id);
            //    CrestInfos.Add(crestInfo.Id, crestInfo);
            //}
            //
            //CharacterApi.RegisterExtraBehaviour<LewdCrestXController>(GUID);
            //
            //if (StudioAPI.InsideStudio)
            //{
            //    //todo
            //}
            //else
            //{
            //    MakerAPI.RegisterCustomSubCategories += MakerAPI_RegisterCustomSubCategories;
            //    MakerAPI.MakerFinishedLoading += MakerAPIOnMakerFinishedLoading;
            //}
        }

        //private void MakerAPIOnMakerFinishedLoading(object sender, EventArgs e)
        //{
        //    _descControl.ControlObject.GetComponent<LayoutElement>().minHeight = 80;
        //}
        //
        //private void MakerAPI_RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent e)
        //{
        //    var category = new MakerCategory(MakerConstants.Parameter.ADK.CategoryName, "Crest");
        //    e.AddSubCategory(category);
        //
        //    var infos = CrestInfos.Values.ToList();
        //    var crests = new[] { "None" }.Concat(infos.Select(x => x.Name)).ToArray();
        //
        //    var dropdown = e.AddControl(new MakerDropdown("Crest type", crests, category, 0, this));
        //    dropdown.BindToFunctionController<LewdCrestXController, int>(
        //        controller => infos.FindIndex(info => info.Id == controller.CurrentCrest) + 1,
        //        (controller, value) => controller.CurrentCrest = value <= 0 ? CrestType.None : infos[value - 1].Id);
        //
        //    _descControl = e.AddControl(new MakerText("Description", category, this));
        //    dropdown.ValueChanged.Subscribe(value => _descControl.Text = value <= 0 ? "No crest selected, no effects applied" : infos[value - 1].Description);
        //}


        //private static LewdCrestXController GetController(SaveData.Heroine heroine) => GetController(heroine?.chaCtrl);
        //private static LewdCrestXController GetController(ChaControl chaCtrl) => chaCtrl != null ? chaCtrl.GetComponent<LewdCrestXController>() : null;
        public static CrestType GetCurrentCrest(SaveData.Heroine heroine)
        {
            //var ctrl = GetController(heroine);
            //return ctrl == null ? CrestType.None : ctrl.CurrentCrest;
            return CrestType.command;
        }
    }

    internal static class HsceneHooks
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddAibuOrg))]
        [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuAnalOrg))]
        [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuOrg))]
        public static void OnOrg(HFlag __instance)
        {
            var h = __instance.GetLeadingHeroine();
            var c = LewdCrestXPlugin.GetCurrentCrest(h);
            switch (c)
            {
                case CrestType.mindmelt:
                    // This effect makes character slowly forget things on every org
                    h.favor = Mathf.Clamp(h.favor - 10, 0, 100);
                    h.intimacy = Mathf.Clamp(h.intimacy - 8, 0, 100);

                    h.anger = Mathf.Clamp(h.anger - 10, 0, 100);
                    if (h.anger == 0) h.isAnger = false;

                    if (Random.value < 0.2f) h.isDate = false;

                    // In exchange they get lewder
                    h.lewdness = Mathf.Clamp(h.lewdness + 30, 0, 100);

                    var orgCount = __instance.GetOrgCount();
                    if (orgCount >= 2)
                    {
                        if (h.favor == 0 && h.intimacy == 0)
                        {
                            h.isGirlfriend = false;
                            if (Random.value < 0.2f) h.confessed = false;
                        }

                        if (h.isKiss && Random.value < 0.1f) h.isKiss = false;
                        else if (!h.isAnalVirgin && Random.value < 0.1f) h.isAnalVirgin = true;
                        else if (Random.value < 0.3f + orgCount / 10f)
                        {
                            // Remove a random seen event so she acts like it never happened
                            var randomEvent = h.talkEvent.GetRandomElement();
                            var isMeetingEvent = randomEvent == 0 || randomEvent == 1;
                            if (isMeetingEvent)
                            {
                                if (h.talkEvent.Count <= 2)
                                    h.talkEvent.Clear();
                            }
                            else
                            {
                                h.talkEvent.Remove(randomEvent);
                            }
                        }
                    }

                    break;
            }
        }
    }

    internal static class TalkHooks
    {

        private static CrestType _currentCrestType;
        private static bool _isHEvent;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Info), nameof(Info.GetEventADV))]
        static void GetEventADVPrefix(Info __instance, int _command, PassingInfo ____passingInfo)
        {
            _currentCrestType = LewdCrestXPlugin.GetCurrentCrest(____passingInfo.heroine);
            Console.WriteLine("GetEventADVPrefix " + _currentCrestType);
            _isHEvent = _command == 3;
        }
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(Info), nameof(Info.GetEventADV))]
        static void GetEventADVFinalizer()
        {
            Console.WriteLine("GetEventADVFinalizer " + _currentCrestType);
            _currentCrestType = CrestType.None;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TalkScene), "UpdateUI")]
        //private void UpdateUI(bool _gauge = false)
        static void UpdateUIPrefix(TalkScene __instance)
        {
            _currentCrestType = LewdCrestXPlugin.GetCurrentCrest(__instance.targetHeroine);
            Console.WriteLine("UpdateUIPrefix " + _currentCrestType);
            _isHEvent = false;
        }

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(TalkScene), "UpdateUI")]
        static void UpdateUIFinalizer(TalkScene __instance, Button[] ___buttonEventContents)
        {
            Console.WriteLine("UpdateUIFinalizer " + _currentCrestType);
            if (_currentCrestType == CrestType.libido)
            {
                // 3 is lets have h
                ___buttonEventContents[3].gameObject.SetActiveIfDifferent(true);
            }

            _currentCrestType = CrestType.None;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Info), "GetStage")]
        //private int GetStage()
        static void GetStagePatch(ref int __result)
        {
            Console.WriteLine("GetStagePatch " + _currentCrestType);
            switch (_currentCrestType)
            {
                case CrestType.libido:
                    if (_isHEvent) __result = 2;
                    break;
                case CrestType.command:
                    if (__result == 0) __result = 1;
                    break;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Info), "RandomBranch")]
        //private int RandomBranch(params int[] _values)
        static void RandomBranchPatch(ref int __result)
        {
            Console.WriteLine("RandomBranchPatch " + _currentCrestType);
            switch (_currentCrestType)
            {
                case CrestType.libido:
                    if (_isHEvent) __result = 0;
                    break;
                case CrestType.command:
                    __result = 0;
                    break;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PassingInfo), "isHPossible", MethodType.Getter)]
        static void isHPossiblePatch(ref bool __result, PassingInfo __instance)
        {
            var crest = _currentCrestType;
            if (_currentCrestType == CrestType.None) crest = LewdCrestXPlugin.GetCurrentCrest(__instance.heroine);
            Console.WriteLine("isHPossiblePatch " + _currentCrestType);

            switch (crest)
            {
                case CrestType.command:
                case CrestType.libido:
                    __result = true;
                    break;
            }
        }
    }

    sealed class LewdCrestXBoneModifier : BoneEffect
    {
        private readonly LewdCrestXController _controller;

        private static readonly string[] _vibrancyBones;

        // todo might cause issues if in future abmx holds on to the bone modifiers since we reuse them for all characters
        private static readonly Dictionary<string, KeyValuePair<Vector3, BoneModifierData>> _vibrancyBoneModifiers;

        public LewdCrestXBoneModifier(LewdCrestXController controller)
        {
            _controller = controller;
        }

        static LewdCrestXBoneModifier()
        {
            var d = new Dictionary<string, Vector3>
            {
                {"cf_d_bust01_L", new Vector3(1.25f, 1.25f, 1.25f)},
                {"cf_d_bust01_R", new Vector3(1.25f, 1.25f, 1.25f)},
                {"cf_d_bnip01_L", new Vector3(1.1f, 1.1f, 1.1f)},
                {"cf_d_bnip01_R", new Vector3(1.1f, 1.1f, 1.1f)},
                {"cf_s_bnip02_L", new Vector3(1.2f, 1.2f, 1.2f)},
                {"cf_s_bnip02_R", new Vector3(1.2f, 1.2f, 1.2f)},
                {"cf_s_siri_L", new Vector3(1.2f, 1.2f, 1.2f)},
                {"cf_s_siri_R", new Vector3(1.2f, 1.2f, 1.2f)},
                {"cf_s_waist01", new Vector3(0.9f, 0.9f, 0.9f)},
                {"cf_s_waist02", new Vector3(1.1f, 1.1f, 1.1f)},
                {"cf_s_thigh01_L", new Vector3(1.1f, 1.1f, 1.1f)},
                {"cf_s_thigh01_R", new Vector3(1.1f, 1.1f, 1.1f)},
                {"cf_s_thigh02_L", new Vector3(1.1f, 1.1f, 1.1f)},
                {"cf_s_thigh02_R", new Vector3(1.1f, 1.1f, 1.1f)},
                {"cf_s_thigh03_L", new Vector3(1.1f, 1.1f, 1.1f)},
                {"cf_s_thigh03_R", new Vector3(1.1f, 1.1f, 1.1f)},
            };

            _vibrancyBones = d.Keys.ToArray();
            _vibrancyBoneModifiers = d.ToDictionary(x => x.Key, x => new KeyValuePair<Vector3, BoneModifierData>(x.Value, new BoneModifierData(x.Value, 1)));
        }

        public override IEnumerable<string> GetAffectedBones(BoneController origin)
        {
            return _controller.CurrentCrest == CrestType.vibrancy && _controller.Heroine != null && _controller.Heroine.lewdness > 0 ? _vibrancyBones : Enumerable.Empty<string>();
        }

        private float _previousRatio;

        public override BoneModifierData GetEffect(string bone, BoneController origin, ChaFileDefine.CoordinateType coordinate)
        {
            if (_controller.CurrentCrest == CrestType.vibrancy)
            {
                if (_vibrancyBoneModifiers.TryGetValue(bone, out var kvp))
                {
                    var modifier = kvp.Value;

                    if (_controller.Heroine != null)
                    {
                        // Effect increases the lewder the character is
                        var ratio = _controller.Heroine.lewdness / 120f + (int)_controller.Heroine.HExperience * 0.1f;
                        if (ratio != _previousRatio)
                        {
                            ratio = Mathf.MoveTowards(_previousRatio, ratio, Time.deltaTime / 10);
                            _previousRatio = ratio;
                        }
                        modifier.ScaleModifier = Vector3.Lerp(Vector3.one, kvp.Key, ratio);
                    }
                    else
                    {
                        // If outside of main game always set to max
                        modifier.ScaleModifier = kvp.Key;
                    }

                    return modifier;
                }
            }
            return null;
        }
    }

    public class LewdCrestXController : CharaCustomFunctionController
    {
        private CrestType _currentCrest;
        private KoiSkinOverlayController _overlayCtrl;
        private BoneController _boneCtrl;

        internal SaveData.Heroine Heroine { get; private set; }
        //private NPC _npc;

        public CrestType CurrentCrest
        {
            get => _currentCrest;
            set
            {
                if (_currentCrest != value)
                {
                    _currentCrest = value;
                    ApplyCrestTexture();
                }
            }
        }

        protected override void Start()
        {
            base.Start();
            _boneCtrl = GetComponent<BoneController>() ?? throw new Exception("Missing BoneController");
            _boneCtrl.AddBoneEffect(new LewdCrestXBoneModifier(this));
        }

        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
            // todo save null if default
            var data = new PluginData();
            data.data[nameof(CurrentCrest)] = CurrentCrest;
            SetExtendedData(data);
        }

        protected override void OnReload(GameMode currentGameMode)
        {
            // todo better handling
            var data = GetExtendedData()?.data;
            if (data != null && data.TryGetValue(nameof(CurrentCrest), out var cr))
            {
                try
                {
                    CurrentCrest = (CrestType)cr;
                }
                catch (Exception e)
                {
                    LewdCrestXPlugin.Logger.LogError(e);
                    CurrentCrest = CrestType.None;
                }
            }

            Heroine = ChaControl.GetHeroine();
            //if (_heroine != null) _npc = ChaControl.transform.parent?.GetComponent<NPC>(););
        }

        protected override void Update()
        {
            base.Update();

            // todo reduce run rate?
            if (CurrentCrest == CrestType.libido)
            {
                if (Heroine != null)
                {
                    Heroine.lewdness = 100;
                    var actCtrl = Manager.Game.Instance.actScene.actCtrl;
                    actCtrl.SetDesire(4, Heroine, 110); //want to mast
                    actCtrl.SetDesire(5, Heroine, 120); //want to h
                    actCtrl.SetDesire(26, Heroine, 100); //les
                    actCtrl.SetDesire(27, Heroine, 100); //les
                    actCtrl.SetDesire(29, Heroine, 150); //ask for h
                }
            }
        }

        private void ApplyCrestTexture()
        {
            if (_overlayCtrl == null)
                _overlayCtrl = GetComponent<KoiSkinOverlayController>() ?? throw new Exception("Missing KoiSkinOverlayController");

            var any = _overlayCtrl.AdditionalTextures.RemoveAll(texture => ReferenceEquals(texture.Tag, this)) > 0;

            if (CurrentCrest > CrestType.None)
            {
                if (LewdCrestXPlugin.CrestInfos.TryGetValue(CurrentCrest, out var info))
                {
                    var tex = new AdditionalTexture(info.GetTexture(), TexType.BodyOver, this, 1010);
                    _overlayCtrl.AdditionalTextures.Add(tex);
                    any = true;
                }
                else
                {
                    LewdCrestXPlugin.Logger.LogWarning($"Unknown crest type \"{CurrentCrest}\", resetting to no crest");
                    CurrentCrest = CrestType.None;
                }
            }

            if (any) _overlayCtrl.UpdateTexture(TexType.BodyOver);
        }
    }
}