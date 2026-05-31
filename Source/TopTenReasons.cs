using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using I2.Loc;
using MonsterLove.StateMachine;
using NineSolsAPI;
using NineSolsAPI.Utils;
using RCGMaker.Runtime.Character;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

namespace TopTenReasons;

[BepInDependency(NineSolsAPICore.PluginGUID)]
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class TopTenReasons : BaseUnityPlugin {

    public const string MURMUR_PREFAB_NAME = "a0_s1_intro_morning/gamelevel/room1/[timeline]a0_s1_npcdialogues/[timeline]murmurbubble00.prefab";
    public const string ASSET_BUNDLE_EMBEDDED_PATH = "TopTenReasons.Resources.nkvbundle.bundle";
    public const string YI_GUNG_NAME = "Boss_Yi Gung";

    public const string TRUE_ENDING_SCENE_NAME = "A11_S0_Boss_YiGung";
    public const string ENDING_SCENE_NAME = "A11_S0_Boss_YiGung_回蓬萊";

    public const string MURMUR_LINE = "Eigong/ReasonsPresent/TopTen";
    public const string MURMUR_LINE_CONTENT = "Top 10 reasons eigongs talisman is fair and you're stupid";

    private static readonly Dictionary<string, string> Languages = new() {
       { "zh-TW", "繁體中文 (Traditional Chinese) [zh-TW]" },

        { "zh-CN", "简体中文 (Simplified Chinese) [zh-CN]" },
        { "en-US", "English" },
        { "ko", "한국어 (Korean) [ko]" },
        { "ja", "日本語 (Japanese) [ja]" },
        { "es-US", "Spanish (Latin Americas)" },
        { "de-DE", "German (Germany)" },
        { "fr-FR", "French (France)" },
        { "ru", "Russian" },
        { "uk", "Ukrainian" },
        { "pt-BR", "Português - Brasil [pt-BR]" },
        { "pl", "Polski (Polish)" },
        { "es-ES", "Español - España (Spanish - Spain)" }

    };

    internal static TopTenReasons Instance = null!;
    internal GameObject? BubblePrefab = null;
    internal TimelineMurmur? MurmurTimeLine = null;

    private Harmony harmony = null!;

    private float _explosionTime = 1.8f;

    private void Awake() {
        try {
            Instance = this;

            Log.Init(Logger);
            RCGLifeCycle.DontDestroyForever(gameObject);

            harmony = Harmony.CreateAndPatchAll(typeof(TopTenReasons).Assembly);

            var bundle = AssemblyUtils.GetEmbeddedAssetBundle(ASSET_BUNDLE_EMBEDDED_PATH);
            BubblePrefab = (GameObject?)bundle?.LoadAsset(MURMUR_PREFAB_NAME);

            if(BubblePrefab != null) {
                BubblePrefab.SetActive(false);
                BubblePrefab.transform.localPosition = Vector3.zero;
                var cullingGroup = BubblePrefab.GetComponent<RCGCullingGroup>();
                GameObject.Destroy(cullingGroup);
                var murmurComponent = BubblePrefab.GetComponent<TimelineMurmur>();
                murmurComponent.isPlayed = null;
            }

            AddLocalization();

            MonsterBasePatches.OnCheckInitPostfix += HandleMurmurCreation;

            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        } catch(Exception ex) {
            Log.Exception(ex);
        }
    }

    private void HandleMurmurCreation(MonsterBase monster) {
        try {
            var objectScene = monster.gameObject.scene;

            if (objectScene.name != TRUE_ENDING_SCENE_NAME && objectScene.name != ENDING_SCENE_NAME && monster.name != YI_GUNG_NAME) return;

            InitMurmur(monster.gameObject);
        
            var stateLookupFieldInfo = AccessTools.Field(typeof(StateMachine<MonsterBase.States>), "stateLookup");

            var stateLookup = (Dictionary<MonsterBase.States, StateMapping<MonsterBase.States>>)stateLookupFieldInfo.GetValue(monster.fsm);
            var chargeFooMapping = stateLookup[MonsterBase.States.Attack10];
            var quickFooMapping = stateLookup[MonsterBase.States.Attack16];
            bool played = false;

            monster.fsm.Changed += HandleStateChange;

            void HandleStateChange(MonsterBase.States state) {
                if (state == MonsterBase.States.Attack10) {
                    var monsterState = monster.FindState(MonsterBase.States.Attack10);

                    MonsterStatesHelper.SubscibeOnStateUpdateOnce(chargeFooMapping, () => HandleFooStateUpdate(monsterState.statusTimer));
                } else if (state == MonsterBase.States.Attack16) {
                    var monsterState = monster.FindState(MonsterBase.States.Attack16);

                    MonsterStatesHelper.SubscibeOnStateUpdateOnce(quickFooMapping, () => HandleFooStateUpdate(monsterState.statusTimer));
                }
            }

            void HandleFooStateUpdate(float stateTimer) {
                if (played || stateTimer < _explosionTime || MurmurTimeLine == null) return;

                if (Player.i.CurrentStateType == Player.i.PlayerDeadState.stateType) {
                    MurmurTimeLine.gameObject.SetActive(true);
                    MurmurTimeLine.PlayBubbleTimeline();
                    played = true;
                }
            }
        } catch(Exception ex) {
            Log.Exception(ex);
        }
    }

    private void InitMurmur(GameObject gameObject) {
        var murmurObject = GameObject.Instantiate(BubblePrefab, gameObject.transform);
        AutoAttributeManager.AutoReferenceAllChildren(murmurObject);

        if (murmurObject == null) return;

        murmurObject.SetActive(true);
        murmurObject.transform.localPosition = new Vector3(0.2f, 51f, 0f);

        var murmurComponent = murmurObject.GetComponent<TimelineMurmur>();
        var timeline = murmurObject.GetComponentInChildren<PlayableDirector>();

        var localization = murmurObject.GetComponentInChildren<Localize>();
        localization.SetTerm(MURMUR_LINE);

        var playableAsset = (TimelineAsset)timeline.playableAsset;
        var animationTrack = (AnimationTrack)playableAsset.GetRootTrack(1);
        var clips = (TimelineClip[])animationTrack.GetClips();

        if (clips.Length < 2) {
            throw new InvalidOperationException("Murmur timeline does not have enough clips");
        }

        var hideMonologueClip = clips[1];
        hideMonologueClip.start = 1.5;

        MurmurTimeLine = murmurComponent;
    }

    private void AddLocalization() {
        var sourcesGameObject = new GameObject("TopTenReasons_LanguageSource");
        sourcesGameObject.hideFlags = HideFlags.HideAndDontSave;

        var newLocalizationSource = new LanguageSourceData();
        var languageSource = sourcesGameObject.AddComponent<LanguageSource>();

        languageSource.mSource = newLocalizationSource;


        foreach (var (code, language) in Languages) {
            newLocalizationSource.AddLanguage(language, code);
        }

        var newTerm = newLocalizationSource.AddTerm(MURMUR_LINE);
        for (int i = 0; i < Languages.Count; i++) {
            newTerm.SetTranslation(i, MURMUR_LINE_CONTENT);
            
        }
    }


    private void OnDestroy() {
        // Make sure to clean up resources here to support hot reloading

        harmony.UnpatchSelf();
    }
}