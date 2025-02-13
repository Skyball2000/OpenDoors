﻿using System;
using System.Collections.Generic;
using System.Linq;
using OWML.Common;
using OWML.ModHelper;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OpenDoors
{
    public class OpenDoors : ModBehaviour
    {
        private bool _ready = false;
        private bool _debugMode = false;
        private float _maxDistance = 70f;

        private void Awake()
        {
            // You won't be able to access OWML's mod helper in Awake.
            // So you probably don't want to do anything here.
            // Use Start() instead.
        }

        private void Start()
        {
            ModHelper.Console.WriteLine($"OpenDoors mod loaded", MessageType.Success);
            LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
            {
                if (loadScene != OWScene.SolarSystem) return;

                InitializeComplexElements();
                SetOpenStateOfComplexElements(false);

                if (_ready) return;

                CreateDoorObjectNames();

                ModHelper.Console.WriteLine($"OpenDoors mod is now ready", MessageType.Success);
                _ready = true;
            };
        }

        private void InteractionNearbyObjects(bool activateCollision, bool filtered)
        {
            var start = DateTime.Now;

            var playerBody = FindObjectOfType<PlayerBody>();
            var playerPos = playerBody.transform.position;

            var allGameObjects = GetAllGameObjectsAroundPosition(playerPos, _maxDistance);

            foreach (var obj in allGameObjects)
            {
                if (IsHideableObject(obj, activateCollision || filtered))
                {
                    SetGameObjectVisibility(obj, activateCollision);
                }
            }

            SetFullPathObjectsVisibility(activateCollision, filtered);

            SetOpenStateOfComplexElements(!activateCollision);

            ModHelper.Console.WriteLine(activateCollision
                ? $"OpenDoors: closed surrounding doors ({(DateTime.Now - start).TotalMilliseconds} ms)"
                : $"OpenDoors: opened surrounding doors ({(DateTime.Now - start).TotalMilliseconds} ms)");

            NotificationManager.SharedInstance.PostNotification(new NotificationData(NotificationTarget.Player,
                activateCollision
                    ? "CLOSED SURROUNDING PATHWAYS"
                    : "OPENED " + (filtered ? "ALL " : "") + "SURROUNDING PATHWAYS"));
        }

        private HashSet<GameObject> GetAllGameObjectsAroundPosition(Vector3 referencePosition, float maxDistance)
        {
            var queryAllGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            var allGameObjects = new HashSet<GameObject>();

            foreach (var obj in queryAllGameObjects)
            {
                var objectPos = obj.transform.position;
                var distance = Vector3.Distance(objectPos, referencePosition);
                var objPosMag = objectPos.magnitude;

                if (objPosMag == 0 || distance > maxDistance || distance == 0) continue;

                allGameObjects.Add(obj);

                ExtractAllParents(obj, allGameObjects);
            }

            return allGameObjects;
        }

        private static void ExtractAllParents(GameObject obj, HashSet<GameObject> allParents)
        {
            while (true)
            {
                if (obj.transform.parent == null) return;
                var parent = obj.transform.parent;
                if (allParents.Add(parent.gameObject))
                {
                    obj = parent.gameObject;
                    continue;
                }

                break;
            }
        }

        private static string GetClipboardText()
        {
            return GUIUtility.systemCopyBuffer;
        }

        private void SetFullPathObjectsVisibility(bool visible, bool filtered)
        {
            foreach (var (key, value) in _hideDoorObjectsByFullPath)
            {
                var obj = GameObject.Find(key);
                if (obj == null) continue;
                SetGameObjectVisibility(obj, visible);
            }

            if (visible || filtered)
            {
                foreach (var (key, value) in _hideDoorObjectsByFullPathFiltered)
                {
                    var obj = GameObject.Find(key);
                    if (obj == null) continue;
                    SetGameObjectVisibility(obj, visible);
                }
            }
        }

        private void SetGameObjectVisibility(string obj, bool visible)
        {
            SetGameObjectVisibility(GameObject.Find(obj), visible);
        }

        private void SetGameObjectVisibility(GameObject obj, bool visible)
        {
            if (obj == null)
            {
                ModHelper.Console.WriteLine($"Could not find object '{obj}'", MessageType.Error);
                return;
            }

            if (visible)
            {
                ShowGameObject(obj);
            }
            else
            {
                HideGameObject(obj);
            }
        }

        private static void HideGameObject(GameObject obj)
        {
            obj.SetActive(false);
        }

        private static void ShowGameObject(GameObject obj)
        {
            obj.SetActive(true);
        }

        private Dictionary<string, string> _hideDoorObjectsByFullPath = new();
        private Dictionary<string, string> _hideDoorObjectsByFullPathFiltered = new();
        private Dictionary<string, string> _hideDoorObjectsEquals = new();
        private Dictionary<string, string> _hideDoorObjectsEqualsFiltered = new();
        private Dictionary<string, string> _hideDoorObjectsConatins = new();

        private void CreateDoorObjectNames()
        {
            _hideDoorObjectsByFullPath.Add(
                "CaveTwin_Body/Sector_CaveTwin/Sector_SouthHemisphere/Sector_SouthUnderground/Sector_FossilCave/Interactables_FossilCave/ProbePrompt_PodFossilWindow",
                "anglerfish fossil overview pod collision");
            _hideDoorObjectsByFullPath.Add(
                "CaveTwin_Body/Sector_CaveTwin/Sector_SouthHemisphere/Sector_SouthUnderground/Sector_FossilCave/Geometry_FossilCave/OtherComponentsGroup/Rocks_FossilOverlook/BatchedGroup/BatchedMeshRenderers_1",
                "anglerfish fossil overview pod stalagmites");
            _hideDoorObjectsByFullPath.Add(
                "CaveTwin_Body/Sector_CaveTwin/Sector_NorthHemisphere/Sector_NorthUnderground/Sector_LakebedCaves/Geometry_LakebedCaves/Rocks",
                "lakebed stalagmites stalactites");
            _hideDoorObjectsByFullPath.Add(
                "CaveTwin_Body/Sector_CaveTwin/Interactables_CaveTwin/Structure_NOM_EyeSymbol",
                "sunless city eye symbol outside");
            _hideDoorObjectsByFullPath.Add(
                "CaveTwin_Body/Sector_CaveTwin/Interactables_CaveTwin/Structure_NOM_EyeSymbol (1)",
                "sunless city eye symbol inside");
            _hideDoorObjectsByFullPath.Add(
                "CaveTwin_Body/Sector_CaveTwin/Sector_SouthHemisphere/Sector_CannonPath/Geometry_CannonPath/OtherComponentsGroup/Rocks",
                "sunless city cannon path stones");
            _hideDoorObjectsByFullPath.Add(
                "CaveTwin_Body/Sector_CaveTwin/Sector_SouthHemisphere/Sector_CannonPath/Geometry_CannonPath/BatchedGroup/BatchedMeshColliders_0",
                "sunless city cannon path stones colliders");
            // collision is only disable-able on the entire tower. if there were a way to only disable the collision on these two...
            //_hideDoorObjectsByFullPath.Add("BrittleHollow_Body/Sector_BH/Sector_NorthHemisphere/Sector_NorthPole/Sector_HangingCity/Sector_HangingCity_Entrance/Geometry_HangingCity_Entrance/BatchedGroup/BatchedMeshRenderers_15", "hanging city tower collapsed");
            //_hideDoorObjectsByFullPath.Add("BrittleHollow_Body/Sector_BH/Sector_NorthHemisphere/Sector_NorthPole/Sector_HangingCity/Sector_HangingCity_Entrance/Geometry_HangingCity_Entrance/BatchedGroup/BatchedMeshRenderers_7", "hanging city tower collapsed");
            _hideDoorObjectsByFullPath.Add(
                "BrittleHollow_Body/Sector_BH/Sector_QuantumFragment/Interactables_QuantumFragment/VisibleFrom_BH/ProbeWindows",
                "tower of quantum knowledge top/side probe windows");
            _hideDoorObjectsByFullPath.Add(
                "BrittleHollow_Body/Sector_BH/Sector_OldSettlement/Fragment OldSettlement 0/Core_OldSettlement0/Interactables_Core_OldSettlement0/probeWindow (1)",
                "old settlement center piece probe window");
            // this will also disable all water physics, making the islands sink to the core. it can also only be reactivated if you leave the planet and reenter it.
            //_hideDoorObjectsByFullPath.Add("GiantsDeep_Body/Sector_GD/Sector_GDInterior/Effects_GDInterior/Effects_GD_Current", "giants deep current visuals");
            //_hideDoorObjectsByFullPath.Add("GiantsDeep_Body/Sector_GD/Volumes_GD/Ocean_Fluid_Audio_Rain", "giants deep current pushback");
            _hideDoorObjectsByFullPath.Add(
                "QuantumMoon_Body/Sector_QuantumMoon/State_GD/Volumes_GDState/HurricaneFluidVolume",
                "quantum moon gd tornado physics");
            _hideDoorObjectsByFullPath.Add(
                "QuantumMoon_Body/Sector_QuantumMoon/State_GD/Effects_GDState/Effects_GD_Hurricane (1)",
                "quantum moon gd tornado visuals");
            _hideDoorObjectsByFullPath.Add(
                "Comet_Body/Sector_CO/Geometry_CO/MeltingIce",
                "interloper melting ice");
            _hideDoorObjectsByFullPath.Add(
                "RingWorld_Body/Sector_RingInterior/Sector_Zone4/Sector_PrisonDocks/Sector_PrisonInterior/Interactibles_PrisonInterior/Prefab_IP_Sarcophagus/Geo_IP_Sarcophagus/Seals",
                "stranger sarcophagus seal geometry");
            _hideDoorObjectsByFullPath.Add(
                "RingWorld_Body/Sector_RingInterior/Sector_Zone4/Sector_PrisonDocks/Sector_PrisonInterior/Interactibles_PrisonInterior/Prefab_IP_Sarcophagus/Symbol_Sarcophagus_01",
                "stranger sarcophagus seal symbol 1");
            _hideDoorObjectsByFullPath.Add(
                "RingWorld_Body/Sector_RingInterior/Sector_Zone4/Sector_PrisonDocks/Sector_PrisonInterior/Interactibles_PrisonInterior/Prefab_IP_Sarcophagus/Symbol_Sarcophagus_02",
                "stranger sarcophagus seal symbol 2");
            _hideDoorObjectsByFullPath.Add(
                "RingWorld_Body/Sector_RingInterior/Sector_Zone4/Sector_PrisonDocks/Sector_PrisonInterior/Interactibles_PrisonInterior/Prefab_IP_Sarcophagus/Symbol_Sarcophagus_03",
                "stranger sarcophagus seal symbol 3");
            _hideDoorObjectsByFullPath.Add(
                "RingWorld_Body/Sector_RingInterior/Sector_Zone4/Sector_BlightedShore/Sector_JammingControlRoom_Zone4/Interactables_JammingControlRoom_Zone4/Rotten_IP_Wall_Probe_A_Flipped",
                "stranger signal jammer scout wall");
            _hideDoorObjectsByFullPath.Add(
                "DreamWorld_Body/Sector_DreamWorld/Sector_Underground/Interactibles_Underground/SarcophagusController/SarcophagusHoleCover",
                "stranger dream sarcophagus wall hole cover");
            _hideDoorObjectsByFullPath.Add(
                "DreamWorld_Body/Sector_DreamWorld/Sector_Underground/Interactibles_Underground/SarcophagusController/Seals",
                "stranger dream sarcophagus seal symbol 3");
            _hideDoorObjectsByFullPath.Add(
                "DreamWorld_Body/Sector_DreamWorld/Sector_Underground/Volumes_Underground/WaterVolume_Underground",
                "stranger dream underground sector water volume");

            _hideDoorObjectsEquals.Add("slabs_door", "large orb doors");
            _hideDoorObjectsEquals.Add("Structure_NOM_RotatingDoor_Broken_Panels", "single sided rotating orb door");
            _hideDoorObjectsEquals.Add("PointLight_NOM_OrbSmall", "general door orb");
            _hideDoorObjectsEquals.Add("HazardVolume", "removes all hazards");
            _hideDoorObjectsEquals.Add("Cacti", "cactus parent object");
            _hideDoorObjectsEquals.Add("DarkMatter", "ghost matter");
            _hideDoorObjectsEquals.Add("DarkMatterVolume", "ghost matter");
            _hideDoorObjectsEquals.Add("GhostMatter_Clutter", "ghost matter");
            _hideDoorObjectsEquals.Add("Props_GM_Clutter", "ghost matter");
            _hideDoorObjectsEquals.Add("Airlock_OuterSphere", "airlock outer sphere");
            _hideDoorObjectsEquals.Add("Airlcok_MidSphere", "airlock mid sphere");
            _hideDoorObjectsEquals.Add("Fol_GM_Clutter", "interloper ghost matter patches");
            _hideDoorObjectsEquals.Add("OPC_Connector_Broken_BrokenPiece",
                "orbital probe cannon alunch module broken pieces");
            _hideDoorObjectsEquals.Add("DoorInterface_IP", "stranger door insterface");
            _hideDoorObjectsEquals.Add("Door_A", "stranger door wing A");
            _hideDoorObjectsEquals.Add("Door_B", "stranger door wing B");
            _hideDoorObjectsEquals.Add("COL_IP_Door_A", "stranger dream hotel door collision A");
            _hideDoorObjectsEquals.Add("COL_IP_Door_B", "stranger dream hotel door collision B");
            _hideDoorObjectsEquals.Add("ElevatorDestinations", "stranger elevator");
            _hideDoorObjectsEquals.Add("Prefab_IP_CageElevator", "stranger elevator");
            _hideDoorObjectsEquals.Add("Sarc_Piece_A", "stranger sarcophagus door A");
            _hideDoorObjectsEquals.Add("Sarc_Piece_B", "stranger sarcophagus door B");
            _hideDoorObjectsEquals.Add("SecretMuralPassage", "stranger dream hotel passage mural");

            _hideDoorObjectsConatins.Add("Cactus", "all variants and plants on cacti");
            _hideDoorObjectsConatins.Add("Structure_NOM_RotatingDoor_Panel", "both sided rotating orb door");
            _hideDoorObjectsConatins.Add("EmergencyHatch", "general emergency hatches");
            _hideDoorObjectsConatins.Add("SecretPassage", "stranger home world mural secret passage");


            _hideDoorObjectsEqualsFiltered.Add("Props_NOM_TractorBeam", "tractor beam (ring)");
            _hideDoorObjectsEqualsFiltered.Add("BeamVolume", "tractor beam (beam)");

            _hideDoorObjectsByFullPathFiltered.Add(
                "Comet_Body/Sector_CO/Geometry_CO/Frictionless_Batched/BatchedGroup",
                "interloper ice spires collision");
            _hideDoorObjectsByFullPathFiltered.Add(
                "Comet_Body/Sector_CO/Geometry_CO/Frictionless_Batched/OtherComponentsGroup/Spires/Rock_Ice_MergedSpires",
                "interloper ice spires geometry");
            _hideDoorObjectsByFullPathFiltered.Add(
                "QuantumMoon_Body/Sector_QuantumMoon/State_DB/Geometry_DBState/BatchedGroup/BatchedMeshRenderers_0",
                "quantum moon db north obstacle visuals");
            _hideDoorObjectsByFullPathFiltered.Add(
                "QuantumMoon_Body/Sector_QuantumMoon/State_DB/Geometry_DBState/BatchedGroup/BatchedMeshColliders_0",
                "quantum moon db north obstacle colliders");
        }

        private bool IsHideableObject(GameObject obj, bool filtered)
        {
            var objectName = GetObjectNameOnly(obj);

            if (_hideDoorObjectsEquals.ContainsKey(objectName))
            {
                return true;
            }

            if (filtered && _hideDoorObjectsEqualsFiltered.ContainsKey(objectName))
            {
                return true;
            }

            foreach (var (key, value) in _hideDoorObjectsConatins)
            {
                if (objectName.Contains(key))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetObjectNameOnly(GameObject obj)
        {
            return obj.name.Split(' ')[0];
        }

        private static bool ShouldObjectNameBeSkipped(GameObject obj)
        {
            return ExcludeIfContainedInName.Any(excludeIfContained => obj.name.Contains(excludeIfContained));
        }

        [SerializeField] private static readonly string[] ExcludeIfContainedInName =
            "LeftArrow,LabelBG,Top,ScreenPrompt,CommandImage,Text,Scroll View,Viewport,HorizontalLayoutGroup,Checkbox,Box,ScreenPromptListBottomLeft,Arm_M_pivot,Root,HelmetElectricalArc_3,Props_HEA_Probe_Prelaunch,ThrusterWash,DownImage,GlassBorder,ScanLightVolume4,Effects_NOM_OrbitHologram_Large,OPC_WingPiece_Tip_02_SunkenModule_Hologram,Reticule1,NomaiTranslatorProp,RightBubbles,Effects_HEA_MarshmallowFlames,Arrow1Pivot,UnderwaterEffectBubble,ScanProjector1,QuantumFogEffectBubble,Effects_NOM_OrbitalProbeCannon_Hologram,DataParticles,Effects_IP_Z4RaftHouseSplash3,SafetyCollider,GiantsDeepRoot,Sliding,EyeCoords,PlayerFootstep_Dirt,FlashlightRoot,HUD_HelmetCracks,giantsDeep,Flashlight_BasePivot,OPC_Cannon_Mid_Hologram,LeftImage,Props_HEA_ProbeLauncher_ProbeCamera,Props_HEA_Translator_RotatingPart,ToolHoldTransform,ScrollSocket,Slides_Front,Effects_IP_Z4RaftHouseSplash2,Scrollbar,Effects_HEA_ThrusterFlame,Props_HEA_Translator_Button_R,TextWarningBlock,ScaleAndRotate,Effects_NOM_HologramDrips,ScreenPromptList,RecallEffect,LockOnGUI,HelmetVisorMaskRenderer,CommandImage,FogWarpEffectBubble,Props_NOM_SmallTractorBeam_Geo,ToolModeUI,HelmetUVRenderer,HelmetFrame,Reticule2,Bottom,RotBuildingSplash_8,Props_HEA_ProbeLauncher,CanvasMarker,OPC_WingPiece_Mid_02_SunkenModule_Hologram,PlayerFootstep_None,Props_HEA_Translator_Prepass,ProbeLauncher,Scarf,HelmetRoot,HorizontalLayoutGroup,Props_HEA_Flashlight_FrontHeadlight,BackwardRightThrust,DreamEyeMask,Mallow_Root,TranslatorGroup,LaunchParticleEffect_Underwater,OPC_Module_Sunken_Hologram,ThrusterLight,OPC_WingPiece_Mid_01_SunkenModule_Hologram,ProbeLauncherChassis,VesselCoreSocket,ForwardRightBubbles,ItemSocket,ScreenPrompt,Scroll,Props_NOM_SmallTractorBeam_Anchor,DownThrust,HelmetRainDroplets,Content,ScanLightVolume5,Effects_NOM_ProbeHologram,BackwardLeftBubbles,LockOnCircle,Stick_Tip,OffScreenIndicator,HelmetVisorUVRenderer,HelmetRainStreaks,Frame_Whole,ImageBlock,OPC_Cannon_Tip_Hologram,Frame_8,Props_HEA_Translator_Button_L,CameraDetector,Effects_HEA_AirLeak,Probe,HelmetVisorEffects,SimpleLanternSocket,ThrusterWash_Default,ScanLightVolume3,Traveller_Mesh_v01,WarpCoreSocket,HelmetElectricalArc_2,BakedTerrain_Proxy_QPolePath_4_Baked,ToolStowTransform,Stick_Pivot,ProbeLauncherTransform,TextInfoBlock,Arm_S_pivot,FogWarpMarker,PointLight_HEA_TranslatorBulb,Props_HEA_Signalscope,Props_HEA_Probe_Prelaunch_Prepass,Hologram_AllProbeTrajectories,TranslatorBeams,HelmetOffLockOn,LineX,PlayerFootstep_Snow,DataStream,ScreenEffects,HUD_CurvedSurface,PageNumberText,RightThrust,VisionTorchSocket,BakedTerrain_VM_Proxy_Base,Top,Canvas,ForwardLeftThrust,ScanProjector2,LaunchParticleEffect,Props_HEA_Translator,ScanProjector4,player_mesh_noSuit,PressureGauge_Arrow,CanvasMarkerManager,ScanProjector5,RingCircle,Ring,UniverseLibCanvas,ScanLightVolume2,ForwardLeftBubbles,TranslatorScanBeam3,ShadowProjector,OPC_Base_Hologram,AttachPointWarningBlock,Props_HEA_Translator_Screen,PointLight_HEA_TranslatorButtonLeft,LineY,Flashlight_WobblePivot,Props_HEA_Translator_Pivot_RotatingPart,RoastingStick_Arm_NoSuit,Lines,ScanLightVolume1,LeftThrust,ScaleRoot,Handle,Bracket,TranslatorScanBeam1,Arm_L_pivot,LighthouseSplash_2,Props_HEA_RoastingStick,MallowSmoke,Slides_Back,preLaunchCamera,BackwardRightBubbles,Traveller_HEA_Player_v2,VesselCoreStowTransform,Stick_Root,LightFlickerEffectBubble,Flashlight_WobblePivot_OldTransforms,Effects_IP_LighthouseSplash,PointLight_HEA_TranslatorBulb2,TextScaleRoot,SingularityEffectAmbientAudio,RoastingStick_Stick,PointLight_HEA_TranslatorButtonRight,ArrowPivot,Arrow,HelmetMesh,Traveller_Rig_v01,ItemCarryTool,HUD_Helmet_v2,NebulaParticles,Frame_7,Background,RightImage,Exclamation,Text,SharedStoneSocket,Flashlight_SpotLight,Frame_6,ImageWarningBlock,RotBuildingSplash_9,CloudsEffectBubble,TranslatorScanBeam4,ScanProjector3,SlideReelSocket,FullTextBlock,SingularityEffectOneShotAudio,Arrows,ForwardRightThrust,Props_HEA_Flashlight_Geo,Props_HEA_Signalscope_Prepass,TranslatorText,HelmetElectricalArc_1,LeftBubbles,UpThrust,AttachPointInfoBlock,UpBubbles,Props_HEA_Flashlight,DreamLanternSocket,WarningBlock,TranslatorScanBeam2,CenteringPivot,Lighting,Cannon_Pivot,Viewport,LighthouseSplash_4,Props_HEA_Translator_Geo,Effects_IP_Z4RaftHouseSplash4,Props_HEA_Marshmallow,HighlightBracket,DarkMatterBubble,DownBubbles,Props_HEA_Translator_RotatingPart_Prepass,AmbientLight_EyeHologram,Signalscope,Flashlight_FillLight,SandEffectBubble,BakedTerrain_Proxy_Frag_23_Baked,GlassScreen,Helmet,TranslatorScanBeam5,HUDController,UpImage,RoastingSystem,PlayerCamera,BackwardLeftThrust,RoastingStick_Arm,Props_HEA_ProbeLauncher_Prepass"
                .Split(',');

        private const int BrittleHollowQtGravityFloorCount = 9;

        private void InitializeComplexElements()
        {
            // brittle hollow tower of quantum knowledge
            {
                for (var i = 0; i < BrittleHollowQtGravityFloorCount; i++)
                {
                    CloneGravityVolumeWithOffset(
                        "BrittleHollow_Body/Sector_BH/Sector_QuantumFragment/Interactables_QuantumFragment/Undercrust/GravityFloorVolumes/GravityVolume",
                        $"GravityVolume (OD QT {i})",
                        new Vector3(-1, i * 10, 0),
                        new Vector3(1, 2, 1));
                }
            }
        }

        private void SetOpenStateOfComplexElements(bool active)
        {
            // brittle hollow tower of quantum knowledge
            {
                for (var i = 0; i < BrittleHollowQtGravityFloorCount; i++)
                {
                    SetGameObjectVisibility(
                        $"BrittleHollow_Body/Sector_BH/Sector_QuantumFragment/Interactables_QuantumFragment/Undercrust/GravityFloorVolumes/GravityVolume (OD QT {i})",
                        active);
                }
            }
        }

        private GameObject CloneGravityVolumeWithOffset(string originalPath, string clonedName, Vector3 offset,
            Vector3 scale)
        {
            var originalGravityVolume = GameObject.Find(originalPath);
            var gravityVolume = Instantiate(originalGravityVolume, originalGravityVolume.transform.parent, true);
            gravityVolume.name = clonedName;

            gravityVolume.transform.position = originalGravityVolume.transform.position;
            gravityVolume.transform.rotation = originalGravityVolume.transform.rotation;

            var originalDirectionalForceVolume = originalGravityVolume.GetComponent<DirectionalForceVolume>();
            var directionalForceVolume = gravityVolume.GetComponent<DirectionalForceVolume>();
            directionalForceVolume._attachedBody = originalDirectionalForceVolume._attachedBody;
            gravityVolume.GetComponent<BoxShape>().enabled = true;

            var localPosition = originalGravityVolume.transform.localPosition;
            localPosition += offset;
            gravityVolume.transform.localPosition = localPosition;

            gravityVolume.transform.localScale = scale;

            ModHelper.Console.WriteLine(
                $"Cloned {gravityVolume.transform.parent.name} to {gravityVolume.name} with offset {offset} and scale {scale}",
                MessageType.Debug);

            return gravityVolume;
        }

        public void Update()
        {
            if (!_ready) return;

            if (Keyboard.current[Key.O].isPressed && Keyboard.current[Key.I].wasPressedThisFrame)
            {
                InteractionNearbyObjects(false, false);
            }

            if (Keyboard.current[Key.O].isPressed && Keyboard.current[Key.P].wasPressedThisFrame)
            {
                InteractionNearbyObjects(true, false);
            }

            if (Keyboard.current[Key.O].isPressed && Keyboard.current[Key.K].wasPressedThisFrame)
            {
                InteractionNearbyObjects(false, true);
            }

            if (Keyboard.current[Key.O].isPressed && Keyboard.current[Key.Digit0].wasPressedThisFrame)
            {
                _maxDistance = _maxDistance + 10;
                ModHelper.Console.WriteLine($"Max distance: {_maxDistance}");
                NotificationManager.SharedInstance.PostNotification(new NotificationData(NotificationTarget.Player,
                    $"SET RADIUS TO [{_maxDistance}]"));
            }

            if (Keyboard.current[Key.O].isPressed && Keyboard.current[Key.M].isPressed &&
                Keyboard.current[Key.N].wasPressedThisFrame)
            {
                _debugMode = !_debugMode;
                ModHelper.Console.WriteLine($"Debug mode: {_debugMode}");
                NotificationManager.SharedInstance.PostNotification(new NotificationData(NotificationTarget.Player,
                    "DEBUG MODE " + (_debugMode ? "ACTIVATED" : "DISABLED")));
            }

            if (Keyboard.current[Key.O].isPressed && Keyboard.current[Key.Digit9].wasPressedThisFrame)
            {
                _maxDistance = _maxDistance - 10;
                if (_maxDistance < 0)
                {
                    _maxDistance = 0;
                }

                ModHelper.Console.WriteLine($"Max distance: {_maxDistance}");
                NotificationManager.SharedInstance.PostNotification(new NotificationData(NotificationTarget.Player,
                    $"SET RADIUS TO [{_maxDistance}]"));
            }

            if (_debugMode && !Keyboard.current[Key.O].isPressed && Keyboard.current[Key.I].wasPressedThisFrame)
            {
                var clipboardText = GetClipboardText();
                if (clipboardText.Length > 0)
                {
                    ModHelper.Console.WriteLine($"Adding {clipboardText} to hide-able objects");
                    _hideDoorObjectsEquals.Add(clipboardText, clipboardText + " (from clipboard)");
                    _hideDoorObjectsConatins.Add(clipboardText, clipboardText + " (from clipboard)");
                    _hideDoorObjectsByFullPath.Add(clipboardText, clipboardText + " (from clipboard)");

                    NotificationManager.SharedInstance.PostNotification(new NotificationData(NotificationTarget.Player,
                        $"Added {clipboardText} to hide-able objects"));
                }
            }
        }
    }
}