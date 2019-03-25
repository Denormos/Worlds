using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Profiling;

public delegate void PostProgressOperation();
public delegate void PointerOperation(Vector2 position);

public enum GameMode
{
    Simulator,
    Editor,
    None
}

public class GuiManagerScript : MonoBehaviour
{
    public static GuiManagerScript ManagerScript;

    public const float MaxDeltaTimeIterations = 0.02f; // max real time to be spent on iterations on a single frame (this is the value that matters the most performance-wise)

    public Button LoadButton;

    public PlanetScript PlanetScript;
    public MapScript MapScript;

    public InfoTooltipScript InfoTooltipScript;

    public InfoPanelScript InfoPanelScript;

    public TextInputDialogPanelScript SaveFileDialogPanelScript;
    public TextInputDialogPanelScript ExportMapDialogPanelScript;
    public DecisionDialogPanelScript DecisionDialogPanelScript;
    public LoadFileDialogPanelScript LoadFileDialogPanelScript;
    public SelectFactionDialogPanelScript SelectFactionDialogPanelScript;
    public OverlayDialogPanelScript OverlayDialogPanelScript;
    public DialogPanelScript MainMenuDialogPanelScript;
    public DialogPanelScript OptionsDialogPanelScript;
    public DialogPanelScript ExceptionDialogPanelScript;
    public SettingsDialogPanelScript SettingsDialogPanelScript;
    public ProgressDialogPanelScript ProgressDialogPanelScript;
    public ImageDialogPanelScript ActivityDialogPanelScript;
    public DialogPanelScript ErrorMessageDialogPanelScript;
    public WorldCustomizationDialogPanelScript SetSeedDialogPanelScript;
    public AddPopulationDialogScript AddPopulationDialogScript;
    public FocusPanelScript FocusPanelScript;
    public GuidingPanelScript GuidingPanelScript;
    public ModalPanelScript CreditsDialogPanelScript;

    public PaletteScript BiomePaletteScript;
    public PaletteScript MapPaletteScript;
    public PaletteScript OverlayPaletteScript;

    public SelectionPanelScript SelectionPanelScript;

    public QuickTipPanelScript QuickTipPanelScript;

    public EventPanelScript EventPanelScript;

    public ToggleEvent OnSimulationInterrupted;
    public ToggleEvent OnSimulationPaused;

    public ToggleEvent OnFirstMaxSpeedOptionSet;
    public ToggleEvent OnLastMaxSpeedOptionSet;

    public UnityEvent MapEntitySelected;
    public UnityEvent OverlayChanged;

    public UnityEvent OpenModeSelectionDialogRequested;

    public UnityEvent EnteredEditorMode;
    public UnityEvent EnteredSimulationMode;

    public ToggleEvent ToggledGlobeViewing;

    public SpeedChangeEvent OnSimulationSpeedChanged;

    private bool _eventPauseActive = false;

    private bool _pauseButtonPressed = false;

    private bool _pausingDialogActive = false;

    private bool _displayedTip_mapScroll = false;
    private bool _displayedTip_initialPopulation = false;

    private Vector3 _tooltipOffset = new Vector3(0, 0);

    private TerrainCell _lastHoveredCell = null;
    
    private Territory _lastHoveredOverTerritory = null;
    private Region _lastHoveredOverRegion = null;

    private PlanetView _planetView = PlanetView.Biomes;

    private PlanetOverlay _planetOverlay = PlanetOverlay.General;

    private string _planetOverlaySubtype = "None";

    private List<PlanetOverlay> _popOverlays = new List<PlanetOverlay>()
    {
        PlanetOverlay.PopDensity,
        PlanetOverlay.FarmlandDistribution,
        PlanetOverlay.PopCulturalPreference,
        PlanetOverlay.PopCulturalActivity,
        PlanetOverlay.PopCulturalSkill,
        PlanetOverlay.PopCulturalKnowledge,
        PlanetOverlay.PopCulturalDiscovery
    };
    private int _currentPopOverlay = 0;

    private List<PlanetOverlay> _polityOverlays = new List<PlanetOverlay>()
    {
        PlanetOverlay.PolityTerritory,
        PlanetOverlay.PolityProminence,
        PlanetOverlay.PolityContacts,
        PlanetOverlay.PolityCulturalPreference,
        PlanetOverlay.PolityCulturalActivity,
        PlanetOverlay.PolityCulturalSkill,
        PlanetOverlay.PolityCulturalKnowledge,
        PlanetOverlay.PolityCulturalDiscovery,
        PlanetOverlay.FactionCoreDistance,
        PlanetOverlay.PolityCluster
    };
    private int _currentPolityOverlay = 0;

    private List<PlanetOverlay> _miscOverlays = new List<PlanetOverlay>()
    {
        PlanetOverlay.Temperature,
        PlanetOverlay.Rainfall,
        PlanetOverlay.Arability,
        PlanetOverlay.Region,
        PlanetOverlay.Language
    };
    private int _currentMiscOverlay = 0;

    private List<PlanetOverlay> _debugOverlays = new List<PlanetOverlay>()
    {
        PlanetOverlay.PopChange,
        PlanetOverlay.UpdateSpan
    };
    private int _currentDebugOverlay = 0;

    private Dictionary<PlanetOverlay, string> _planetOverlaySubtypeCache = new Dictionary<PlanetOverlay, string>();

    private bool _displayRoutes = false;
    private bool _displayGroupActivity = false;
    
    private bool _regenTextures = false;
    private bool _regenPointerOverlayTextures = false;

    private bool _resetOverlays = true;

    private bool _backgroundProcessActive = false;

    private string _progressMessage = null;
    private float _progressValue = 0;

    private event PostProgressOperation _postProgressOp = null;
    private event PostProgressOperation _generateWorldPostProgressOp = null;
    private event PostProgressOperation _regenerateWorldPostProgressOp = null;
    private event PostProgressOperation _loadWorldPostProgressOp = null;

    private event PointerOperation _mapLeftClickOp = null;

    private const float _maxAccTime = 1.0f; // the standard length of time of a simulation cycle (in real time)

    private float _accDeltaTime = 0;
    private long _simulationDateSpan = 0;

    private bool _resolvedDecision = false;

    private int _mapUpdateCount = 0;
    private int _lastMapUpdateCount = 0;
    private int _pixelUpdateCount = 0;
    private int _lastPixelUpdateCount = 0;
    private float _timeSinceLastMapUpdate = 0;
    private long _lastUpdateDate = 0;
    private long _lastDateSpan = 0;

    private int _topMaxSpeedLevelIndex;
    private int _selectedMaxSpeedLevelIndex;

    private bool _infoTextMinimized = false;

    private Texture2D _heightmap = null;

    private List<ModalPanelScript> _hiddenInteractionPanels = new List<ModalPanelScript>();

    void OnEnable()
    {
        Manager.InitializeDebugLog();

        Application.logMessageReceivedThreaded += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceivedThreaded -= HandleLog;

        Manager.CloseDebugLog();
    }

    public void RegisterRegenerateWorldPostProgressOp(PostProgressOperation op)
    {
        _regenerateWorldPostProgressOp += op;
    }

    public void DeregisterRegenerateWorldPostProgressOp(PostProgressOperation op)
    {
        _regenerateWorldPostProgressOp -= op;
    }

    public void RegisterGenerateWorldPostProgressOp(PostProgressOperation op)
    {
        _generateWorldPostProgressOp += op;
    }

    public void DeregisterGenerateWorldPostProgressOp(PostProgressOperation op)
    {
        _generateWorldPostProgressOp -= op;
    }

    public void RegisterLoadWorldPostProgressOp(PostProgressOperation op)
    {
        _loadWorldPostProgressOp += op;
    }

    public void DeregisterLoadWorldPostProgressOp(PostProgressOperation op)
    {
        _loadWorldPostProgressOp -= op;
    }

    public void ResetAllDialogs()
    {
        SelectionPanelScript.RemoveAllOptions();
        SelectionPanelScript.SetVisible(false);
        
        DecisionDialogPanelScript.SetVisible(false);
        SelectFactionDialogPanelScript.SetVisible(false);
        MainMenuDialogPanelScript.SetVisible(false);
        ProgressDialogPanelScript.SetVisible(false);
        ActivityDialogPanelScript.SetVisible(false);
        OptionsDialogPanelScript.SetVisible(false);
        ErrorMessageDialogPanelScript.SetVisible(false);
        ExceptionDialogPanelScript.SetVisible(false);
        AddPopulationDialogScript.SetVisible(false);

        FocusPanelScript.SetVisible(false);
        GuidingPanelScript.SetVisible(false);

        QuickTipPanelScript.SetVisible(false);
        InfoTooltipScript.SetVisible(false);
    }

    public void HandleLog(string logString, string stackTrace, LogType type)
    {
        Manager.HandleLog(logString, stackTrace, type);

        if (type == LogType.Exception)
        {
            Manager.EnqueueTaskAndWait(() =>
            {
                PauseSimulation(true);

                ResetAllDialogs();

                ExceptionDialogPanelScript.SetDialogText(logString);
                ExceptionDialogPanelScript.SetVisible(true);

                return true;
            });
        }
    }

    // Use this for initialization
    void Awake()
    {
        ManagerScript = this;

        Manager.LoadAppSettings(@"Worlds.settings");
    }

    // Use this for initialization
    void Start()
    {
        _topMaxSpeedLevelIndex = Speed.Levels.Length - 1;
        _selectedMaxSpeedLevelIndex = _topMaxSpeedLevelIndex;

        Manager.UpdateMainThreadReference();

        ResetAllDialogs();

        _mapLeftClickOp += ClickOp_SelectCell;

        if (!Manager.WorldIsReady)
        {
            _heightmap = Manager.LoadTexture(@"Heightmaps\mergetest_4b_3600x1800.png");

            //GenerateWorld(false, 407252633);
            //GenerateWorld(false, 1159850609);
            //GenerateWorld(false, 952294588);
            GenerateWorld(false, 732011012, useHeightmap: true);
            //GenerateWorld(false, 215020278);
        }
        else
        {
            SetGameModeAccordingToCurrentWorld();
        }

        LoadButton.interactable = HasFilesToLoad();

        Manager.SetBiomePalette(BiomePaletteScript.Colors);
        Manager.SetMapPalette(MapPaletteScript.Colors);
        Manager.SetOverlayPalette(OverlayPaletteScript.Colors);

        _regenTextures = true;
        _regenPointerOverlayTextures = true;
    }

    void OnDestroy()
    {
        Manager.SaveAppSettings(@"Worlds.settings");
    }

    // Update is called once per frame
    void Update()
    {
        ReadKeyboardInput();

        if (Manager.DebugModeEnabled)
        {
            _timeSinceLastMapUpdate += Time.deltaTime;

            if (_timeSinceLastMapUpdate > 1) // Every second
            {
                _lastMapUpdateCount = _mapUpdateCount;
                _mapUpdateCount = 0;

                _lastPixelUpdateCount = _pixelUpdateCount;
                _pixelUpdateCount = 0;

                _timeSinceLastMapUpdate -= 1;

                if (Manager.WorldIsReady)
                {
                    long currentDate = Manager.CurrentWorld.CurrentDate;

                    _lastDateSpan = currentDate - _lastUpdateDate;
                    _lastUpdateDate = currentDate;
                }
                else
                {
                    _lastDateSpan = 0;
                    _lastUpdateDate = 0;
                }
            }
        }
        
        Manager.ExecuteTasks(100);

        if (_backgroundProcessActive)
        {
            if (_progressMessage != null) ProgressDialogPanelScript.SetDialogText(_progressMessage);

            ProgressDialogPanelScript.SetProgress(_progressValue);
        }

        if (!Manager.WorldIsReady)
        {
            return;
        }

        if (Manager.PerformingAsyncTask)
        {
            return;
        }

        if (_backgroundProcessActive)
        {
            ProgressDialogPanelScript.SetVisible(false);
            ActivityDialogPanelScript.SetVisible(false);

            _backgroundProcessActive = false;

            if (_postProgressOp != null)
                _postProgressOp();

            ShowHiddenInteractionPanels();
        }

        bool simulationRunning = Manager.SimulationCanRun && Manager.SimulationRunning;

        if (simulationRunning)
        {
            Profiler.BeginSample("Perform Simulation");

            World world = Manager.CurrentWorld;

            Speed maxSpeed = Speed.Levels[_selectedMaxSpeedLevelIndex];

            _accDeltaTime += Time.deltaTime;

            if (_accDeltaTime > _maxAccTime)
            {
                _accDeltaTime -= _maxAccTime;
                _simulationDateSpan = 0;
            }

            int maxSimulationDateSpan = (int)Mathf.Ceil(maxSpeed * _accDeltaTime);

            // Simulate additional iterations if we haven't reached the max amount of iterations allowed per the percentage of transpired real time during this cycle
            if (_simulationDateSpan < maxSimulationDateSpan)
            {
                long maxDateSpanBetweenUpdates = (int)Mathf.Ceil(maxSpeed * MaxDeltaTimeIterations);
                long lastUpdateDate = world.CurrentDate;

                long dateSpan = 0;

                float startTimeIterations = Time.realtimeSinceStartup;

                // Simulate up to the max amout of iterations allowed per frame
                while ((lastUpdateDate + maxDateSpanBetweenUpdates) > world.CurrentDate)
                {

                    if (_resolvedDecision)
                    {
                        _resolvedDecision = false;
                    }
                    else
                    {
                        world.EvaluateEventsToHappen();
                    }

                    if (world.HasDecisionsToResolve())
                    {
                        RequestDecisionResolution();
                        break;
                    }

                    Profiler.BeginSample("World Update");

                    dateSpan += world.Update();

                    Profiler.EndSample();

                    float deltaTimeIterations = Time.realtimeSinceStartup - startTimeIterations;

                    // If too much real time was spent simulating after this iteration stop simulating until the next frame
                    if (deltaTimeIterations > MaxDeltaTimeIterations)
                        break;
                }

                _simulationDateSpan += dateSpan;
            }

            while (world.EventMessagesLeftToShow() > 0)
            {
                ShowEventMessage(Manager.CurrentWorld.GetNextMessageToShow());
            }

            Profiler.EndSample();
        }

        ExecuteMapHoverOps();

        if (_regenPointerOverlayTextures)
        {
            Manager.GeneratePointerOverlayTextures();
            MapScript.RefreshPointerOverlayTexture();

            _regenPointerOverlayTextures = false;
        }
        else
        {
            if (Manager.GameMode == GameMode.Editor)
            {
                Manager.UpdatePointerOverlayTextures();
            }
        }

        if (_regenTextures)
        {
            Profiler.BeginSample("Regen Textures");

            if (_resetOverlays)
            {
                _planetView = PlanetView.Biomes;

                _planetOverlay = PlanetOverlay.None;
            }

            Profiler.BeginSample("Manager.Set*");
            
            Manager.SetPlanetOverlay(_planetOverlay, _planetOverlaySubtype);
            Manager.SetPlanetView(_planetView);
            Manager.SetDisplayRoutes(_displayRoutes);
            Manager.SetDisplayGroupActivity(_displayGroupActivity);

            if (_resetOverlays)
            {
                OverlayChanged.Invoke();

                _resetOverlays = false;
            }

            Profiler.EndSample();

            Profiler.BeginSample("Manager.GenerateTextures");

            Manager.GenerateTextures();

            Profiler.EndSample();

            Profiler.BeginSample("Manager.RefreshTexture");

            MapScript.RefreshTexture();
            PlanetScript.RefreshTexture();

            Profiler.EndSample();

            if (Manager.DebugModeEnabled)
            {
                _pixelUpdateCount += Manager.UpdatedPixelCount;
                _mapUpdateCount++;
            }

            _regenTextures = false;

            Profiler.EndSample();
        }
        else
        {
            Profiler.BeginSample("Update Textures");

            Manager.UpdateTextures();

            if (Manager.DebugModeEnabled)
            {
                _pixelUpdateCount += Manager.UpdatedPixelCount;
                _mapUpdateCount++;
            }

            Profiler.EndSample();
        }

        if (MapScript.IsVisible())
        {
            UpdateInfoPanel();
            UpdateFocusPanel();
            UpdateGuidingPanel();
            UpdateSelectionMenu();
        }

        if (Manager.GameMode == GameMode.Editor)
        {
            Manager.ApplyEditorBrush();

            Manager.UpdateEditorBrushState();
        }
    }

    public void SetSimulatorMode()
    {
        Manager.GameMode = GameMode.Simulator;

        MapScript.EnablePointerOverlay(false);

        Debug.Log("Game entered history simulator mode.");

        EnteredSimulationMode.Invoke();

#if DEBUG
        ChangePlanetOverlay(PlanetOverlay.General); // When debugging we might like to autoselect a different default overlay
#else
		ChangePlanetOverlay(PlanetOverlay.General);
#endif
    }

    public void SetEditorMode()
    {
        Manager.GameMode = GameMode.Editor;

        MapScript.EnablePointerOverlay(true);

        Debug.Log("Game entered map editor mode.");

        EnteredEditorMode.Invoke();

#if DEBUG
        ChangePlanetOverlay(PlanetOverlay.None); // When debugging we might like to autoselect a different default overlay
#else
		ChangePlanetOverlay(PlanetOverlay.None);
#endif
    }

    private bool CanAlterRunningStateOrSpeed()
    {
        return Manager.SimulationCanRun && !_pausingDialogActive;
    }

    private void ReadKeyboardInput_TimeControls()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (CanAlterRunningStateOrSpeed())
            {
                PauseSimulation(Manager.SimulationRunning);
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (CanAlterRunningStateOrSpeed())
            {
                SetMaxSpeedLevel(0);
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (CanAlterRunningStateOrSpeed())
            {
                SetMaxSpeedLevel(1);
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            if (CanAlterRunningStateOrSpeed())
            {
                SetMaxSpeedLevel(2);
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            if (CanAlterRunningStateOrSpeed())
            {
                SetMaxSpeedLevel(3);
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            if (CanAlterRunningStateOrSpeed())
            {
                SetMaxSpeedLevel(4);
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            if (CanAlterRunningStateOrSpeed())
            {
                SetMaxSpeedLevel(5);
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            if (CanAlterRunningStateOrSpeed())
            {
                SetMaxSpeedLevel(6);
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            if (CanAlterRunningStateOrSpeed())
            {
                SetMaxSpeedLevel(7);
            }
        }

        if (Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            if (CanAlterRunningStateOrSpeed())
            {
                SetMaxSpeedLevel(_selectedMaxSpeedLevelIndex + 1);
            }
        }

        if (Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            if (CanAlterRunningStateOrSpeed())
            {
                SetMaxSpeedLevel(_selectedMaxSpeedLevelIndex - 1);
            }
        }
    }

    private void ReadKeyboardInput_Escape()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            if (!_backgroundProcessActive)
            {
                if (SelectFactionDialogPanelScript.gameObject.activeInHierarchy)
                {
                    CancelSelectFaction();
                }
                else if (MainMenuDialogPanelScript.gameObject.activeInHierarchy)
                {
                    CloseMainMenu();
                }
                else if (OptionsDialogPanelScript.gameObject.activeInHierarchy)
                {
                    CloseOptionsMenu();
                }
                else if (ErrorMessageDialogPanelScript.gameObject.activeInHierarchy)
                {
                    CloseErrorMessageAction();
                }
                else if (!IsModalPanelActive())
                {
                    OpenMainMenu();
                }
            }
        }
    }

    private void ReadKeyboardInput_Menus()
    {
        bool controlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        if (controlPressed)
        {
            if (Input.GetKeyUp(KeyCode.X))
            {
                ExportImageAs();
            }
            else if (Input.GetKeyUp(KeyCode.S))
            {
                SaveWorldAs();
            }
            else if (Input.GetKeyUp(KeyCode.L))
            {
                LoadWorld();
            }
            else if (Input.GetKeyUp(KeyCode.G))
            {
                SetGenerationSeed();
            }
            else if (Input.GetKeyUp(KeyCode.F))
            {
                ToogleFullscreen(!Manager.FullScreenEnabled);
            }
        }
    }

    private void ReadKeyboardInput_Globe()
    {
        bool shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (shiftPressed)
        {
            if (Input.GetKeyUp(KeyCode.G))
            {
                ToggleGlobeView();
            }
        }
    }

    private void ReadKeyboardInput_MapViews()
    {
        if (Input.GetKeyUp(KeyCode.V))
        {
            SetView(_planetView.GetNextEnumValue());
        }
    }

    private void ReadKeyboardInput_MapOverlays()
    {
        if (Input.GetKeyUp(KeyCode.N))
        {
            ChangePlanetOverlay(PlanetOverlay.None);
        }

        if (Manager.GameMode == GameMode.Simulator)
        {
            if (Input.GetKeyUp(KeyCode.G))
            {
                ChangePlanetOverlay(PlanetOverlay.General);
            }

            if (Input.GetKeyUp(KeyCode.O))
            {
                ActivatePopOverlay();
            }

            if (Input.GetKeyUp(KeyCode.P))
            {
                ActivatePolityOverlay();
            }

            if (Manager.DebugModeEnabled)
            {
                if (Input.GetKeyUp(KeyCode.D))
                {
                    ActivateDebugOverlay();
                }
            }
        }

        if (Input.GetKeyUp(KeyCode.M))
        {
            ActivateMiscOverlay();
        }
    }

    public static bool IsModalPanelActive()
    {
        return IsMenuPanelActive() || IsInteractionPanelActive();
    }

    public static bool IsMenuPanelActive()
    {
        return GameObject.FindGameObjectsWithTag("MenuPanel").Length > 0;
    }

    public static bool IsInteractionPanelActive()
    {
        return GameObject.FindGameObjectsWithTag("InteractionPanel").Length > 0;
    }

    public void HideInteractionPanel(ModalPanelScript panel)
    {
        _hiddenInteractionPanels.Add(panel);
    }

    public void ShowHiddenInteractionPanels()
    {
        if (IsMenuPanelActive())
            return; // Don't show any hidden panel if there's any menu panel still active.

            foreach (ModalPanelScript panel in _hiddenInteractionPanels)
        {
            panel.SetVisible(true);
        }

        _hiddenInteractionPanels.Clear();
    }

    private void ActivatePopOverlay()
    {
        if (_popOverlays[_currentPopOverlay] == _planetOverlay)
        {
            _currentPopOverlay = (_currentPopOverlay + 1) % _popOverlays.Count;
        }

        ChangePlanetOverlay(_popOverlays[_currentPopOverlay]);
    }

    private void ActivatePolityOverlay()
    {
        if (_polityOverlays[_currentPolityOverlay] == _planetOverlay)
        {
            _currentPolityOverlay = (_currentPolityOverlay + 1) % _polityOverlays.Count;

            if ((!Manager.DebugModeEnabled) &&
                ((_polityOverlays[_currentPolityOverlay] == PlanetOverlay.FactionCoreDistance) ||
                (_polityOverlays[_currentPolityOverlay] == PlanetOverlay.PolityCluster)))
            {
                _currentPolityOverlay = 0;
            }
        }

        ChangePlanetOverlay(_polityOverlays[_currentPolityOverlay]);
    }

    private void ActivateMiscOverlay()
    {
        if (_miscOverlays[_currentMiscOverlay] == _planetOverlay)
        {
            _currentMiscOverlay = (_currentMiscOverlay + 1) % _miscOverlays.Count;

            if ((Manager.GameMode == GameMode.Editor) &&
                ((_miscOverlays[_currentMiscOverlay] == PlanetOverlay.Language) ||
                (_miscOverlays[_currentMiscOverlay] == PlanetOverlay.Region)))
            {
                _currentMiscOverlay = 0;
            }
        }

        ChangePlanetOverlay(_miscOverlays[_currentMiscOverlay]);
    }

    private void ActivateDebugOverlay()
    {
        if (_debugOverlays[_currentDebugOverlay] == _planetOverlay)
        {
            _currentDebugOverlay = (_currentDebugOverlay + 1) % _debugOverlays.Count;
        }

        ChangePlanetOverlay(_debugOverlays[_currentDebugOverlay]);
    }

    public void SetBiomeView()
    {
        SetView(PlanetView.Biomes);
    }

    public void SetElevationView()
    {
        SetView(PlanetView.Elevation);
    }

    public void SetCoastlineView()
    {
        SetView(PlanetView.Coastlines);
    }

    private void ReadKeyboardInput()
    {
        if (_backgroundProcessActive)
            return; // Do not process keyboard inputs while a background process (generate/load/save/export) is executing.

        ReadKeyboardInput_TimeControls();
        ReadKeyboardInput_Escape();
        ReadKeyboardInput_Menus();
        ReadKeyboardInput_Globe();
        ReadKeyboardInput_MapViews();
        ReadKeyboardInput_MapOverlays();
    }

    private bool IsPolityOverlay(PlanetOverlay overlay)
    {
        return (overlay == PlanetOverlay.PolityCulturalActivity) ||
            (overlay == PlanetOverlay.PolityCulturalSkill) ||
            (overlay == PlanetOverlay.PolityCulturalPreference) ||
            (overlay == PlanetOverlay.PolityCulturalKnowledge) ||
            (overlay == PlanetOverlay.PolityCulturalDiscovery) ||
            (overlay == PlanetOverlay.PolityTerritory) ||
            (overlay == PlanetOverlay.PolityContacts) ||
            (overlay == PlanetOverlay.General);
    }

    private void UpdateFocusPanel()
    {
        Polity selectedPolity = null;
        bool isUnderFocus = false;

        if ((Manager.CurrentWorld.SelectedTerritory != null) && IsPolityOverlay(_planetOverlay))
        {
            selectedPolity = Manager.CurrentWorld.SelectedTerritory.Polity;

            isUnderFocus |= (Manager.CurrentWorld.PolitiesUnderPlayerFocus.Contains(selectedPolity));
        }

        if (selectedPolity != null)
        {
            FocusPanelScript.SetVisible(true);

            if (isUnderFocus)
                FocusPanelScript.SetState(FocusPanelState.UnsetFocus, selectedPolity);
            else
                FocusPanelScript.SetState(FocusPanelState.SetFocus, selectedPolity);
        }
        else
        {
            FocusPanelScript.SetVisible(false);
        }
    }

    private void UpdateGuidingPanel()
    {
        Faction guidedFaction = Manager.CurrentWorld.GuidedFaction;

        if (guidedFaction != null)
        {
            GuidingPanelScript.SetVisible(true);

            GuidingPanelScript.SetState(guidedFaction);
        }
        else
        {
            GuidingPanelScript.SetVisible(false);
        }
    }

    public void SelectAndCenterOnCell(WorldPosition position)
    {
        MapScript.ShiftMapToPosition(position);

        Manager.SetSelectedCell(position);

        MapEntitySelected.Invoke();
    }

    public string GetMessageToShow(WorldEventMessage eventMessage)
    {
        return Manager.GetDateString(eventMessage.Date) + " - " + eventMessage.Message;
    }

    public void ShowEventMessageForPolity(WorldEventMessage eventMessage, long polityId)
    {
        Polity polity = Manager.CurrentWorld.GetPolity(polityId);

        if (polity != null)
        {
            WorldPosition corePosition = polity.CoreGroup.Position;

            EventPanelScript.AddEventMessage(GetMessageToShow(eventMessage), () =>
            {
                SelectAndCenterOnCell(corePosition);

                if ((_planetOverlay != PlanetOverlay.PolityTerritory) && (_planetOverlay != PlanetOverlay.General))
                    ChangePlanetOverlay(PlanetOverlay.PolityTerritory);
            });
        }
        else
        {
            EventPanelScript.AddEventMessage(GetMessageToShow(eventMessage));
        }
    }

    public void ShowEventMessage(WorldEventMessage eventMessage)
    {
        if (eventMessage is TribeSplitEventMessage)
        {
            TribeSplitEventMessage tribeSplitEventMessage = eventMessage as TribeSplitEventMessage;

            ShowEventMessageForPolity(eventMessage, tribeSplitEventMessage.NewTribeId);
        }
        else if (eventMessage is PolityFormationEventMessage)
        {
            PolityFormationEventMessage polityFormationEventMessage = eventMessage as PolityFormationEventMessage;

            ShowEventMessageForPolity(eventMessage, polityFormationEventMessage.PolityId);
        }
        else if (eventMessage is PolityEventMessage)
        {
            PolityEventMessage polityEventMessage = eventMessage as PolityEventMessage;

            ShowEventMessageForPolity(eventMessage, polityEventMessage.PolityId);
        }
        else if (eventMessage is DiscoveryEventMessage)
        {
            DiscoveryEventMessage discoveryEventMessage = eventMessage as DiscoveryEventMessage;

            EventPanelScript.AddEventMessage(GetMessageToShow(discoveryEventMessage), () =>
            {
                SelectAndCenterOnCell(discoveryEventMessage.Position);

                SetPopCulturalDiscoveryOverlay(discoveryEventMessage.DiscoveryId);
            });
        }
        else if (eventMessage is CellEventMessage)
        {
            CellEventMessage cellEventMessage = eventMessage as CellEventMessage;

            EventPanelScript.AddEventMessage(GetMessageToShow(cellEventMessage), () =>
            {
                SelectAndCenterOnCell(cellEventMessage.Position);
            });
        }
        else
        {
            EventPanelScript.AddEventMessage(GetMessageToShow(eventMessage));
        }
    }

    public void ProgressUpdate(float value, string message = null, bool reset = false)
    {
        if (reset || (value >= _progressValue))
        {
            if (message != null)
                _progressMessage = message;

            _progressValue = value;
        }
    }

    public void MenuUninterruptSimulation()
    {
        if (!_eventPauseActive)
        {
            InterruptSimulation(false);
        }
    }

    private void MenuUninterruptSimulationInternal()
    {
        MenuUninterruptSimulation();

        ShowHiddenInteractionPanels();
    }

    public void CloseMainMenu()
    {
        MainMenuDialogPanelScript.SetVisible(false);

        MenuUninterruptSimulationInternal();
    }

    public void CloseSettingsDialog()
    {
        SettingsDialogPanelScript.SetVisible(false);

        MenuUninterruptSimulationInternal();
    }

    public void CloseCreditsDialog()
    {
        CreditsDialogPanelScript.SetVisible(false);

        MenuUninterruptSimulationInternal();
    }

    public void CloseOptionsMenu()
    {
        OptionsDialogPanelScript.SetVisible(false);

        MenuUninterruptSimulationInternal();
    }

    public void Exit()
    {
        Application.Quit();
    }

    public void OpenSettingsDialog()
    {
        MainMenuDialogPanelScript.SetVisible(false);

        SettingsDialogPanelScript.FullscreenToggle.isOn = Manager.FullScreenEnabled;

        SettingsDialogPanelScript.SetVisible(true);

        InterruptSimulation(true);
    }

    public void OpenCreditsDialog()
    {
        MainMenuDialogPanelScript.SetVisible(false);

        CreditsDialogPanelScript.SetVisible(true);

        InterruptSimulation(true);
    }

    public void ToogleFullscreen(bool state)
    {
        Manager.SetFullscreen(state);
    }

    public void ToogleDebugMode(bool state)
    {
        Manager.DebugModeEnabled = state;

        if (state)
        {
            _mapUpdateCount = 0;
            _lastMapUpdateCount = 0;
            _pixelUpdateCount = 0;
            _lastPixelUpdateCount = 0;
            _timeSinceLastMapUpdate = 0;
            _lastDateSpan = 0;

            if (Manager.WorldIsReady)
            {
                _lastUpdateDate = Manager.CurrentWorld.CurrentDate;
            }
            else
            {
                _lastUpdateDate = 0;
            }
        }
    }

    public void SetGenerationSeed()
    {
        MainMenuDialogPanelScript.SetVisible(false);

        int seed = Random.Range(0, int.MaxValue);

        SetSeedDialogPanelScript.SetSeedString(seed.ToString());

        SetSeedDialogPanelScript.SetVisible(true);

        InterruptSimulation(true);
    }

    public void CloseErrorMessageAction()
    {
        ErrorMessageDialogPanelScript.SetVisible(false);

        SetGenerationSeed();
    }

    public void CloseExceptionMessageAction()
    {
        Exit();
    }

    private void PostProgressOp_RegenerateWorld()
    {
        Debug.Log("Finished regenerating world with seed: " + Manager.CurrentWorld.Seed);

        Manager.WorldName = "world_" + Manager.CurrentWorld.Seed;

        SelectionPanelScript.RemoveAllOptions();

        _selectedMaxSpeedLevelIndex = _topMaxSpeedLevelIndex;

        SetMaxSpeedLevel(_selectedMaxSpeedLevelIndex);

        _postProgressOp -= PostProgressOp_RegenerateWorld;

        if (_regenerateWorldPostProgressOp != null)
            _regenerateWorldPostProgressOp.Invoke();
    }

    public void RegenerateWorldAltitudeScaleChange(float value)
    {
        Manager.AltitudeScale = value;

        RegenerateWorld(GenerationType.TerrainRegeneration);
    }

    public void RegenerateWorldSeaLevelOffsetChange(float value)
    {
        Manager.SeaLevelOffset = value;

        RegenerateWorld(GenerationType.TerrainRegeneration);
    }

    public void RegenerateWorldTemperatureOffsetChange(float value)
    {
        Manager.TemperatureOffset = value;

        RegenerateWorld(GenerationType.TemperatureRegeneration);
    }

    public void RegenerateWorldRainfallOffsetChange(float value)
    {
        Manager.RainfallOffset = value;

        RegenerateWorld(GenerationType.RainfallRegeneration);
    }

    private void RegenerateWorld(GenerationType type)
    {
        ProgressDialogPanelScript.SetVisible(true);

        ProgressUpdate(0, "Regenerating World...", true);

        Manager.RegenerateWorldAsync(type, ProgressUpdate);

        _postProgressOp += PostProgressOp_RegenerateWorld;

        _backgroundProcessActive = true;

        _regenTextures = true;
    }

    public void GenerateWorld(bool randomSeed = true, int seed = 0, bool useHeightmap = false)
    {
        if (randomSeed)
        {
            seed = Random.Range(0, int.MaxValue);
        }

        GenerateWorldInternal(seed, useHeightmap);
    }

    public void GenerateWorldWithCustomSeed()
    {
        int seed = 0;
        string seedStr = SetSeedDialogPanelScript.GetSeedString();

        if (!int.TryParse(seedStr, out seed))
        {
            ErrorMessageDialogPanelScript.SetVisible(true);
            return;
        }

        if (seed < 0)
        {
            ErrorMessageDialogPanelScript.SetVisible(true);
            return;
        }

        GenerateWorldInternal(seed);
    }

    private void PostProgressOp_GenerateWorld()
    {
        EventPanelScript.DestroyMessagePanels(); // We don't want to keep messages referencing previous worlds

        Debug.Log("Finished generating world with seed: " + Manager.CurrentWorld.Seed);

        Manager.WorldName = "world_" + Manager.CurrentWorld.Seed;

        SelectionPanelScript.RemoveAllOptions();

        OpenModeSelectionDialog();

        _selectedMaxSpeedLevelIndex = _topMaxSpeedLevelIndex;

        SetMaxSpeedLevel(_selectedMaxSpeedLevelIndex);

        _postProgressOp -= PostProgressOp_GenerateWorld;

        if (_generateWorldPostProgressOp != null)
            _generateWorldPostProgressOp.Invoke();
    }

    private void GenerateWorldInternal(int seed, bool useHeightmap = false)
    {
        ProgressDialogPanelScript.SetVisible(true);

        ProgressUpdate(0, "Generating World...", true);

        if (SetSeedDialogPanelScript.UseHeightmapToggle.isOn || useHeightmap)
        {
            Manager.GenerateNewWorldAsync(seed, _heightmap, ProgressUpdate);
        }
        else
        {
            Manager.GenerateNewWorldAsync(seed, null, ProgressUpdate);
        }

        _postProgressOp += PostProgressOp_GenerateWorld;

        _backgroundProcessActive = true;

        _regenTextures = true;
        _regenPointerOverlayTextures = true;
    }

    public void SetInitialPopulationForTests()
    {
        int population = (int)Mathf.Ceil(World.StartPopulationDensity * TerrainCell.MaxArea);

        Manager.GenerateRandomHumanGroup(population);

        InterruptSimulation(false);

        DisplayTip_MapScroll();
    }

    public void OpenModeSelectionDialog()
    {
        MapScript.EnablePointerOverlay(false);

        OpenModeSelectionDialogRequested.Invoke();

        Debug.Log("Player went back to mode selection dialog.");

        InterruptSimulation(true);
    }

    public void RandomPopulationPlacement()
    {
        int population = AddPopulationDialogScript.Population;

        AddPopulationDialogScript.SetVisible(false);

        Debug.Log(string.Format("Player chose to do random population placement of {0}...", population));

        if (population <= 0)
            return;

        Manager.GenerateRandomHumanGroup(population);

        MenuUninterruptSimulationInternal();

        DisplayTip_MapScroll();
    }

    public void ClickOp_SelectCell(Vector2 mapPosition)
    {
        int longitude = (int)mapPosition.x;
        int latitude = (int)mapPosition.y;

        Manager.SetSelectedCell(longitude, latitude);

        MapEntitySelected.Invoke();
    }

    public void ClickOp_SelectPopulationPlacement(Vector2 mapPosition)
    {
        int population = AddPopulationDialogScript.Population;

        if (AddPopulationGroupAtPosition(mapPosition, population))
        {
            MenuUninterruptSimulationInternal();

            DisplayTip_MapScroll();

            _mapLeftClickOp -= ClickOp_SelectPopulationPlacement;
        }
    }

    public void SelectPopulationPlacement()
    {
        int population = AddPopulationDialogScript.Population;

        AddPopulationDialogScript.SetVisible(false);

        Debug.Log(string.Format("Player chose to select cell for population placement of {0}...", population));

        if (population <= 0)
            return;

        DisplayTip_InitialPopulationPlacement();

        _mapLeftClickOp += ClickOp_SelectPopulationPlacement;
    }

    public bool AddPopulationGroupAtPosition(Vector2 mapPosition, int population)
    {
        World world = Manager.CurrentWorld;

        int longitude = (int)mapPosition.x;
        int latitude = (int)mapPosition.y;

        if ((longitude < 0) || (longitude >= world.Width))
            return false;

        if ((latitude < 0) || (latitude >= world.Height))
            return false;

        TerrainCell cell = world.GetCell(longitude, latitude);

        if (cell.Altitude <= Biome.Ocean.MaxAltitude)
            return false;

        Manager.GenerateHumanGroup(longitude, latitude, population);

        return true;
    }

    public void DisplayTip_InitialPopulationPlacement()
    {
        if (_displayedTip_initialPopulation)
        {
            QuickTipPanelScript.SetVisible(false);
            return;
        }

        string message = "Left click on any non-ocean position in the map to place the initial population group\n";

        if (!_displayedTip_mapScroll)
        {
            message += "Right click and drag with the mouse to scroll the map left or right\n";
        }

        message += "\n(Click anywhere on this message to close)";

        QuickTipPanelScript.SetText(message);
        QuickTipPanelScript.Reset(10);

        QuickTipPanelScript.SetVisible(true);

        _displayedTip_initialPopulation = true;
        _displayedTip_mapScroll = true;
    }

    public void DisplayTip_MapScroll()
    {
        if (_displayedTip_mapScroll)
        {
            QuickTipPanelScript.SetVisible(false);
            return;
        }

        QuickTipPanelScript.SetText(
            "Right click and drag with the mouse to scroll the map left or right\n" +
            "\n(Click anywhere on this message to close)");
        QuickTipPanelScript.Reset(10);

        QuickTipPanelScript.SetVisible(true);

        _displayedTip_mapScroll = true;
    }

    private void LoadHeightmapAction()
    {
        string path = LoadFileDialogPanelScript.GetPathToLoad();
        Texture2D texture = Manager.LoadTexture(path);

        if (texture == null)
        {
            SetSeedDialogPanelScript.SetImageTexture(Path.GetFileName(path), null, TextureValidationResult.Unknown);
        }
        else
        {
            TextureValidationResult result = Manager.ValidateTexture(texture);

            SetSeedDialogPanelScript.SetImageTexture(Path.GetFileName(path), texture, result);
        }

        _heightmap = texture;

        SetSeedDialogPanelScript.SetVisible(true);
    }

    private void CancelLoadHeightmapAction()
    {
        SetSeedDialogPanelScript.SetVisible(true);
    }

    public void LoadHeightmapImage()
    {
        LoadFileDialogPanelScript.Initialize(
            "Select Heightmap Image to Load...",
            "Load",
            LoadHeightmapAction,
            CancelLoadHeightmapAction,
            Manager.HeightmapsPath,
            Manager.SupportedHeightmapFormats);

        LoadFileDialogPanelScript.SetVisible(true);
    }

    private bool HasFilesToLoad()
    {
        string dirPath = Manager.SavePath;

        string[] files = Directory.GetFiles(dirPath, "*.PLNT");

        return files.Length > 0;
    }

    public void ExportMapAction()
    {
        ActivityDialogPanelScript.SetVisible(true);

        ActivityDialogPanelScript.SetDialogText("Exporting map to PNG file...");

        string imageName = ExportMapDialogPanelScript.GetText();

        string path = Manager.ExportPath + imageName + ".png";

        Manager.ExportMapTextureToFileAsync(path, MapScript.MapImage.uvRect);

        _postProgressOp += PostProgressOp_ExportAction;

        _backgroundProcessActive = true;
    }

    public void ExportImageAs()
    {
        OptionsDialogPanelScript.SetVisible(false);

        string planetViewStr = "";

        switch (_planetView)
        {
            case PlanetView.Biomes: planetViewStr = "_biomes"; break;
            case PlanetView.Coastlines: planetViewStr = "_coastlines"; break;
            case PlanetView.Elevation: planetViewStr = "_elevation"; break;
            default: throw new System.Exception("Unexpected planet view type: " + _planetView);
        }

        string planetOverlayStr;

        switch (_planetOverlay)
        {
            case PlanetOverlay.None: planetOverlayStr = ""; break;
            case PlanetOverlay.General:
                planetOverlayStr = "_general";
                break;
            case PlanetOverlay.PopDensity:
                planetOverlayStr = "_population_density";
                break;
            case PlanetOverlay.FarmlandDistribution:
                planetOverlayStr = "_farmland_distribution";
                break;
            case PlanetOverlay.PopCulturalPreference:
                planetOverlayStr = "_population_cultural_preference_" + _planetOverlaySubtype;
                break;
            case PlanetOverlay.PopCulturalActivity:
                planetOverlayStr = "_population_cultural_activity_" + _planetOverlaySubtype;
                break;
            case PlanetOverlay.PopCulturalSkill:
                planetOverlayStr = "_population_cultural_skill_" + _planetOverlaySubtype;
                break;
            case PlanetOverlay.PopCulturalKnowledge:
                planetOverlayStr = "_population_cultural_knowledge_" + _planetOverlaySubtype;
                break;
            case PlanetOverlay.PopCulturalDiscovery:
                planetOverlayStr = "_population_cultural_discovery_" + _planetOverlaySubtype;
                break;
            case PlanetOverlay.PolityTerritory:
                planetOverlayStr = "_polity_territories";
                break;
            case PlanetOverlay.PolityCluster:
                planetOverlayStr = "_polity_vlusters";
                break;
            case PlanetOverlay.FactionCoreDistance:
                planetOverlayStr = "_faction_core_distances";
                break;
            case PlanetOverlay.Language:
                planetOverlayStr = "_languages";
                break;
            case PlanetOverlay.PolityProminence:
                planetOverlayStr = "_polity_prominences";
                break;
            case PlanetOverlay.PolityContacts:
                planetOverlayStr = "_polity_contacts";
                break;
            case PlanetOverlay.PolityCulturalPreference:
                planetOverlayStr = "_polity_cultural_preference_" + _planetOverlaySubtype;
                break;
            case PlanetOverlay.PolityCulturalActivity:
                planetOverlayStr = "_polity_cultural_activity_" + _planetOverlaySubtype;
                break;
            case PlanetOverlay.PolityCulturalSkill:
                planetOverlayStr = "_polity_cultural_skill_" + _planetOverlaySubtype;
                break;
            case PlanetOverlay.PolityCulturalKnowledge:
                planetOverlayStr = "_polity_cultural_knowledge_" + _planetOverlaySubtype;
                break;
            case PlanetOverlay.PolityCulturalDiscovery:
                planetOverlayStr = "_polity_cultural_discovery_" + _planetOverlaySubtype;
                break;
            case PlanetOverlay.Temperature:
                planetOverlayStr = "_temperature";
                break;
            case PlanetOverlay.Rainfall:
                planetOverlayStr = "_rainfall";
                break;
            case PlanetOverlay.Arability:
                planetOverlayStr = "_arability";
                break;
            case PlanetOverlay.Region:
                planetOverlayStr = "_region";
                break;
            case PlanetOverlay.PopChange:
                planetOverlayStr = "_population_change";
                break;
            case PlanetOverlay.UpdateSpan:
                planetOverlayStr = "_update_span";
                break;
            default: throw new System.Exception("Unexpected planet overlay type: " + _planetOverlay);
        }

        ExportMapDialogPanelScript.SetText(Manager.AddDateToWorldName(Manager.WorldName) + planetViewStr + planetOverlayStr);
        ExportMapDialogPanelScript.SetVisible(true);

        InterruptSimulation(true);
    }

    public void PostProgressOp_SaveAction()
    {
        Debug.Log("Finished saving world to file.");

        LoadButton.interactable = HasFilesToLoad();

        _postProgressOp -= PostProgressOp_SaveAction;

        if (!_eventPauseActive)
        {
            InterruptSimulation(!Manager.SimulationCanRun);
        }

        ShowHiddenInteractionPanels();
    }

    public void PostProgressOp_ExportAction()
    {
        Debug.Log("Finished exporting world map to .png file.");

        _postProgressOp -= PostProgressOp_ExportAction;

        if (!_eventPauseActive)
        {
            InterruptSimulation(!Manager.SimulationCanRun);
        }

        ShowHiddenInteractionPanels();
    }

    public void SaveAction()
    {
        ActivityDialogPanelScript.SetVisible(true);

        ActivityDialogPanelScript.SetDialogText("Saving World...");

        string saveName = SaveFileDialogPanelScript.GetText();

        Manager.WorldName = Manager.RemoveDateFromWorldName(saveName);

        string path = Manager.SavePath + saveName + ".plnt";

        Manager.SaveWorldAsync(path);

        _postProgressOp += PostProgressOp_SaveAction;

        _backgroundProcessActive = true;
    }

    public void SaveWorldAs()
    {
        MainMenuDialogPanelScript.SetVisible(false);

        SaveFileDialogPanelScript.SetText(Manager.AddDateToWorldName(Manager.WorldName));
        SaveFileDialogPanelScript.SetVisible(true);

        InterruptSimulation(true);
    }

    public void GetMaxSpeedOptionFromCurrentWorld()
    {
        long maxSpeed = Manager.CurrentWorld.MaxTimeToSkip;

        for (int i = 0; i < Speed.Levels.Length; i++)
        {
            if (maxSpeed <= Speed.Levels[i])
            {
                _selectedMaxSpeedLevelIndex = i;

                SetMaxSpeedLevel(_selectedMaxSpeedLevelIndex);

                break;
            }
        }
    }

    public void IncreaseMaxSpeed()
    {
        if (_pauseButtonPressed)
            return;

        if (_selectedMaxSpeedLevelIndex == _topMaxSpeedLevelIndex)
            return;

        _selectedMaxSpeedLevelIndex++;

        SetMaxSpeedLevel(_selectedMaxSpeedLevelIndex);
    }

    public void DecreaseMaxSpeed()
    {
        if (_pauseButtonPressed)
            return;

        if (_selectedMaxSpeedLevelIndex == 0)
            return;

        _selectedMaxSpeedLevelIndex--;

        SetMaxSpeedLevel(_selectedMaxSpeedLevelIndex);
    }

    public void SetMaxSpeedLevel(int speedLevelIndex)
    {
        _selectedMaxSpeedLevelIndex = Mathf.Clamp(speedLevelIndex, 0, _topMaxSpeedLevelIndex);

        OnFirstMaxSpeedOptionSet.Invoke(_pausingDialogActive || (_selectedMaxSpeedLevelIndex == 0));
        OnLastMaxSpeedOptionSet.Invoke(_pausingDialogActive || (_selectedMaxSpeedLevelIndex == _topMaxSpeedLevelIndex));

        // This is the max amount of iterations to simulate per second
        Speed selectedSpeed = Speed.Levels[_selectedMaxSpeedLevelIndex];

        // This is the max amount of iterations to simulate per frame
        int maxSpeed = (int)Mathf.Ceil(selectedSpeed * MaxDeltaTimeIterations);

        Manager.CurrentWorld.SetMaxTimeToSkip(maxSpeed);

        OnSimulationSpeedChanged.Invoke(selectedSpeed);

        ResetAccDeltaTime();
    }

    private void ResetAccDeltaTime()
    {
        _accDeltaTime = 0;
        _simulationDateSpan = 0;
    }

    public void SetGameModeAccordingToCurrentWorld()
    {
        if (!Manager.SimulationCanRun)
        {
            OpenModeSelectionDialog();
        }
        else
        {
            MenuUninterruptSimulationInternal();

            SetSimulatorMode();
        }

        GetMaxSpeedOptionFromCurrentWorld();
    }

    public void PostProgressOp_LoadAction()
    {
        EventPanelScript.DestroyMessagePanels(); // We don't want to keep messages referencing previous worlds

        Debug.Log(string.Format(
            "Finished loading world. Seed: {0}, Avg. Temperature: {1}, Avg. Rainfall: {2}, Sea Level Offset: {3}, Current Date: {4}",
            Manager.CurrentWorld.Seed,
            Manager.CurrentWorld.TemperatureOffset,
            Manager.CurrentWorld.RainfallOffset,
            Manager.CurrentWorld.SeaLevelOffset,
            Manager.GetDateString(Manager.CurrentWorld.CurrentDate)));

        SelectionPanelScript.RemoveAllOptions();

        SetGameModeAccordingToCurrentWorld();

        _postProgressOp -= PostProgressOp_LoadAction;

        if (_loadWorldPostProgressOp != null)
            _loadWorldPostProgressOp.Invoke();
    }

    private void LoadAction()
    {
        ProgressDialogPanelScript.SetVisible(true);

        ProgressUpdate(0, "Loading World...", true);

        string path = LoadFileDialogPanelScript.GetPathToLoad();

        Manager.LoadWorldAsync(path, ProgressUpdate);

        Manager.WorldName = Manager.RemoveDateFromWorldName(Path.GetFileNameWithoutExtension(path));

        _postProgressOp += PostProgressOp_LoadAction;

        _backgroundProcessActive = true;

        _regenTextures = true;
        _regenPointerOverlayTextures = true;
    }

    private void CancelLoadAction()
    {
        MenuUninterruptSimulationInternal();
    }

    public void LoadWorld()
    {
        MainMenuDialogPanelScript.SetVisible(false);
        
        LoadFileDialogPanelScript.Initialize(
            "Select World to Load...",
            "Load",
            LoadAction,
            CancelLoadAction,
            Manager.SavePath,
            new string[] { ".PLNT" });

        LoadFileDialogPanelScript.SetVisible(true);

        InterruptSimulation(true);
    }

    public void RequestDecisionResolution()
    {
        Decision decisionToResolve = Manager.CurrentWorld.PullDecisionToResolve();

        DecisionDialogPanelScript.Set(decisionToResolve, _selectedMaxSpeedLevelIndex);

        if (!IsMenuPanelActive())
        {
            DecisionDialogPanelScript.SetVisible(true);
        }
        else
        {
            // Hide the decision dialog until all menu panels are inactive
            HideInteractionPanel(DecisionDialogPanelScript);
        }

        InterruptSimulation(true);

        _eventPauseActive = true;
    }

    public void ResolveDecision()
    {
        DecisionDialogPanelScript.SetVisible(false);

        int resumeSpeedLevelIndex = DecisionDialogPanelScript.ResumeSpeedLevelIndex;

        if (resumeSpeedLevelIndex == -1)
        {
            PauseSimulation(true);
        }
        else
        {
            _selectedMaxSpeedLevelIndex = resumeSpeedLevelIndex;

            SetMaxSpeedLevel(_selectedMaxSpeedLevelIndex);
        }

        InterruptSimulation(false);

        _eventPauseActive = false;

        _resolvedDecision = true;
    }

    public void ChangePlanetOverlayToSelected()
    {
        SelectionPanelScript.RemoveAllOptions();
        SelectionPanelScript.SetVisible(false);

        if (OverlayDialogPanelScript.DontUpdateDialog)
            return;

        OverlayDialogPanelScript.ResetToggles();

        if (OverlayDialogPanelScript.GeneralDataToggle.isOn)
        {
            ChangePlanetOverlay(PlanetOverlay.General, false);
        }
        else if (OverlayDialogPanelScript.PopDensityToggle.isOn)
        {
            ChangePlanetOverlay(PlanetOverlay.PopDensity, false);
        }
        else if (OverlayDialogPanelScript.FarmlandToggle.isOn)
        {
            ChangePlanetOverlay(PlanetOverlay.FarmlandDistribution, false);
        }
        else if (OverlayDialogPanelScript.PopCulturalPreferenceToggle.isOn)
        {
            ChangePlanetOverlay(PlanetOverlay.PopCulturalPreference, false);
        }
        else if (OverlayDialogPanelScript.PopCulturalActivityToggle.isOn)
        {
            ChangePlanetOverlay(PlanetOverlay.PopCulturalActivity, false);
        }
        else if (OverlayDialogPanelScript.PopCulturalSkillToggle.isOn)
        {
            ChangePlanetOverlay(PlanetOverlay.PopCulturalSkill, false);
        }
        else if (OverlayDialogPanelScript.PopCulturalKnowledgeToggle.isOn)
        {
            ChangePlanetOverlay(PlanetOverlay.PopCulturalKnowledge, false);
        }
        else if (OverlayDialogPanelScript.PopCulturalDiscoveryToggle.isOn)
        {
            ChangePlanetOverlay(PlanetOverlay.PopCulturalDiscovery, false);
        }
        else if (OverlayDialogPanelScript.TerritoriesToggle.isOn)
        {
            ChangePlanetOverlay(PlanetOverlay.PolityTerritory, false);
        }
        else if (OverlayDialogPanelScript.PolityClustersToggle.isOn)
        {
            ChangePlanetOverlay(PlanetOverlay.PolityCluster, false);
        }
        else if (OverlayDialogPanelScript.DistancesToCoresToggle.isOn)
        {
            ChangePlanetOverlay(PlanetOverlay.FactionCoreDistance, false);
        }
        else if (OverlayDialogPanelScript.ProminenceToggle.isOn)
        {
            ChangePlanetOverlay(PlanetOverlay.PolityProminence, false);
        }
        else if (OverlayDialogPanelScript.ContactsToggle.isOn)
        {
            ChangePlanetOverlay(PlanetOverlay.PolityContacts, false);
        }
        else if (OverlayDialogPanelScript.PolityCulturalPreferenceToggle.isOn)
        {
            ChangePlanetOverlay(PlanetOverlay.PolityCulturalPreference, false);
        }
        else if (OverlayDialogPanelScript.PolityCulturalActivityToggle.isOn)
        {
            ChangePlanetOverlay(PlanetOverlay.PolityCulturalActivity, false);
        }
        else if (OverlayDialogPanelScript.PolityCulturalSkillToggle.isOn)
        {
            ChangePlanetOverlay(PlanetOverlay.PolityCulturalSkill, false);
        }
        else if (OverlayDialogPanelScript.PolityCulturalKnowledgeToggle.isOn)
        {
            ChangePlanetOverlay(PlanetOverlay.PolityCulturalKnowledge, false);
        }
        else if (OverlayDialogPanelScript.PolityCulturalDiscoveryToggle.isOn)
        {
            ChangePlanetOverlay(PlanetOverlay.PolityCulturalDiscovery, false);
        }
        else if (OverlayDialogPanelScript.TemperatureToggle.isOn)
        {
            ChangePlanetOverlay(PlanetOverlay.Temperature, false);
        }
        else if (OverlayDialogPanelScript.RainfallToggle.isOn)
        {
            ChangePlanetOverlay(PlanetOverlay.Rainfall, false);
        }
        else if (OverlayDialogPanelScript.ArabilityToggle.isOn)
        {
            ChangePlanetOverlay(PlanetOverlay.Arability, false);
        }
        else if (OverlayDialogPanelScript.RegionToggle.isOn)
        {
            ChangePlanetOverlay(PlanetOverlay.Region, false);
        }
        else if (OverlayDialogPanelScript.LanguageToggle.isOn)
        {
            ChangePlanetOverlay(PlanetOverlay.Language, false);
        }
        else if (OverlayDialogPanelScript.PopChangeToggle.isOn)
        {
            ChangePlanetOverlay(PlanetOverlay.PopChange, false);
        }
        else if (OverlayDialogPanelScript.UpdateSpanToggle.isOn)
        {
            ChangePlanetOverlay(PlanetOverlay.UpdateSpan, false);
        }
        else
        {
            ChangePlanetOverlay(PlanetOverlay.None, false);
        }

        SetRouteDisplayOverlay(OverlayDialogPanelScript.DisplayRoutesToggle.isOn, false);
        SetGroupActivityOverlay(OverlayDialogPanelScript.DisplayGroupActivityToggle.isOn, false);
    }

    public void OpenMainMenu()
    {
        MainMenuDialogPanelScript.SetVisible(true);

        InterruptSimulation(true);
    }

    public void OpenOptionsMenu()
    {
        OptionsDialogPanelScript.SetVisible(true);

        InterruptSimulation(true);
    }

    public void SetSimulationSpeedStopped(bool state)
    {
        if (state)
        {
            OnSimulationSpeedChanged.Invoke(Speed.Zero);
        }
        else
        {
            OnSimulationSpeedChanged.Invoke(Speed.Levels[_selectedMaxSpeedLevelIndex]);
        }
    }

    public void MinimizeInfoText(bool state)
    {
        _infoTextMinimized = state;
    }

    public void PauseSimulation(bool state)
    {
        OnSimulationPaused.Invoke(state);

        _pauseButtonPressed = state;

        bool holdState = _pauseButtonPressed;

        HoldSimulation(holdState);
    }

    public void InterruptSimulation(bool state)
    {
        _pausingDialogActive = state;

        OnSimulationInterrupted.Invoke(state);

        bool holdState = _pausingDialogActive || _pauseButtonPressed;

        HoldSimulation(holdState);
    }

    private void HoldSimulation(bool state)
    {
        SetSimulationSpeedStopped(state);

        OnFirstMaxSpeedOptionSet.Invoke(state || (_selectedMaxSpeedLevelIndex == 0));
        OnLastMaxSpeedOptionSet.Invoke(state || (_selectedMaxSpeedLevelIndex == _topMaxSpeedLevelIndex));

        Manager.InterruptSimulation(state);

        ResetAccDeltaTime();
    }

    public void ToggleGlobeView()
    {
        bool newState = !Manager.ViewingGlobe;

        Manager.ViewingGlobe = newState;

        MapScript.SetVisible(!newState);
        PlanetScript.SetVisible(newState);

        ToggledGlobeViewing.Invoke(newState);
    }

    public void SetRouteDisplayOverlay(bool value)
    {
        SetRouteDisplayOverlay(value, true);
    }

    public void SetRouteDisplayOverlay(bool value, bool invokeEvent)
    {
        _regenTextures |= _displayRoutes != value;

        _displayRoutes = value;

        if (_regenTextures)
        {
            Manager.SetDisplayRoutes(_displayRoutes);

            if (invokeEvent)
            {
                OverlayChanged.Invoke();

                _resetOverlays = false;
            }
        }
    }

    public void SetGroupActivityOverlay(bool value)
    {
        SetGroupActivityOverlay(value, true);
    }

    public void SetGroupActivityOverlay(bool value, bool invokeEvent)
    {
        _regenTextures |= _displayGroupActivity != value;

        _displayGroupActivity = value;

        if (_regenTextures)
        {
            Manager.SetDisplayGroupActivity(_displayGroupActivity);

            if (invokeEvent)
            {
                OverlayChanged.Invoke();

                _resetOverlays = false;
            }
        }
    }

    public void ChangeToTemperatureOverlayFromEditorToolbar(bool state)
    {
        if (state)
        {
            ChangePlanetOverlay(PlanetOverlay.Temperature);
        }
        else
        {
            ChangePlanetOverlay(PlanetOverlay.None);
        }
    }

    public void ChangeToRainfallOverlayFromEditorToolbar(bool state)
    {
        if (state)
        {
            ChangePlanetOverlay(PlanetOverlay.Rainfall);
        }
        else
        {
            ChangePlanetOverlay(PlanetOverlay.None);
        }
    }

    public void DisableAllOverlays()
    {
        ChangePlanetOverlay(PlanetOverlay.None);
    }

    public void SetCurrentOverlayIndexInGroup(PlanetOverlay overlay)
    {
        int index = _popOverlays.IndexOf(overlay);

        if (index != -1)
        {
            _currentPopOverlay = index;
            return;
        }

        index = _polityOverlays.IndexOf(overlay);

        if (index != -1)
        {
            _currentPolityOverlay = index;
            return;
        }

        index = _miscOverlays.IndexOf(overlay);

        if (index != -1)
        {
            _currentMiscOverlay = index;
            return;
        }

        index = _debugOverlays.IndexOf(overlay);

        if (index != -1)
        {
            _currentDebugOverlay = index;
            return;
        }
    }

    public void ChangePlanetOverlay(PlanetOverlay overlay, string planetOverlaySubtype, bool invokeEvent = true)
    {
        _regenTextures |= _planetOverlaySubtype != planetOverlaySubtype;
        _regenTextures |= _planetOverlay != overlay;

        if ((_planetOverlay != overlay) && (_planetOverlay != PlanetOverlay.None))
        {
            _planetOverlaySubtypeCache[_planetOverlay] = _planetOverlaySubtype;
        }

        _planetOverlaySubtype = planetOverlaySubtype;

        _planetOverlay = overlay;

        SetCurrentOverlayIndexInGroup(overlay);

        if (invokeEvent)
        {
            Manager.SetPlanetOverlay(_planetOverlay, _planetOverlaySubtype);

            OverlayChanged.Invoke();

            _resetOverlays = false;
        }

        HandleOverlayWithSubtypes(overlay);
    }

    public void ChangePlanetOverlay(PlanetOverlay overlay)
    {
        ChangePlanetOverlay(overlay, true);
    }

    public void ChangePlanetOverlay(PlanetOverlay overlay, bool invokeEvent)
    {
        string currentOverlaySubtype;

        if (!_planetOverlaySubtypeCache.TryGetValue(overlay, out currentOverlaySubtype))
        {
            currentOverlaySubtype = "None";
        }

        ChangePlanetOverlay(overlay, currentOverlaySubtype, invokeEvent);
    }

    private void HandleOverlayWithSubtypes(PlanetOverlay value)
    {
        switch (value)
        {
            case PlanetOverlay.PopCulturalPreference:
                HandleCulturalPreferenceOverlay();
                break;

            case PlanetOverlay.PolityCulturalPreference:
                HandleCulturalPreferenceOverlay();
                break;

            case PlanetOverlay.PopCulturalActivity:
                HandleCulturalActivityOverlay();
                break;

            case PlanetOverlay.PolityCulturalActivity:
                HandleCulturalActivityOverlay();
                break;

            case PlanetOverlay.PopCulturalSkill:
                HandleCulturalSkillOverlay();
                break;

            case PlanetOverlay.PolityCulturalSkill:
                HandleCulturalSkillOverlay();
                break;

            case PlanetOverlay.PopCulturalKnowledge:
                HandleCulturalKnowledgeOverlay();
                break;

            case PlanetOverlay.PolityCulturalKnowledge:
                HandleCulturalKnowledgeOverlay();
                break;

            case PlanetOverlay.PopCulturalDiscovery:
                HandleCulturalDiscoveryOverlay();
                break;

            case PlanetOverlay.PolityCulturalDiscovery:
                HandleCulturalDiscoveryOverlay();
                break;
        }
    }

    private void HandleCulturalPreferenceOverlay()
    {
        SelectionPanelScript.Title.text = "Displayed Preference:";

        foreach (CulturalPreferenceInfo preferenceInfo in Manager.CurrentWorld.CulturalPreferenceInfoList)
        {
            AddSelectionPanelOption(preferenceInfo.Name, preferenceInfo.Id);
        }

        SelectionPanelScript.SetVisible(true);
    }

    private void HandleCulturalActivityOverlay()
    {
        SelectionPanelScript.Title.text = "Displayed Activity:";

        foreach (CulturalActivityInfo activityInfo in Manager.CurrentWorld.CulturalActivityInfoList)
        {
            AddSelectionPanelOption(activityInfo.Name, activityInfo.Id);
        }

        SelectionPanelScript.SetVisible(true);
    }

    private void HandleCulturalSkillOverlay()
    {
        SelectionPanelScript.Title.text = "Displayed Skill:";

        foreach (CulturalSkillInfo skillInfo in Manager.CurrentWorld.CulturalSkillInfoList)
        {
            AddSelectionPanelOption(skillInfo.Name, skillInfo.Id);
        }

        SelectionPanelScript.SetVisible(true);
    }

    private void HandleCulturalKnowledgeOverlay()
    {
        SelectionPanelScript.Title.text = "Displayed Knowledge:";

        foreach (CulturalKnowledgeInfo knowledgeInfo in Manager.CurrentWorld.CulturalKnowledgeInfoList)
        {
            AddSelectionPanelOption(knowledgeInfo.Name, knowledgeInfo.Id);
        }

        SelectionPanelScript.SetVisible(true);
    }

    private void HandleCulturalDiscoveryOverlay()
    {
        SelectionPanelScript.Title.text = "Displayed Discovery:";

        foreach (CulturalDiscoveryInfo discoveryInfo in Manager.CurrentWorld.CulturalDiscoveryInfoList)
        {
            AddSelectionPanelOption(discoveryInfo.Name, discoveryInfo.Id);
        }

        SelectionPanelScript.SetVisible(true);
    }

    public void SetPopCulturalDiscoveryOverlay(string planetOverlaySubtype, bool invokeEvent = true)
    {
        ChangePlanetOverlay(PlanetOverlay.PopCulturalDiscovery, planetOverlaySubtype, invokeEvent);
    }

    public void AddSelectionPanelOption(string optionName, string optionId)
    {
        SelectionPanelScript.AddOption(optionId, optionName, (state) =>
        {
            if (state)
            {
                _planetOverlaySubtype = optionId;
            }
            else if (_planetOverlaySubtype == optionId)
            {
                _planetOverlaySubtype = "None";
            }

            _regenTextures = true;
        });

        if (_planetOverlaySubtype == optionId)
        {
            SelectionPanelScript.SetStateOption(optionId, true);
        }
    }

    private void UpdateSelectionMenu()
    {
        if (!SelectionPanelScript.IsVisible())
            return;

        if (_planetOverlay == PlanetOverlay.PopCulturalPreference)
        {
            foreach (CulturalPreferenceInfo preferenceInfo in Manager.CurrentWorld.CulturalPreferenceInfoList)
            {
                AddSelectionPanelOption(preferenceInfo.Name, preferenceInfo.Id);
            }
        }
        else if (_planetOverlay == PlanetOverlay.PopCulturalActivity)
        {
            foreach (CulturalActivityInfo activityInfo in Manager.CurrentWorld.CulturalActivityInfoList)
            {
                AddSelectionPanelOption(activityInfo.Name, activityInfo.Id);
            }
        }
        else if (_planetOverlay == PlanetOverlay.PopCulturalSkill)
        {
            foreach (CulturalSkillInfo skillInfo in Manager.CurrentWorld.CulturalSkillInfoList)
            {
                AddSelectionPanelOption(skillInfo.Name, skillInfo.Id);
            }
        }
        else if (_planetOverlay == PlanetOverlay.PopCulturalKnowledge)
        {
            foreach (CulturalKnowledgeInfo knowledgeInfo in Manager.CurrentWorld.CulturalKnowledgeInfoList)
            {
                AddSelectionPanelOption(knowledgeInfo.Name, knowledgeInfo.Id);
            }
        }
        else if (_planetOverlay == PlanetOverlay.PopCulturalDiscovery)
        {
            foreach (CulturalDiscoveryInfo discoveryInfo in Manager.CurrentWorld.CulturalDiscoveryInfoList)
            {
                AddSelectionPanelOption(discoveryInfo.Name, discoveryInfo.Id);
            }
        }
        else if (_planetOverlay == PlanetOverlay.PolityCulturalPreference)
        {
            foreach (CulturalPreferenceInfo preferenceInfo in Manager.CurrentWorld.CulturalPreferenceInfoList)
            {
                AddSelectionPanelOption(preferenceInfo.Name, preferenceInfo.Id);
            }
        }
        else if (_planetOverlay == PlanetOverlay.PolityCulturalActivity)
        {
            foreach (CulturalActivityInfo activityInfo in Manager.CurrentWorld.CulturalActivityInfoList)
            {
                AddSelectionPanelOption(activityInfo.Name, activityInfo.Id);
            }
        }
        else if (_planetOverlay == PlanetOverlay.PolityCulturalSkill)
        {
            foreach (CulturalSkillInfo skillInfo in Manager.CurrentWorld.CulturalSkillInfoList)
            {
                AddSelectionPanelOption(skillInfo.Name, skillInfo.Id);
            }
        }
        else if (_planetOverlay == PlanetOverlay.PolityCulturalKnowledge)
        {
            foreach (CulturalKnowledgeInfo knowledgeInfo in Manager.CurrentWorld.CulturalKnowledgeInfoList)
            {
                AddSelectionPanelOption(knowledgeInfo.Name, knowledgeInfo.Id);
            }
        }
        else if (_planetOverlay == PlanetOverlay.PolityCulturalDiscovery)
        {
            foreach (CulturalDiscoveryInfo discoveryInfo in Manager.CurrentWorld.CulturalDiscoveryInfoList)
            {
                AddSelectionPanelOption(discoveryInfo.Name, discoveryInfo.Id);
            }
        }
    }
    
    public void SetView(PlanetView planetView)
    {
        _regenTextures |= _planetView != planetView;

        _planetView = planetView;
    }

    public void OpenSelectFactionDialog()
    {
        SelectFactionDialogPanelScript.SetVisible(true);

        InterruptSimulation(true);
    }

    public void SetFactionToGuideAction()
    {
        SelectFactionDialogPanelScript.SetVisible(false);

        Faction faction = SelectFactionDialogPanelScript.ChosenFaction;

        if (faction != null)
        {
            Manager.SetGuidedFaction(faction);
        }

        MenuUninterruptSimulationInternal();
    }

    public void StopGuidingFaction()
    {
        Manager.SetGuidedFaction(null);
    }

    public void CancelSelectFaction()
    {
        SelectFactionDialogPanelScript.SetVisible(false);

        MenuUninterruptSimulationInternal();
    }

    public void SetPlayerFocusOnPolity()
    {
        Territory selectedTerritory = Manager.CurrentWorld.SelectedTerritory;

        if ((selectedTerritory != null) && !selectedTerritory.Polity.IsUnderPlayerFocus)
            Manager.SetFocusOnPolity(selectedTerritory.Polity);
    }

    public void UnsetPlayerFocusOnPolity()
    {
        Territory selectedTerritory = Manager.CurrentWorld.SelectedTerritory;

        if ((selectedTerritory != null) && selectedTerritory.Polity.IsUnderPlayerFocus)
            Manager.UnsetFocusOnPolity(selectedTerritory.Polity);
    }

    private void UpdateInfoPanel()
    {
        World world = Manager.CurrentWorld;

        if (Manager.GameMode == GameMode.Simulator)
        {
            InfoPanelScript.InfoText.text = Manager.GetDateString(world.CurrentDate);
        }
        else if (Manager.GameMode == GameMode.Editor)
        {
            InfoPanelScript.InfoText.text = "Map Editor Mode";
        }

        if (_infoTextMinimized)
            return;

        if (Manager.CurrentWorld.SelectedCell != null)
        {
            AddCellDataToInfoPanel(Manager.CurrentWorld.SelectedCell);
        }

        InfoPanelScript.InfoText.text += "\n";

        if (Manager.DebugModeEnabled)
        {
            InfoPanelScript.InfoText.text += "\n -- Debug Data -- ";

            if ((Manager.CurrentWorld != null) &&
                (Manager.CurrentWorld.SelectedTerritory != null))
            {
                InfoPanelScript.InfoText.text += "\n";
                InfoPanelScript.InfoText.text += "\nSelected Territory's Polity Id: " + Manager.CurrentWorld.SelectedTerritory.Polity.Id;
            }

            InfoPanelScript.InfoText.text += "\n";
            InfoPanelScript.InfoText.text += "\nNumber of Migration Events: " + MigrateGroupEvent.MigrationEventCount;

            InfoPanelScript.InfoText.text += "\n";
            InfoPanelScript.InfoText.text += "\nMap Updates Per RTS: " + _lastMapUpdateCount;
            InfoPanelScript.InfoText.text += "\nPixel Updates Per RTS: " + _lastPixelUpdateCount;

            if (_lastMapUpdateCount > 0)
            {
                float pixelUpdatesPerMapUpdate = _lastPixelUpdateCount / (float)_lastMapUpdateCount;

                InfoPanelScript.InfoText.text += "\nPixel Updates Per Map Update: " + pixelUpdatesPerMapUpdate.ToString("0.00");
            }

            InfoPanelScript.InfoText.text += "\n";
            InfoPanelScript.InfoText.text += "\nSimulated Time Per RTS:";
            InfoPanelScript.InfoText.text += "\n" + Manager.GetTimeSpanString(_lastDateSpan);
            InfoPanelScript.InfoText.text += "\n";
        }
    }

    public void AddCellDataToInfoPanel(int longitude, int latitude)
    {
        TerrainCell cell = Manager.CurrentWorld.GetCell(longitude, latitude);

        if (cell == null) return;

        AddCellDataToInfoPanel(cell);
    }

    public void AddCellDataToInfoPanel_Terrain (TerrainCell cell) {
		
		float cellArea = cell.Area;

		InfoPanelScript.InfoText.text += "\n";
		InfoPanelScript.InfoText.text += "\n -- Cell Terrain Data -- ";
		InfoPanelScript.InfoText.text += "\n";

		InfoPanelScript.InfoText.text += "\nArea: " + cellArea + " Km^2";
		InfoPanelScript.InfoText.text += "\nAltitude: " + cell.Altitude + " meters";
		InfoPanelScript.InfoText.text += "\nRainfall: " + cell.Rainfall + " mm / year";
		InfoPanelScript.InfoText.text += "\nTemperature: " + cell.Temperature + " C";
		InfoPanelScript.InfoText.text += "\n";

		for (int i = 0; i < cell.PresentBiomeNames.Count; i++) {
			float percentage = cell.BiomePresences [i];

			InfoPanelScript.InfoText.text += "\nBiome: " + cell.PresentBiomeNames [i];
			InfoPanelScript.InfoText.text += " (" + percentage.ToString ("P") + ")";
		}

		InfoPanelScript.InfoText.text += "\n";
		InfoPanelScript.InfoText.text += "\nSurvivability: " + cell.Survivability.ToString ("P");
		InfoPanelScript.InfoText.text += "\nForaging Capacity: " + cell.ForagingCapacity.ToString ("P");
		InfoPanelScript.InfoText.text += "\nAccessibility: " + cell.Accessibility.ToString ("P");
		InfoPanelScript.InfoText.text += "\nArability: " + cell.Arability.ToString ("P");
		InfoPanelScript.InfoText.text += "\n";

		Region region = cell.Region;

		if (region == null) {
			InfoPanelScript.InfoText.text += "\nCell doesn't belong to any known region";
		} else {
			InfoPanelScript.InfoText.text += "\nCell is part of Region #" + region.Id + ": " + region.Name;
		}
	}

	public void AddCellDataToInfoPanel_Region (TerrainCell cell) {

		Region region = cell.Region;

		InfoPanelScript.InfoText.text += "\n";
		InfoPanelScript.InfoText.text += "\n -- Region Terrain Data -- ";
		InfoPanelScript.InfoText.text += "\n";

		if (region == null) {
			InfoPanelScript.InfoText.text += "\nCell doesn't belong to any known region";

			return;
		} else {
			InfoPanelScript.InfoText.text += "\nRegion #" + region.Id + ": " + region.Name;
		}
		InfoPanelScript.InfoText.text += "\n";
		InfoPanelScript.InfoText.text += "\nAttributes: ";

		bool first = true;
		foreach (RegionAttribute attr in region.Attributes) {

			if (first) {
				InfoPanelScript.InfoText.text += attr.Name;
				first = false;
			} else {
				InfoPanelScript.InfoText.text += ", " + attr.Name;
			}
		}

		InfoPanelScript.InfoText.text += "\n";
		InfoPanelScript.InfoText.text += "\nTotal Area: " + region.TotalArea + " Km^2";

		InfoPanelScript.InfoText.text += "\n";
		InfoPanelScript.InfoText.text += "\nCoast Percentage: " + region.CoastPercentage.ToString ("P");
		InfoPanelScript.InfoText.text += "\nOcean Percentage: " + region.OceanPercentage.ToString ("P");

		InfoPanelScript.InfoText.text += "\n";
		InfoPanelScript.InfoText.text += "\nAverage Altitude: " + region.AverageAltitude + " meters";
		InfoPanelScript.InfoText.text += "\nAverage Rainfall: " + region.AverageRainfall + " mm / year";
		InfoPanelScript.InfoText.text += "\nAverage Temperature: " + region.AverageTemperature + " C";
		InfoPanelScript.InfoText.text += "\n";

		InfoPanelScript.InfoText.text += "\nMin Region Altitude: " + region.MinAltitude + " meters";
		InfoPanelScript.InfoText.text += "\nMax Region Altitude: " + region.MaxAltitude + " meters";
		InfoPanelScript.InfoText.text += "\nAverage Border Altitude: " + region.AverageOuterBorderAltitude + " meters";
		InfoPanelScript.InfoText.text += "\n";

		for (int i = 0; i < region.PresentBiomeNames.Count; i++) {
			float percentage = region.BiomePresences [i];

			InfoPanelScript.InfoText.text += "\nBiome: " + region.PresentBiomeNames [i];
			InfoPanelScript.InfoText.text += " (" + percentage.ToString ("P") + ")";
		}

		InfoPanelScript.InfoText.text += "\n";
		InfoPanelScript.InfoText.text += "\nAverage Survivability: " + region.AverageSurvivability.ToString ("P");
		InfoPanelScript.InfoText.text += "\nAverage Foraging Capacity: " + region.AverageForagingCapacity.ToString ("P");
		InfoPanelScript.InfoText.text += "\nAverage Accessibility: " + region.AverageAccessibility.ToString ("P");
		InfoPanelScript.InfoText.text += "\nAverage Arability: " + region.AverageArability.ToString ("P");
	}

	public void AddCellDataToInfoPanel_FarmlandDistribution (TerrainCell cell) {

		float cellArea = cell.Area;
		float farmlandPercentage = cell.FarmlandPercentage;

		if (farmlandPercentage > 0) {

			InfoPanelScript.InfoText.text += "\n";
			InfoPanelScript.InfoText.text += "\n -- Cell Farmland Distribution Data -- ";
			InfoPanelScript.InfoText.text += "\n";

			InfoPanelScript.InfoText.text += "\nFarmland Percentage: " + farmlandPercentage.ToString ("P");
			InfoPanelScript.InfoText.text += "\n";
		}

		if (cell.Group == null) {

			InfoPanelScript.InfoText.text += "\n\tNo population at location";

			return;
		}

		int population = cell.Group.Population;

		if (population <= 0) {

			InfoPanelScript.InfoText.text += "\n\tNo population at location";

			return;
		}

		if (cell.FarmlandPercentage > 0) {

			float farmlandArea = farmlandPercentage * cellArea;

			InfoPanelScript.InfoText.text += "\nFarmland Area per Pop: " + (farmlandArea / (float)population).ToString ("0.000") + " Km^2 / Pop";
		}
	}

	public void AddCellDataToInfoPanel_PopDensity (TerrainCell cell) {

		float cellArea = cell.Area;

		InfoPanelScript.InfoText.text += "\n";
		InfoPanelScript.InfoText.text += "\n -- Group Population Density Data -- ";
		InfoPanelScript.InfoText.text += "\n";

		if (cell.Group == null) {

			InfoPanelScript.InfoText.text += "\n\tNo population at location";

			return;
		}

		int population = cell.Group.Population;

		if (population <= 0) {

			InfoPanelScript.InfoText.text += "\n\tNo population at location";

			return;
		}

		int optimalPopulation = cell.Group.OptimalPopulation;
        int previousPopulation = cell.Group.PreviousPopulation;

        InfoPanelScript.InfoText.text += "\nPopulation: " + population;
		InfoPanelScript.InfoText.text += "\nPrevious Population: " + previousPopulation;
        InfoPanelScript.InfoText.text += "\nPopulation Change: " + (population - previousPopulation);
        InfoPanelScript.InfoText.text += "\nOptimal Population: " + optimalPopulation;
		InfoPanelScript.InfoText.text += "\nPop Density: " + (population / cellArea).ToString ("0.000") + " Pop / Km^2";

		float modifiedSurvivability = 0;
		float modifiedForagingCapacity = 0;

		cell.Group.CalculateAdaptionToCell (cell, out modifiedForagingCapacity, out modifiedSurvivability);

		InfoPanelScript.InfoText.text += "\n";
		InfoPanelScript.InfoText.text += "\nModified Survivability: " + modifiedSurvivability.ToString ("P");
		InfoPanelScript.InfoText.text += "\nModified Foraging Capacity: " + modifiedForagingCapacity.ToString ("P");
	}

	public void AddCellDataToInfoPanel_Language (TerrainCell cell) {

		InfoPanelScript.InfoText.text += "\n";
		InfoPanelScript.InfoText.text += "\n -- Group Language Data -- ";
		InfoPanelScript.InfoText.text += "\n";

		if (cell.Group == null) {

			InfoPanelScript.InfoText.text += "\n\tNo population at location";

			return;
		}

		int population = cell.Group.Population;

		if (population <= 0) {

			InfoPanelScript.InfoText.text += "\n\tNo population at location";

			return;
		}

		Language groupLanguage = cell.Group.Culture.Language;

		if (groupLanguage == null) {

			InfoPanelScript.InfoText.text += "\n\tNo major language spoken at location";

			return;
		}

		InfoPanelScript.InfoText.text += "\n\tPredominant language at location: " + groupLanguage.Id;
	}

	public void AddCellDataToInfoPanel_UpdateSpan (TerrainCell cell) {

		InfoPanelScript.InfoText.text += "\n";
		InfoPanelScript.InfoText.text += "\n -- Group Update Span Data -- ";
		InfoPanelScript.InfoText.text += "\n";

		if (cell.Group == null) {

			InfoPanelScript.InfoText.text += "\n\tNo population at location";

			return;
		}

		int population = cell.Group.Population;

		if (population <= 0) {

			InfoPanelScript.InfoText.text += "\n\tNo population at location";

			return;
		}

		long lastUpdateDate = cell.Group.LastUpdateDate;
		long nextUpdateDate = cell.Group.NextUpdateDate;

		InfoPanelScript.InfoText.text += "\nLast Update Date: " + Manager.GetDateString(lastUpdateDate);
		InfoPanelScript.InfoText.text += "\nNext Update Date: " + Manager.GetDateString(nextUpdateDate);
		InfoPanelScript.InfoText.text += "\nTime between updates: " + Manager.GetTimeSpanString(nextUpdateDate - lastUpdateDate);
	}

	public void AddCellDataToInfoPanel_PolityProminence(TerrainCell cell)
    {
        InfoPanelScript.InfoText.text += "\n";
        InfoPanelScript.InfoText.text += "\n -- Group Polity Prominence Data -- ";
        InfoPanelScript.InfoText.text += "\n";

        if (cell.Group == null)
        {
            InfoPanelScript.InfoText.text += "\n\tNo population at location";

            return;
        }

        int population = cell.Group.Population;

        if (population <= 0)
        {
            InfoPanelScript.InfoText.text += "\n\tNo population at location";

            return;
        }

        bool firstPolity = true;

        List<PolityProminence> polityProminences = new List<PolityProminence>(cell.Group.PolityProminences.Values);

        polityProminences.Sort((a, b) =>
        {
            if (a.Value > b.Value) return -1;
            if (a.Value < b.Value) return 1;
            return 0;
        });

        foreach (PolityProminence polityProminence in polityProminences)
        {
            Polity polity = polityProminence.Polity;
            float prominenceValue = polityProminence.Value;
            float factionCoreDistance = polityProminence.FactionCoreDistance;
            float polityCoreDistance = polityProminence.PolityCoreDistance;
            float administrativeCost = polityProminence.AdministrativeCost;

            if (prominenceValue >= 0.001)
            {
                if (firstPolity)
                {
                    InfoPanelScript.InfoText.text += "\nPolities:";

                    firstPolity = false;
                }

                InfoPanelScript.InfoText.text += "\n\tPolity: " + polity.Name.Text +
                    "\n\t\tProminence: " + prominenceValue.ToString("P") +
                    "\n\t\tDistance to Polity Core: " + polityCoreDistance.ToString("0.000") +
                    "\n\t\tDistance to Faction Core: " + factionCoreDistance.ToString("0.000") +
                    "\n\t\tAdministrative Cost: " + administrativeCost.ToString("0.000");
            }
        }
    }

    public void AddCellDataToInfoPanel_PolityContacts (TerrainCell cell) {

		InfoPanelScript.InfoText.text += "\n";
		InfoPanelScript.InfoText.text += "\n -- Polity Contacts Data -- ";
		InfoPanelScript.InfoText.text += "\n";

		if (cell.Group == null) {

			InfoPanelScript.InfoText.text += "\n\tNo population at location";

			return;
		}

		int population = cell.Group.Population;

		if (population <= 0) {

			InfoPanelScript.InfoText.text += "\n\tNo population at location";

			return;
		}

		Territory territory = cell.EncompassingTerritory;


		if (territory == null) {
			InfoPanelScript.InfoText.text += "\n\tGroup not part of a polity's territory";
			return;
		}

		Polity polity = territory.Polity;

		InfoPanelScript.InfoText.text += "\nTerritory of the " + polity.Name.Text + " " + polity.Type.ToLower ();
		InfoPanelScript.InfoText.text += "\nTranslates to: " + polity.Name.Meaning;
		InfoPanelScript.InfoText.text += "\n";

		if (polity.Contacts.Count <= 0) {

			InfoPanelScript.InfoText.text += "\nPolity has no contact with other polities...";
		} else {

			InfoPanelScript.InfoText.text += "\nPolities in contact:";
		}

		foreach (PolityContact contact in polity.Contacts.Values)
        {
			Polity contactPolity = contact.Polity;

			InfoPanelScript.InfoText.text += "\n\n\tPolity: " + contactPolity.Name.Text + " " + contactPolity.Type.ToLower ();

			Faction dominantFaction = contactPolity.DominantFaction;

			InfoPanelScript.InfoText.text += "\n\tDominant Faction: " + dominantFaction.Type + " " + dominantFaction.Name;

			Agent leader = contactPolity.CurrentLeader;

			InfoPanelScript.InfoText.text += "\n\tLeader: " + leader.Name.Text;

			InfoPanelScript.InfoText.text += "\n\tContact Strength: " + contact.GroupCount;
		}
	}

	public void AddCellDataToInfoPanel_General (TerrainCell cell) {
		
		InfoPanelScript.InfoText.text += "\n";
		InfoPanelScript.InfoText.text += "\n";

		if (cell.Group == null) {

			InfoPanelScript.InfoText.text += "Uninhabited land";

			return;
		}

		int cellPopulation = cell.Group.Population;

		if (cellPopulation <= 0) {

			InfoPanelScript.InfoText.text += "Group has zero population";
			Debug.LogError ("Group has zero or less population: " + cellPopulation);

			return;
		}

		Territory territory = cell.EncompassingTerritory;

		if (territory == null) {
			
			InfoPanelScript.InfoText.text += "Disorganized bands";
			InfoPanelScript.InfoText.text += "\n";
			InfoPanelScript.InfoText.text += "\n";

			InfoPanelScript.InfoText.text += cellPopulation + " inhabitants in selected cell";
		
		} else {

			Polity polity = territory.Polity;

			InfoPanelScript.InfoText.text += "Territory of the " + polity.Name.Text + " " + polity.Type.ToLower ();
			InfoPanelScript.InfoText.text += "\nTranslates to: " + polity.Name.Meaning;
			InfoPanelScript.InfoText.text += "\n";

			Agent leader = polity.CurrentLeader;

			InfoPanelScript.InfoText.text += "\nLeader: " + leader.Name.Text;
			InfoPanelScript.InfoText.text += "\nTranslates to: " + leader.Name.Meaning;
			InfoPanelScript.InfoText.text += "\nBirth Date: " + Manager.GetDateString(leader.BirthDate);
			InfoPanelScript.InfoText.text += "\nAge: " + Manager.GetTimeSpanString(leader.Age);
			InfoPanelScript.InfoText.text += "\nGender: " + ((leader.IsFemale) ? "Female" : "Male");
			InfoPanelScript.InfoText.text += "\nCharisma: " + leader.Charisma;
			InfoPanelScript.InfoText.text += "\nWisdom: " + leader.Wisdom;
			InfoPanelScript.InfoText.text += "\n";
			InfoPanelScript.InfoText.text += "\n";

			int polPopulation = (int)polity.TotalPopulation;

			if (polity.Type == Tribe.PolityType) {
				InfoPanelScript.InfoText.text += polPopulation + " tribe members";
			} else {
				InfoPanelScript.InfoText.text += polPopulation + " polity citizens";
			}

//			SetFocusButton (polity);
		}
	}

    public void AddCellDataToInfoPanel_PolityTerritory(TerrainCell cell)
    {
        InfoPanelScript.InfoText.text += "\n";
        InfoPanelScript.InfoText.text += "\n -- Polity Territory Data -- ";
        InfoPanelScript.InfoText.text += "\n";

        if (cell.Group == null)
        {
            InfoPanelScript.InfoText.text += "\n\tNo population at location";

            return;
        }

        int population = cell.Group.Population;

        if (population <= 0)
        {
            InfoPanelScript.InfoText.text += "\n\tNo population at location";

            return;
        }

        Territory territory = cell.EncompassingTerritory;
        
        if (territory == null)
        {
            InfoPanelScript.InfoText.text += "\n\tGroup not part of a polity's territory";
            return;
        }

        Polity polity = territory.Polity;

        PolityProminence pi = cell.Group.GetPolityProminence(polity);

        InfoPanelScript.InfoText.text += "\nTerritory of the " + polity.Name.Text + " " + polity.Type.ToLower();
        InfoPanelScript.InfoText.text += "\nTranslates to: " + polity.Name.Meaning;
        InfoPanelScript.InfoText.text += "\n";

        int totalPopulation = (int)Mathf.Floor(polity.TotalPopulation);

        InfoPanelScript.InfoText.text += "\n\tPolity population: " + totalPopulation;
        InfoPanelScript.InfoText.text += "\n";

        float administrativeCost = polity.TotalAdministrativeCost;

        InfoPanelScript.InfoText.text += "\n\tAdministrative Cost: " + administrativeCost;

        Agent leader = polity.CurrentLeader;

        InfoPanelScript.InfoText.text += "\nLeader: " + leader.Name.Text;
        InfoPanelScript.InfoText.text += "\nTranslates to: " + leader.Name.Meaning;
        InfoPanelScript.InfoText.text += "\nBirth Date: " + Manager.GetDateString(leader.BirthDate);
        InfoPanelScript.InfoText.text += "\nGender: " + ((leader.IsFemale) ? "Female" : "Male");
        InfoPanelScript.InfoText.text += "\nCharisma: " + leader.Charisma;
        InfoPanelScript.InfoText.text += "\nWisdom: " + leader.Wisdom;
        InfoPanelScript.InfoText.text += "\n";

        InfoPanelScript.InfoText.text += "\n";
        InfoPanelScript.InfoText.text += "\n -- Polity Factions -- ";
        InfoPanelScript.InfoText.text += "\n";

        List<Faction> factions = new List<Faction>(polity.GetFactions());

        factions.Sort((a, b) =>
        {
            if (a.Influence > b.Influence)
                return -1;
            if (a.Influence < b.Influence)
                return 1;

            return 0;
        });

        foreach (Faction faction in factions)
        {
            InfoPanelScript.InfoText.text += "\n\t" + faction.Type + " " + faction.Name;
            InfoPanelScript.InfoText.text += "\n\t\tCore: " + faction.CoreGroup.Position;
            InfoPanelScript.InfoText.text += "\n\t\tInfluence: " + faction.Influence.ToString("P");

            Agent factionLeader = faction.CurrentLeader;

            InfoPanelScript.InfoText.text += "\n\t\tLeader: " + factionLeader.Name.Text;
            InfoPanelScript.InfoText.text += "\n\t\tTranslates to: " + factionLeader.Name.Meaning;
            InfoPanelScript.InfoText.text += "\n\t\tBirth Date: " + Manager.GetDateString(factionLeader.BirthDate);
            InfoPanelScript.InfoText.text += "\n\t\tGender: " + ((factionLeader.IsFemale) ? "Female" : "Male");
            InfoPanelScript.InfoText.text += "\n\t\tCharisma: " + factionLeader.Charisma;
            InfoPanelScript.InfoText.text += "\n\t\tWisdom: " + factionLeader.Wisdom;
            InfoPanelScript.InfoText.text += "\n";
        }

        InfoPanelScript.InfoText.text += "\n";
        InfoPanelScript.InfoText.text += "\n -- Selected Group's Polity Data -- ";
        InfoPanelScript.InfoText.text += "\n";

        float percentageOfPopulation = cell.Group.GetPolityProminenceValue(polity);
        int prominencedPopulation = (int)Mathf.Floor(population * percentageOfPopulation);

        float percentageOfPolity = 1;

        if (totalPopulation > 0)
        {
            percentageOfPolity = prominencedPopulation / (float)totalPopulation;
        }

        InfoPanelScript.InfoText.text += "\n\tProminenced population: " + prominencedPopulation;
        InfoPanelScript.InfoText.text += "\n\tPercentage of polity population: " + percentageOfPolity.ToString("P");
        InfoPanelScript.InfoText.text += "\n\tDistance to polity core: " + pi.PolityCoreDistance.ToString("0.000");
        InfoPanelScript.InfoText.text += "\n\tDistance to faction core: " + pi.FactionCoreDistance.ToString("0.000");
    }

    public void AddCellDataToInfoPanel_PolityClusters(TerrainCell cell)
    {
        InfoPanelScript.InfoText.text += "\n";
        InfoPanelScript.InfoText.text += "\n -- Group Polity Clusters Data -- ";
        InfoPanelScript.InfoText.text += "\n";

        if (cell.Group == null)
        {
            InfoPanelScript.InfoText.text += "\n\tNo population at location";

            return;
        }

        int population = cell.Group.Population;

        if (population <= 0)
        {
            InfoPanelScript.InfoText.text += "\n\tNo population at location";

            return;
        }

        bool firstPolity = true;

        List<PolityProminence> polityProminences = new List<PolityProminence>(cell.Group.PolityProminences.Values);

        polityProminences.Sort((a, b) =>
        {
            if (a.Value > b.Value) return -1;
            if (a.Value < b.Value) return 1;
            return 0;
        });

        foreach (PolityProminence polityProminence in polityProminences)
        {
            Polity polity = polityProminence.Polity;
            float prominenceValue = polityProminence.Value;
            PolityProminenceCluster prominenceCluster = polityProminence.Cluster;

            if (prominenceValue >= 0.001)
            {
                if (firstPolity)
                {
                    InfoPanelScript.InfoText.text += "\nPolities:";

                    firstPolity = false;
                }

                InfoPanelScript.InfoText.text += "\n\tPolity: " + polity.Name.Text +
                    "\n\t\tProminence: " + prominenceValue.ToString("P");

                if (prominenceCluster != null)
                {
                    InfoPanelScript.InfoText.text += "\n\t\tCluster: " + prominenceCluster.Id.ToString();
                }
                else
                {
                    InfoPanelScript.InfoText.text += "\n\t\tCluster: null";
                }
            }
        }
    }

    public void AddCellDataToInfoPanel_PolityCulturalPreference(TerrainCell cell)
    {
        InfoPanelScript.InfoText.text += "\n";
        InfoPanelScript.InfoText.text += "\n -- Polity Preference Data -- ";
        InfoPanelScript.InfoText.text += "\n";

        if (cell.Group == null)
        {
            InfoPanelScript.InfoText.text += "\n\tNo population at location";

            return;
        }

        int population = cell.Group.Population;

        if (population <= 0)
        {
            InfoPanelScript.InfoText.text += "\n\tNo population at location";

            return;
        }

        PolityProminence polityProminence = cell.Group.HighestPolityProminence;

        if (polityProminence == null)
        {
            InfoPanelScript.InfoText.text += "\n\tGroup not part of a polity";

            return;
        }

        bool firstPreference = true;

        foreach (CulturalPreference preference in polityProminence.Polity.Culture.Preferences.Values)
        {
            if (firstPreference)
            {
                InfoPanelScript.InfoText.text += "\nPreferences:";

                firstPreference = false;
            }

            InfoPanelScript.InfoText.text += "\n\t" + preference.Name + " Preference: " + preference.Value.ToString("P");
        }
    }

    public void AddCellDataToInfoPanel_PolityCulturalActivity(TerrainCell cell)
    {
        InfoPanelScript.InfoText.text += "\n";
        InfoPanelScript.InfoText.text += "\n -- Polity Activity Data -- ";
        InfoPanelScript.InfoText.text += "\n";

        if (cell.Group == null)
        {
            InfoPanelScript.InfoText.text += "\n\tNo population at location";

            return;
        }

        int population = cell.Group.Population;

        if (population <= 0)
        {
            InfoPanelScript.InfoText.text += "\n\tNo population at location";

            return;
        }

        PolityProminence polityProminence = cell.Group.HighestPolityProminence;

        if (polityProminence == null)
        {
            InfoPanelScript.InfoText.text += "\n\tGroup not part of a polity";

            return;
        }

        bool firstActivity = true;

        foreach (CulturalActivity activity in polityProminence.Polity.Culture.Activities.Values)
        {
            if (firstActivity)
            {
                InfoPanelScript.InfoText.text += "\nActivities:";

                firstActivity = false;
            }

            InfoPanelScript.InfoText.text += "\n\t" + activity.Name + " Contribution: " + activity.Contribution.ToString("P");
        }
    }

    public void AddCellDataToInfoPanel_PopCulturalPreference(TerrainCell cell)
    {
        InfoPanelScript.InfoText.text += "\n";
        InfoPanelScript.InfoText.text += "\n -- Group Preference Data -- ";
        InfoPanelScript.InfoText.text += "\n";

        if (cell.Group == null)
        {
            InfoPanelScript.InfoText.text += "\n\tNo population at location";

            return;
        }

        int population = cell.Group.Population;

        if (population <= 0)
        {
            InfoPanelScript.InfoText.text += "\n\tNo population at location";

            return;
        }

        bool firstPreference = true;

        foreach (CulturalPreference preference in cell.Group.Culture.Preferences.Values)
        {
            if (firstPreference)
            {
                InfoPanelScript.InfoText.text += "\nPreferences:";

                firstPreference = false;
            }

            InfoPanelScript.InfoText.text += "\n\t" + preference.Name + " Preference: " + preference.Value.ToString("P");
        }
    }

    public void AddCellDataToInfoPanel_PopCulturalActivity(TerrainCell cell)
    {
        InfoPanelScript.InfoText.text += "\n";
        InfoPanelScript.InfoText.text += "\n -- Group Activity Data -- ";
        InfoPanelScript.InfoText.text += "\n";

        if (cell.Group == null)
        {
            InfoPanelScript.InfoText.text += "\n\tNo population at location";

            return;
        }

        int population = cell.Group.Population;

        if (population <= 0)
        {
            InfoPanelScript.InfoText.text += "\n\tNo population at location";

            return;
        }

        bool firstActivity = true;

        foreach (CulturalActivity activity in cell.Group.Culture.Activities.Values)
        {
            if (firstActivity)
            {
                InfoPanelScript.InfoText.text += "\nActivities:";

                firstActivity = false;
            }

            InfoPanelScript.InfoText.text += "\n\t" + activity.Name + " Contribution: " + activity.Contribution.ToString("P");
        }
    }

    public void AddCellDataToInfoPanel_PolityCulturalSkill(TerrainCell cell)
    {
        InfoPanelScript.InfoText.text += "\n";
        InfoPanelScript.InfoText.text += "\n -- Polity Skill Data -- ";
        InfoPanelScript.InfoText.text += "\n";

        if (cell.Group == null)
        {
            InfoPanelScript.InfoText.text += "\n\tNo population at location";

            return;
        }

        int population = cell.Group.Population;

        if (population <= 0)
        {
            InfoPanelScript.InfoText.text += "\n\tNo population at location";

            return;
        }

        PolityProminence polityProminence = cell.Group.HighestPolityProminence;

        if (polityProminence == null)
        {
            InfoPanelScript.InfoText.text += "\n\tGroup not part of a polity";

            return;
        }

        bool firstSkill = true;

        foreach (CulturalSkill skill in polityProminence.Polity.Culture.Skills.Values)
        {
            float skillValue = skill.Value;

            if (skillValue >= 0.001)
            {
                if (firstSkill)
                {
                    InfoPanelScript.InfoText.text += "\nSkills:";

                    firstSkill = false;
                }

                InfoPanelScript.InfoText.text += "\n\t" + skill.Name + " Value: " + skill.Value.ToString("0.000");
            }
        }
    }

    public void AddCellDataToInfoPanel_PopCulturalSkill(TerrainCell cell)
    {
        InfoPanelScript.InfoText.text += "\n";
        InfoPanelScript.InfoText.text += "\n -- Group Skill Data -- ";
        InfoPanelScript.InfoText.text += "\n";

        if (cell.Group == null)
        {
            InfoPanelScript.InfoText.text += "\n\tNo population at location";

            return;
        }

        int population = cell.Group.Population;

        if (population <= 0)
        {
            InfoPanelScript.InfoText.text += "\n\tNo population at location";

            return;
        }

        bool firstSkill = true;

        foreach (CulturalSkill skill in cell.Group.Culture.Skills.Values)
        {
            float skillValue = skill.Value;

            if (skillValue >= 0.001)
            {
                if (firstSkill)
                {
                    InfoPanelScript.InfoText.text += "\nSkills:";

                    firstSkill = false;
                }

                InfoPanelScript.InfoText.text += "\n\t" + skill.Name + " Value: " + skill.Value.ToString("0.000");
            }
        }
    }

    public void AddCellDataToInfoPanel_PolityCulturalKnowledge(TerrainCell cell)
    {
        InfoPanelScript.InfoText.text += "\n";
        InfoPanelScript.InfoText.text += "\n -- Polity Knowledge Data -- ";
        InfoPanelScript.InfoText.text += "\n";

        if (cell.Group == null)
        {
            InfoPanelScript.InfoText.text += "\n\tNo population at location";

            return;
        }

        int population = cell.Group.Population;

        if (population <= 0)
        {
            InfoPanelScript.InfoText.text += "\n\tNo population at location";

            return;
        }

        PolityProminence polityProminence = cell.Group.HighestPolityProminence;

        if (polityProminence == null)
        {
            InfoPanelScript.InfoText.text += "\n\tGroup not part of a polity";

            return;
        }

        bool firstKnowledge = true;

        foreach (CulturalKnowledge knowledge in polityProminence.Polity.Culture.Knowledges.Values)
        {
            float knowledgeValue = knowledge.ScaledValue;

            if (firstKnowledge)
            {
                InfoPanelScript.InfoText.text += "\nKnowledges:";

                firstKnowledge = false;
            }

            InfoPanelScript.InfoText.text += "\n\t" + knowledge.Name + " Value: " + knowledgeValue.ToString("0.000");
        }
    }

    public void AddCellDataToInfoPanel_PopCulturalKnowledge(TerrainCell cell)
    {
        InfoPanelScript.InfoText.text += "\n";
        InfoPanelScript.InfoText.text += "\n -- Group Knowledge Data -- ";
        InfoPanelScript.InfoText.text += "\n";

        if (cell.Group == null)
        {
            InfoPanelScript.InfoText.text += "\n\tNo population at location";

            return;
        }

        int population = cell.Group.Population;

        if (population <= 0)
        {
            InfoPanelScript.InfoText.text += "\n\tNo population at location";

            return;
        }

        bool firstKnowledge = true;

        foreach (CulturalKnowledge knowledge in cell.Group.Culture.Knowledges.Values)
        {
            float knowledgeValue = knowledge.ScaledValue;

            if (firstKnowledge)
            {
                InfoPanelScript.InfoText.text += "\nKnowledges:";

                firstKnowledge = false;
            }

            InfoPanelScript.InfoText.text += "\n\t" + knowledge.Name + " Value: " + knowledgeValue.ToString("0.000");
        }
    }

    public void AddCellDataToInfoPanel_PolityCulturalDiscovery(TerrainCell cell)
    {
        InfoPanelScript.InfoText.text += "\n";
        InfoPanelScript.InfoText.text += "\n -- Polity Discovery Data -- ";
        InfoPanelScript.InfoText.text += "\n";

        if (cell.Group == null)
        {
            InfoPanelScript.InfoText.text += "\n\tNo population at location";

            return;
        }

        int population = cell.Group.Population;

        if (population <= 0)
        {
            InfoPanelScript.InfoText.text += "\n\tNo population at location";

            return;
        }

        PolityProminence polityProminence = cell.Group.HighestPolityProminence;

        if (polityProminence == null)
        {
            InfoPanelScript.InfoText.text += "\n\tGroup not part of a polity";

            return;
        }

        bool firstDiscovery = true;

        foreach (CulturalDiscovery discovery in polityProminence.Polity.Culture.Discoveries.Values)
        {
            if (firstDiscovery)
            {
                InfoPanelScript.InfoText.text += "\nDiscoveries:";

                firstDiscovery = false;
            }

            InfoPanelScript.InfoText.text += "\n\t" + discovery.Name;
        }
    }

    public void AddCellDataToInfoPanel_PopCulturalDiscovery(TerrainCell cell)
    {
        InfoPanelScript.InfoText.text += "\n";
        InfoPanelScript.InfoText.text += "\n -- Group Discovery Data -- ";
        InfoPanelScript.InfoText.text += "\n";

        if (cell.Group == null)
        {
            InfoPanelScript.InfoText.text += "\n\tNo population at location";

            return;
        }

        int population = cell.Group.Population;

        if (population <= 0)
        {
            InfoPanelScript.InfoText.text += "\n\tNo population at location";

            return;
        }

        bool firstDiscovery = true;

        foreach (CulturalDiscovery discovery in cell.Group.Culture.Discoveries.Values)
        {
            if (firstDiscovery)
            {
                InfoPanelScript.InfoText.text += "\nDiscoveries:";

                firstDiscovery = false;
            }

            InfoPanelScript.InfoText.text += "\n\t" + discovery.Name;
        }
    }

    public void AddCellDataToInfoPanel(TerrainCell cell)
    {
        int longitude = cell.Longitude;
        int latitude = cell.Latitude;

        InfoPanelScript.InfoText.text += "\n";
        InfoPanelScript.InfoText.text += string.Format("\nPosition: Longitude {0}, Latitude {1}", longitude, latitude);

        if ((_planetOverlay == PlanetOverlay.None) ||
            (_planetOverlay == PlanetOverlay.Rainfall) ||
            (_planetOverlay == PlanetOverlay.Arability) ||
            (_planetOverlay == PlanetOverlay.Temperature))
        {
            AddCellDataToInfoPanel_Terrain(cell);
        }

        if (_planetOverlay == PlanetOverlay.Region)
        {
            AddCellDataToInfoPanel_Region(cell);
        }

        if (_planetOverlay == PlanetOverlay.Language)
        {
            AddCellDataToInfoPanel_Language(cell);
        }

        if (_planetOverlay == PlanetOverlay.FarmlandDistribution)
        {
            AddCellDataToInfoPanel_FarmlandDistribution(cell);
        }


        if (_planetOverlay == PlanetOverlay.General)
        {
            AddCellDataToInfoPanel_General(cell);
        }

        if ((_planetOverlay == PlanetOverlay.PopDensity) ||
            (_planetOverlay == PlanetOverlay.PopChange))
        {
            AddCellDataToInfoPanel_PopDensity(cell);
        }

        if (_planetOverlay == PlanetOverlay.UpdateSpan)
        {
            AddCellDataToInfoPanel_UpdateSpan(cell);
        }

        if (_planetOverlay == PlanetOverlay.PolityProminence)
        {
            AddCellDataToInfoPanel_PolityProminence(cell);
        }

        if (_planetOverlay == PlanetOverlay.PolityContacts)
        {
            AddCellDataToInfoPanel_PolityContacts(cell);
        }

        if (_planetOverlay == PlanetOverlay.PolityTerritory)
        {
            AddCellDataToInfoPanel_PolityTerritory(cell);
        }

        if (_planetOverlay == PlanetOverlay.PolityCluster)
        {
            AddCellDataToInfoPanel_PolityClusters(cell);
        }

        if (_planetOverlay == PlanetOverlay.FactionCoreDistance)
        {
            AddCellDataToInfoPanel_PolityTerritory(cell);
        }

        if (_planetOverlay == PlanetOverlay.PolityCulturalPreference)
        {
            AddCellDataToInfoPanel_PolityCulturalPreference(cell);
        }

        if (_planetOverlay == PlanetOverlay.PopCulturalPreference)
        {
            AddCellDataToInfoPanel_PopCulturalPreference(cell);
        }

        if (_planetOverlay == PlanetOverlay.PolityCulturalActivity)
        {
            AddCellDataToInfoPanel_PolityCulturalActivity(cell);
        }

        if (_planetOverlay == PlanetOverlay.PopCulturalActivity)
        {
            AddCellDataToInfoPanel_PopCulturalActivity(cell);
        }

        if (_planetOverlay == PlanetOverlay.PolityCulturalSkill)
        {
            AddCellDataToInfoPanel_PolityCulturalSkill(cell);
        }

        if (_planetOverlay == PlanetOverlay.PopCulturalSkill)
        {
            AddCellDataToInfoPanel_PopCulturalSkill(cell);
        }

        if (_planetOverlay == PlanetOverlay.PolityCulturalKnowledge)
        {
            AddCellDataToInfoPanel_PolityCulturalKnowledge(cell);
        }

        if (_planetOverlay == PlanetOverlay.PopCulturalKnowledge)
        {
            AddCellDataToInfoPanel_PopCulturalKnowledge(cell);
        }

        if (_planetOverlay == PlanetOverlay.PolityCulturalDiscovery)
        {
            AddCellDataToInfoPanel_PolityCulturalDiscovery(cell);
        }

        if (_planetOverlay == PlanetOverlay.PopCulturalDiscovery)
        {
            AddCellDataToInfoPanel_PopCulturalDiscovery(cell);
        }
    }

    public void AddCellDataToInfoPanel(Vector2 mapPosition)
    {
        int longitude = (int)mapPosition.x;
        int latitude = (int)mapPosition.y;

        AddCellDataToInfoPanel(longitude, latitude);
    }

    private TerrainCell GetCellFromPointer(Vector2 position, bool allowWrap)
    {
        Vector2 mapCoordinates;

        if (!MapScript.GetMapCoordinatesFromPointerPosition(position, out mapCoordinates, allowWrap))
            return null;

        int longitude = (int)mapCoordinates.x;
        int latitude = (int)mapCoordinates.y;

        TerrainCell cell = Manager.CurrentWorld.GetCell(longitude, latitude);

        if (cell == null)
        {
            throw new System.Exception("Unable to get cell at [" + longitude + "," + latitude + "]");
        }

        return cell;
    }

    public void ShowCellInfoToolTip_PolityTerritory(TerrainCell cell)
    {
        if (cell.EncompassingTerritory == _lastHoveredOverTerritory)
            return;

        _lastHoveredOverTerritory = cell.EncompassingTerritory;

        if (_lastHoveredOverTerritory == null)
        {
            InfoTooltipScript.SetVisible(false);
            return;
        }

        Polity polity = _lastHoveredOverTerritory.Polity;

        if (polity == null)
        {
            throw new System.Exception("Polity can't be null");
        }

        Vector3 tooltipPos = MapScript.GetScreenPositionFromMapCoordinates(polity.CoreGroup.Cell.Position) + _tooltipOffset;

        if (polity.Name == null)
        {
            throw new System.Exception("Polity.Name can't be null");
        }

        if (polity.Name.Text == null)
        {
            throw new System.Exception("polity.Name.Text can't be null");
        }

        switch (_planetOverlay)
        {
            case PlanetOverlay.General:
                InfoTooltipScript.DisplayTip(polity.Name.Text, tooltipPos);
                break;
            case PlanetOverlay.PolityTerritory:
                ShowCellInfoToolTip_PolityTerritory(polity, tooltipPos);
                break;
            case PlanetOverlay.PolityContacts:
                ShowCellInfoToolTip_PolityContacts(polity, tooltipPos);
                break;
            case PlanetOverlay.PolityCulturalPreference:
                ShowCellInfoToolTip_PolityCulturalPreference(polity, tooltipPos);
                break;
            case PlanetOverlay.PolityCulturalActivity:
                ShowCellInfoToolTip_PolityCulturalActivity(polity, tooltipPos);
                break;
            case PlanetOverlay.PolityCulturalSkill:
                ShowCellInfoToolTip_PolityCulturalSkill(polity, tooltipPos);
                break;
            case PlanetOverlay.PolityCulturalKnowledge:
                ShowCellInfoToolTip_PolityCulturalKnowledge(polity, tooltipPos);
                break;
            case PlanetOverlay.PolityCulturalDiscovery:
                ShowCellInfoToolTip_PolityCulturalDiscovery(polity, tooltipPos);
                break;
            default:
                InfoTooltipScript.SetVisible(false);
                break;
        }
    }

    public void ShowCellInfoToolTip_PolityTerritory(Polity polity, Vector3 position, float fadeStart = 5)
    {
        string text = polity.Name.Text + " " + polity.Type.ToLower() + "\n\nFaction Influences:";

        foreach (Faction faction in polity.GetFactions())
        {
            text += "\n " + faction.Name.Text + ": " + faction.Influence.ToString("P");
        }

        InfoTooltipScript.DisplayTip(text, position, fadeStart);
    }

    public void ShowCellInfoToolTip_PolityContacts(Polity polity, Vector3 position, float fadeStart = 5)
    {
        string polityTitle = polity.Name.Text + " " + polity.Type.ToLower();

        string text;

        Territory selectedTerritory = Manager.CurrentWorld.SelectedTerritory;

        float relationshipValue = 0;
        int groupCount = 0;

        if ((polity.Territory != selectedTerritory) && (selectedTerritory != null))
        {
            groupCount = selectedTerritory.Polity.GetContactGroupCount(polity);
        }

        if (groupCount > 0)
        {
            relationshipValue = selectedTerritory.Polity.GetRelationshipValue(polity);

            text = "Neighboring polity: " + polityTitle;

            text += "\n\nRelationship Value: " + relationshipValue.ToString("0.000");
            text += "\n\nOverlaping groups: " + groupCount;

        }
        else
        {
            text = polityTitle;
        }

        InfoTooltipScript.DisplayTip(text, position, fadeStart);
    }

    public void ShowCellInfoToolTip_PolityCulturalPreference(Polity polity, Vector3 position, float fadeStart = 5)
    {
        CulturalPreference preference = polity.Culture.GetPreference(_planetOverlaySubtype);

        if (preference != null)
        {
            string text = preference.Name + " Preference: " + preference.Value.ToString("0.00") + "\n\nFactions:";

            foreach (Faction faction in polity.GetFactions())
            {
                float value = faction.GetPreferenceValue(_planetOverlaySubtype);

                text += "\n " + faction.Name.Text + ": " + value.ToString("0.00");
            }

            InfoTooltipScript.DisplayTip(text, position, fadeStart);

        }
        else
        {
            InfoTooltipScript.SetVisible(false);
        }
    }

    public void ShowCellInfoToolTip_PolityCulturalActivity(Polity polity, Vector3 position, float fadeStart = 5)
    {
        CulturalActivity activity = polity.Culture.GetActivity(_planetOverlaySubtype);

        if (activity != null)
        {
            string text = activity.Name + " Contribution: " + activity.Contribution.ToString("P") + "\n\nFactions:";

            foreach (Faction faction in polity.GetFactions())
            {
                activity = faction.Culture.GetActivity(_planetOverlaySubtype);

                text += "\n " + faction.Name.Text + ": " + activity.Contribution.ToString("P");
            }

            InfoTooltipScript.DisplayTip(text, position, fadeStart);
        }
        else
        {
            InfoTooltipScript.SetVisible(false);
        }
    }

    public void ShowCellInfoToolTip_PolityCulturalSkill(Polity polity, Vector3 position, float fadeStart = 5)
    {
        CulturalSkill skill = polity.Culture.GetSkill(_planetOverlaySubtype);

        if ((skill != null) && (skill.Value >= 0.001))
        {
            string text = skill.Name + " Value: " + skill.Value.ToString("0.000") + "\n\nFactions:";

            foreach (Faction faction in polity.GetFactions())
            {
                skill = faction.Culture.GetSkill(_planetOverlaySubtype);

                text += "\n " + faction.Name.Text + ": " + skill.Value.ToString("0.000");
            }

            InfoTooltipScript.DisplayTip(text, position, fadeStart);
        }
        else
        {
            InfoTooltipScript.SetVisible(false);
        }
    }

    public void ShowCellInfoToolTip_PolityCulturalKnowledge(Polity polity, Vector3 position, float fadeStart = 5)
    {
        CulturalKnowledge knowledge = polity.Culture.GetKnowledge(_planetOverlaySubtype);
        
        if (knowledge != null)
        {
            string text = knowledge.Name + " Value: " + knowledge.ScaledValue.ToString("0.000") + "\n\nFactions:";

            foreach (Faction faction in polity.GetFactions())
            {
                float scaledValue = 0;

                faction.Culture.TryGetKnowledgeScaledValue(_planetOverlaySubtype, out scaledValue);

                text += "\n " + faction.Name.Text + ": " + scaledValue.ToString("0.000");
            }

            InfoTooltipScript.DisplayTip(text, position, fadeStart);
        }
        else
        {
            InfoTooltipScript.SetVisible(false);
        }
    }

    public void ShowCellInfoToolTip_PolityCulturalDiscovery(Polity polity, Vector3 position, float fadeStart = 5)
    {
        CulturalDiscovery discovery = polity.Culture.GetDiscovery(_planetOverlaySubtype) as CulturalDiscovery;
        
        if (discovery != null)
        {
            InfoTooltipScript.DisplayTip(discovery.Name + " is present", position, fadeStart);
        }
        else
        {
            InfoTooltipScript.SetVisible(false);
        }
    }

    public void ShowCellInfoToolTip_Region(TerrainCell cell)
    {
        if (cell.Region == _lastHoveredOverRegion)
            return;

        _lastHoveredOverRegion = cell.Region;

        if (_lastHoveredOverRegion == null)
        {
            InfoTooltipScript.SetVisible(false);
            return;
        }

        WorldPosition regionCenterCellPosition = _lastHoveredOverRegion.GetMostCenteredCell().Position;

        Vector3 tooltipPos = MapScript.GetScreenPositionFromMapCoordinates(regionCenterCellPosition) + _tooltipOffset;

        InfoTooltipScript.DisplayTip(_lastHoveredOverRegion.Name.Text, tooltipPos);
    }

    public void SelectCellOnMap(BaseEventData data)
    {
        if ((Manager.GameMode == GameMode.Editor) && Manager.EditorBrushIsVisible)
            return;

        PointerEventData pointerData = data as PointerEventData;

        if (pointerData.button != PointerEventData.InputButton.Left)
            return;

        Vector2 mapPosition;

        if (!MapScript.GetMapCoordinatesFromPointerPosition(pointerData.position, out mapPosition))
            return;

        if (_mapLeftClickOp != null)
        {
            _mapLeftClickOp(mapPosition);
        }
    }

    public void ExecuteMapHoverOps()
    {
        if (!Manager.PointerIsOverMap && !Manager.EditorBrushIsActive)
        {
            _lastHoveredCell = null;
            Manager.EditorBrushTargetCell = null;

            return;
        }

        TerrainCell hoveredCell = GetCellFromPointer(Input.mousePosition, Manager.EditorBrushIsActive);

        if (hoveredCell != _lastHoveredCell)
        {
            _lastHoveredCell = hoveredCell;
            Manager.EditorBrushTargetCell = hoveredCell;
        }

        if (hoveredCell == null)
            return;

        if (IsPolityOverlay(_planetOverlay))
            ShowCellInfoToolTip_PolityTerritory(hoveredCell);
        else if (_planetOverlay == PlanetOverlay.Region)
            ShowCellInfoToolTip_Region(hoveredCell);
    }
}
