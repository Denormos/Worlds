﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

public delegate void ProgressCastDelegate (float value, string message = null);

public interface ISynchronizable {

	void Synchronize ();
	void FinalizeLoad ();
}

[XmlRoot]
public class World : ISynchronizable {

	public const int MaxPossibleYearsToSkip = int.MaxValue / 100;
	
	public const float Circumference = 40075; // In kilometers;
	
	public const int NumContinents = 12;
	public const float ContinentMinWidthFactor = 5.7f;
	public const float ContinentMaxWidthFactor = 8.7f;
	
	public const float MinPossibleAltitude = -15000;
	public const float MaxPossibleAltitude = 15000;
	
	public const float MinPossibleRainfall = 0;
	public const float MaxPossibleRainfall = 13000;
	public const float RainfallDrynessOffsetFactor = 0.005f;
	
	public const float MinPossibleTemperature = -40;
	public const float MaxPossibleTemperature = 50;

	public const float OptimalRainfallForArability = 1000;
	public const float OptimalTemperatureForArability = 30;
	public const float MaxRainfallForArability = 7000;
	public const float MinRainfallForArability = 0;
	public const float MinTemperatureForArability = -15;
	
	public const float StartPopulationDensity = 0.5f;
	
	public const int MinStartingPopulation = 100;
	public const int MaxStartingPopulation = 100000;
	
	[XmlAttribute]
	public int Width { get; private set; }
	[XmlAttribute]
	public int Height { get; private set; }

	[XmlAttribute]
	public int Seed { get; private set; }
	
	[XmlAttribute]
	public int CurrentDate { get; private set; }

	[XmlAttribute]
	public int MaxYearsToSkip { get; private set; }
	
	[XmlAttribute]
	public long CurrentCellGroupId { get; private set; }
	
	[XmlAttribute]
	public long CurrentEventId { get; private set; }

	[XmlAttribute]
	public long CurrentPolityId { get; private set; }

	[XmlAttribute]
	public long CurrentRegionId { get; private set; }

	[XmlAttribute]
	public long CurrentLanguageId { get; private set; }
	
	[XmlAttribute]
	public int EventsToHappenCount { get; private set; }
	
	[XmlAttribute]
	public int CellGroupCount { get; private set; }

	[XmlAttribute]
	public int PolityCount { get; private set; }

	[XmlAttribute]
	public int RegionCount { get; private set; }

	[XmlAttribute]
	public int LanguageCount { get; private set; }

	[XmlAttribute]
	public int TerrainCellChangesListCount { get; private set; }

	[XmlAttribute]
	public float SeaLevelOffset { get; private set; }

	[XmlAttribute]
	public float RainfallOffset { get; private set; }

	[XmlAttribute]
	public float TemperatureOffset { get; private set; }

	// Start wonky segment (save failures might happen here)

	[XmlArrayItem (Type = typeof(UpdateCellGroupEvent)),
		XmlArrayItem (Type = typeof(MigrateGroupEvent)),
		XmlArrayItem (Type = typeof(SailingDiscoveryEvent)),
		XmlArrayItem (Type = typeof(BoatMakingDiscoveryEvent)),
		XmlArrayItem (Type = typeof(TribalismDiscoveryEvent)),
		XmlArrayItem (Type = typeof(TribeFormationEvent)),
		XmlArrayItem (Type = typeof(PlantCultivationDiscoveryEvent)),
		XmlArrayItem (Type = typeof(FarmDegradationEvent))]
	public List<WorldEvent> EventsToHappen;

	public List<TerrainCellChanges> TerrainCellChangesList = new List<TerrainCellChanges> ();

	public List<CulturalActivityInfo> CulturalActivityInfoList = new List<CulturalActivityInfo> ();
	public List<CulturalSkillInfo> CulturalSkillInfoList = new List<CulturalSkillInfo> ();
	public List<CulturalKnowledgeInfo> CulturalKnowledgeInfoList = new List<CulturalKnowledgeInfo> ();
	public List<CulturalDiscovery> CulturalDiscoveryInfoList = new List<CulturalDiscovery> ();

	public List<CellGroup> CellGroups;

	[XmlArrayItem (Type = typeof(Tribe))]
	public List<Polity> Polities;

	[XmlArrayItem (Type = typeof(CellRegion))]
	public List<Region> Regions;

	public List<Language> Languages;

	// End wonky segment 

	[XmlIgnore]
	public TerrainCell SelectedCell = null;
	[XmlIgnore]
	public Region SelectedRegion = null;
	[XmlIgnore]
	public Territory SelectedTerritory = null;
	
	[XmlIgnore]
	public float MinPossibleAltitudeWithOffset = MinPossibleAltitude - Manager.SeaLevelOffset;
	[XmlIgnore]
	public float MaxPossibleAltitudeWithOffset = MaxPossibleAltitude - Manager.SeaLevelOffset;
	
	[XmlIgnore]
	public float MinPossibleRainfallWithOffset = MinPossibleRainfall + Manager.RainfallOffset;
	[XmlIgnore]
	public float MaxPossibleRainfallWithOffset = MaxPossibleRainfall + Manager.RainfallOffset;
	
	[XmlIgnore]
	public float MinPossibleTemperatureWithOffset = MinPossibleTemperature + Manager.TemperatureOffset;
	[XmlIgnore]
	public float MaxPossibleTemperatureWithOffset = MaxPossibleTemperature + Manager.TemperatureOffset;
	
	[XmlIgnore]
	public float MaxAltitude = float.MinValue;
	[XmlIgnore]
	public float MinAltitude = float.MaxValue;
	
	[XmlIgnore]
	public float MaxRainfall = float.MinValue;
	[XmlIgnore]
	public float MinRainfall = float.MaxValue;
	
	[XmlIgnore]
	public float MaxTemperature = float.MinValue;
	[XmlIgnore]
	public float MinTemperature = float.MaxValue;
	
	[XmlIgnore]
	public TerrainCell[][] TerrainCells;
	
	[XmlIgnore]
	public CellGroup MostPopulousGroup = null;
	
	[XmlIgnore]
	public ProgressCastDelegate ProgressCastMethod { get; set; }
	
	[XmlIgnore]
	public HumanGroup MigrationTaggedGroup = null;

	private BinaryTree<int, WorldEvent> _eventsToHappen = new BinaryTree<int, WorldEvent> ();
	
	private List<IGroupAction> _groupActionsToPerform = new List<IGroupAction> ();

	private HashSet<int> _terrainCellChangesListIndexes = new HashSet<int> ();

	private HashSet<string> _culturalActivityIdList = new HashSet<string> ();
	private HashSet<string> _culturalSkillIdList = new HashSet<string> ();
	private HashSet<string> _culturalKnowledgeIdList = new HashSet<string> ();
	private HashSet<string> _culturalDiscoveryIdList = new HashSet<string> ();

	private Dictionary<long, CellGroup> _cellGroups = new Dictionary<long, CellGroup> ();
	
	private HashSet<CellGroup> _updatedGroups = new HashSet<CellGroup> ();

	private HashSet<CellGroup> _groupsToUpdate = new HashSet<CellGroup>();
	private HashSet<CellGroup> _groupsToRemove = new HashSet<CellGroup>();

	private List<MigratingGroup> _migratingGroups = new List<MigratingGroup> ();

	private Dictionary<long, Polity> _polities = new Dictionary<long, Polity> ();

	private HashSet<Polity> _politiesToUpdate = new HashSet<Polity>();
	private HashSet<Polity> _politiesToRemove = new HashSet<Polity>();

	private Dictionary<long, Region> _regions = new Dictionary<long, Region> ();

	private Dictionary<long, Language> _languages = new Dictionary<long, Language> ();

	private Vector2[] _continentOffsets;
	private float[] _continentWidths;
	private float[] _continentHeights;
	
	private float _progressIncrement = 0.25f;

	private float _accumulatedProgress = 0;

	private float _cellMaxSideLength;

	public World () {

		Manager.WorldBeingLoaded = this;
		
		ProgressCastMethod = (value, message) => {};
	}

	public World (int width, int height, int seed) {

		ProgressCastMethod = (value, message) => {};
		
		Width = width;
		Height = height;
		Seed = seed;
		
		CurrentDate = 0;
		MaxYearsToSkip = MaxPossibleYearsToSkip;
		CurrentCellGroupId = 0;
		CurrentEventId = 0;
		CurrentPolityId = 0;
		CurrentRegionId = 0;
		CurrentLanguageId = 0;
		EventsToHappenCount = 0;
		CellGroupCount = 0;
		PolityCount = 0;
		RegionCount = 0;
		TerrainCellChangesListCount = 0;

		SeaLevelOffset = Manager.SeaLevelOffset;
		RainfallOffset = Manager.RainfallOffset;
		TemperatureOffset = Manager.TemperatureOffset;
	}
	
	public void StartInitialization (float acumulatedProgress, float progressIncrement) {

		Manager.SeaLevelOffset = SeaLevelOffset;
		Manager.RainfallOffset = RainfallOffset;
		Manager.TemperatureOffset = TemperatureOffset;

		_accumulatedProgress = acumulatedProgress;
		_progressIncrement = progressIncrement;
		
		_cellMaxSideLength = Circumference / Width;
		TerrainCell.MaxArea = _cellMaxSideLength * _cellMaxSideLength;
		TerrainCell.MaxWidth = _cellMaxSideLength;
		CellGroup.TravelWidthFactor = _cellMaxSideLength;
		
		TerrainCells = new TerrainCell[Width][];
		
		for (int i = 0; i < Width; i++)
		{
			TerrainCell[] column = new TerrainCell[Height];
			
			for (int j = 0; j < Height; j++)
			{
				float alpha = (j / (float)Height) * Mathf.PI;

				float cellHeight = _cellMaxSideLength;
				float cellWidth = Mathf.Sin(alpha) * _cellMaxSideLength;

				TerrainCell cell = new TerrainCell (this, i, j, cellHeight, cellWidth);
				
				column[j] = cell;
			}
			
			TerrainCells[i] = column;
		}
		
		for (int i = 0; i < Width; i++) {
			
			for (int j = 0; j < Height; j++) {

				TerrainCell cell = TerrainCells [i] [j];
				
				cell.InitializeNeighbors();
			}
		}
		
		_continentOffsets = new Vector2[NumContinents];
		_continentHeights = new float[NumContinents];
		_continentWidths = new float[NumContinents];
		
		Manager.EnqueueTaskAndWait (() => {
			
			Random.seed = Seed;
			return true;
		});
	}

	public void FinishInitialization () {

		for (int i = 0; i < Width; i++) {

			for (int j = 0; j < Height; j++) {

				TerrainCell cell = TerrainCells [i] [j];

				cell.InitializeMiscellaneous();
			}
		}

		foreach (TerrainCellChanges changes in TerrainCellChangesList) {

			SetTerrainCellChanges (changes);
		}
	}

	public void Synchronize () {
	
		EventsToHappen = _eventsToHappen.Values;

		foreach (WorldEvent e in EventsToHappen) {

			e.Synchronize ();
		}

		CellGroups = new List<CellGroup> (_cellGroups.Values);

		foreach (CellGroup g in CellGroups) {

			g.Synchronize ();
		}

		Polities = new List<Polity> (_polities.Values);

		foreach (Polity p in Polities) {
		
			p.Synchronize ();
		}

		Regions = new List<Region> (_regions.Values);

		foreach (Region r in Regions) {

			r.Synchronize ();
		}

		Languages = new List<Language> (_languages.Values);

		foreach (Language l in Languages) {

			l.Synchronize ();
		}

		TerrainCellChangesList.Clear ();
		TerrainCellChangesListCount = 0;

		for (int i = 0; i < Width; i++) {

			for (int j = 0; j < Height; j++) {

				TerrainCell cell = TerrainCells [i] [j];

				GetTerrainCellChanges (cell);
			}
		}
	}

	public void GetTerrainCellChanges (TerrainCell cell) {

		TerrainCellChanges changes = cell.GetChanges ();

		if (changes == null)
			return;
	
		int index = changes.Longitude + (changes.Latitude * Width);

		if (!_terrainCellChangesListIndexes.Add (index))
			return;

		TerrainCellChangesList.Add (changes);

		TerrainCellChangesListCount++;
	}

	public void SetTerrainCellChanges (TerrainCellChanges changes) {

		TerrainCell cell = TerrainCells [changes.Longitude] [changes.Latitude];

		cell.SetChanges (changes);
	}

//	public void AddGroupActionToPerform (KnowledgeTransferAction action) {
//	
//		_groupActionsToPerform.Add (action);
//	}

	public void AddExistingCulturalActivityInfo (CulturalActivityInfo baseInfo) {

		if (_culturalActivityIdList.Contains (baseInfo.Id))
			return;

		CulturalActivityInfoList.Add (new CulturalActivityInfo (baseInfo));
		_culturalActivityIdList.Add (baseInfo.Id);
	}

	public void AddExistingCulturalSkillInfo (CulturalSkillInfo baseInfo) {

		if (_culturalSkillIdList.Contains (baseInfo.Id))
			return;
	
		CulturalSkillInfoList.Add (new CulturalSkillInfo (baseInfo));
		_culturalSkillIdList.Add (baseInfo.Id);
	}
	
	public void AddExistingCulturalKnowledgeInfo (CulturalKnowledgeInfo baseInfo) {
		
		if (_culturalKnowledgeIdList.Contains (baseInfo.Id))
			return;
		
		CulturalKnowledgeInfoList.Add (new CulturalKnowledgeInfo (baseInfo));
		_culturalKnowledgeIdList.Add (baseInfo.Id);
	}
	
	public void AddExistingCulturalDiscoveryInfo (CulturalDiscovery baseInfo) {
		
		if (_culturalDiscoveryIdList.Contains (baseInfo.Id))
			return;
		
		CulturalDiscoveryInfoList.Add (new CulturalDiscovery (baseInfo));
		_culturalDiscoveryIdList.Add (baseInfo.Id);
	}

	public void UpdateMostPopulousGroup (CellGroup contenderGroup) {
	
		if (MostPopulousGroup == null) {

			MostPopulousGroup = contenderGroup;

		} else if (MostPopulousGroup.Population < contenderGroup.Population) {
			
			MostPopulousGroup = contenderGroup;
		}
	}
	
	public void AddUpdatedGroup (CellGroup group) {
		
		_updatedGroups.Add (group);
	}

	public TerrainCell GetCell (WorldPosition position) {
	
		return GetCell (position.Longitude, position.Latitude);
	}
	
	public TerrainCell GetCell (int longitude, int latitude) {

		if ((longitude < 0) || (longitude >= Width))
			return null;
		
		if ((latitude < 0) || (latitude >= Height))
			return null;

		return TerrainCells[longitude][latitude];
	}

	public void SetMaxYearsToSkip (int value) {
	
		MaxYearsToSkip = Mathf.Max (value, 1);
	}

	public int Iterate () {

		if (CellGroupCount <= 0)
			return 0;
		
		//
		// Evaluate Events that will happen at the current date
		//

		int dateToSkipTo = CurrentDate + 1;

		while (true) {

			if (_eventsToHappen.Count <= 0) break;
		
			WorldEvent eventToHappen = _eventsToHappen.Leftmost;

			if (eventToHappen.TriggerDate > CurrentDate) {

				int maxDate = CurrentDate + MaxYearsToSkip;

				if (maxDate < 0) {
					
					#if DEBUG
					Debug.Break ();
					#endif

					throw new System.Exception ("Surpassed date limit (Int32.MaxValue)");
				}

				dateToSkipTo = Mathf.Min (eventToHappen.TriggerDate, maxDate);
				break;
			}

			_eventsToHappen.RemoveLeftmost ();
			EventsToHappenCount--;

			if (eventToHappen.CanTrigger ())
			{
				eventToHappen.Trigger ();
			}

			eventToHappen.Destroy ();
		}

//		//
//		// Preupdate groups that need it
//		//
//
//		foreach (CellGroup group in _groupsToUpdate) {
//
//			group.PreUpdate ();
//		}
		
		//
		// Update Human Groups
		//
	
		foreach (CellGroup group in _groupsToUpdate) {
		
			group.Update ();
		}
		
		_groupsToUpdate.Clear ();
		
		//
		// Migrate Human Groups
		//
		
		MigratingGroup[] currentGroupsToMigrate = _migratingGroups.ToArray();
		
		_migratingGroups.Clear ();
		
		foreach (MigratingGroup group in currentGroupsToMigrate) {
			
			group.SplitFromSourceGroup();
		}
		
		foreach (MigratingGroup group in currentGroupsToMigrate) {

			group.MoveToCell();
		}
		
		//
		// Perform Group Actions
		//

		foreach (IGroupAction action in _groupActionsToPerform) {
		
			action.Perform ();
		}

		_groupActionsToPerform.Clear ();
		
		//
		// Perform post update ops
		//
		
		foreach (CellGroup group in _updatedGroups) {
			
			group.PostUpdate ();
		}

		//
		// Remove Human Groups that have been set to be removed
		//

		foreach (CellGroup group in _groupsToRemove) {

			group.Destroy ();
		}

		_groupsToRemove.Clear ();
		
		//
		// Set next group updates
		//
		
		foreach (CellGroup group in _updatedGroups) {
			
			group.SetupForNextUpdate ();
			Manager.AddUpdatedCell (group.Cell, CellUpdateType.Group);
		}

		_updatedGroups.Clear ();

		//
		// Update Polities
		//

		foreach (Polity polity in _politiesToUpdate) {

			polity.Update ();
		}

		_politiesToUpdate.Clear ();

		//
		// Remove Polities that have been set to be removed
		//

		foreach (Polity polity in _politiesToRemove) {

			polity.Destroy ();
		}

		_politiesToRemove.Clear ();

		//
		// Skip to Next Event's Date
		//

		if (_eventsToHappen.Count > 0) {

			WorldEvent futureEventToHappen = _eventsToHappen.Leftmost;
			
			if (futureEventToHappen.TriggerDate < dateToSkipTo) {
				
				dateToSkipTo = futureEventToHappen.TriggerDate;
			}
		}

		int dateSpan = dateToSkipTo - CurrentDate;

		CurrentDate = dateToSkipTo;

		return dateSpan;
	}

	public void InsertEventToHappen (WorldEvent eventToHappen) {

		EventsToHappenCount++;

		_eventsToHappen.Insert (eventToHappen.TriggerDate, eventToHappen);
	}
	
	public void AddMigratingGroup (MigratingGroup group) {
		
		_migratingGroups.Add (group);

		// Source Group needs to be prepared for pre-upgrade
		_groupsToUpdate.Add (group.SourceGroup);

		// If Target Group is present, it also needs to be prepared for pre-upgrade
		if (group.TargetCell.Group != null) {
			_groupsToUpdate.Add (group.TargetCell.Group);
		}
	}
	
	public void AddGroup (CellGroup group) {

		_cellGroups.Add (group.Id, group);

		Manager.AddUpdatedCell (group.Cell, CellUpdateType.Group);

		CellGroupCount++;
	}
	
	public void RemoveGroup (CellGroup group) {

		_cellGroups.Remove (group.Id);
		
		CellGroupCount--;
	}
	
	public CellGroup GetGroup (long id) {

		CellGroup group;

		_cellGroups.TryGetValue (id, out group);

		return group;
	}

	public void AddGroupToUpdate (CellGroup group) {
	
		_groupsToUpdate.Add (group);
	}
	
	public void AddGroupToRemove (CellGroup group) {
		
		_groupsToRemove.Add (group);
	}

	public void AddLanguage (Language language) {

		_languages.Add (language.Id, language);

		LanguageCount++;
	}

	public void RemoveLanguage (Region language) {

		_languages.Remove (language.Id);

		LanguageCount--;
	}

	public Language GetLanguage (long id) {

		Language language;

		_languages.TryGetValue (id, out language);

		return language;
	}

	public void AddRegion (Region region) {

		_regions.Add (region.Id, region);

		RegionCount++;
	}

	public void RemoveRegion (Region region) {

		_regions.Remove (region.Id);

		RegionCount--;
	}

	public Region GetRegion (long id) {

		Region region;

		_regions.TryGetValue (id, out region);

		return region;
	}

	public void AddPolity (Polity polity) {

		_polities.Add (polity.Id, polity);

		PolityCount++;
	}

	public void RemovePolity (Polity polity) {

		_polities.Remove (polity.Id);

		PolityCount--;
	}

	public Polity GetPolity (long id) {

		Polity polity;

		_polities.TryGetValue (id, out polity);

		return polity;
	}

	public void AddPolityToUpdate (Polity polity) {

		_politiesToUpdate.Add (polity);
	}

	public void AddPolityToRemove (Polity polity) {

		_politiesToRemove.Add (polity);
	}

	public void FinalizeLoad () {

		TerrainCellChangesList.ForEach (c => {
			
			int index = c.Longitude + c.Latitude * Width;
			
			_terrainCellChangesListIndexes.Add (index);
		});

		Languages.ForEach (l => {

			_languages.Add (l.Id, l);
		});

		Regions.ForEach (r => {

			r.World = this;

			_regions.Add (r.Id, r);
		});

		Polities.ForEach (p => {

			p.World = this;

			_polities.Add (p.Id, p);
		});

		CellGroups.ForEach (g => {

			g.World = this;

			_cellGroups.Add (g.Id, g);
		});

		Languages.ForEach (l => {

			l.FinalizeLoad ();
		});

		Regions.ForEach (r => {

			r.FinalizeLoad ();
		});

		Polities.ForEach (p => {

			p.FinalizeLoad ();
		});

		CellGroups.ForEach (g => {
			
			g.FinalizeLoad ();
		});

		EventsToHappen.ForEach (e => {

			e.World = this;
			e.FinalizeLoad ();

			_eventsToHappen.Insert (e.TriggerDate, e);
		});

		CulturalActivityInfoList.ForEach (a => _culturalActivityIdList.Add (a.Id));
		CulturalSkillInfoList.ForEach (s => _culturalSkillIdList.Add (s.Id));
		CulturalKnowledgeInfoList.ForEach (k => _culturalKnowledgeIdList.Add (k.Id));
		CulturalDiscoveryInfoList.ForEach (d => _culturalDiscoveryIdList.Add (d.Id));
	}

	public void MigrationTagGroup (HumanGroup group) {
	
		MigrationUntagGroup ();
		
		MigrationTaggedGroup = group;

		group.MigrationTagged = true;
	}
	
	public void MigrationUntagGroup () {
		
		if (MigrationTaggedGroup != null)
			MigrationTaggedGroup.MigrationTagged = false;
	}
	
	public void GenerateTerrain () {
		
		ProgressCastMethod (_accumulatedProgress, "Generating Terrain...");
		
		GenerateTerrainAltitude ();
		
		ProgressCastMethod (_accumulatedProgress, "Calculating Rainfall...");
		
		GenerateTerrainRainfall ();
		
		//		ProgressCastMethod (_accumulatedProgress, "Generating Rivers...");
		//		
		//		GenerateTerrainRivers ();
		
		ProgressCastMethod (_accumulatedProgress, "Calculating Temperatures...");
		
		GenerateTerrainTemperature ();
		
		ProgressCastMethod (_accumulatedProgress, "Generating Biomes...");
		
		GenerateTerrainBiomes ();

		ProgressCastMethod (_accumulatedProgress, "Generating Arability...");

		GenerateTerrainArability ();
	}

	public void Generate () {

		GenerateTerrain ();
		
		ProgressCastMethod (_accumulatedProgress, "Finalizing...");
	}
	
	public void GenerateHumanGroup (int longitude, int latitude, int initialPopulation) {
			
		TerrainCell cell = GetCell (longitude, latitude);
		
		CellGroup group = new CellGroup(this, cell, initialPopulation);
		
		AddGroup(group);
	}

	public void GenerateRandomHumanGroups (int maxGroups, int initialPopulation) {

		ProgressCastMethod (_accumulatedProgress, "Adding Random Human Groups...");
		
		float minPresence = 0.50f;
		
		int sizeX = Width;
		int sizeY = Height;
		
		List<TerrainCell> SuitableCells = new List<TerrainCell> ();
		
		for (int i = 0; i < sizeX; i++) {
			
			for (int j = 0; j < sizeY; j++) {
				
				TerrainCell cell = TerrainCells [i] [j];
				
				float biomePresence = cell.GetBiomePresence(Biome.Grassland);
				
				if (biomePresence < minPresence) continue;
				
				SuitableCells.Add(cell);
			}
			
			ProgressCastMethod (_accumulatedProgress + _progressIncrement * (i + 1)/(float)sizeX);
		}
		
		_accumulatedProgress += _progressIncrement;

		maxGroups = Mathf.Min (SuitableCells.Count, maxGroups);
		
		bool first = true;
		
		for (int i = 0; i < maxGroups; i++) {
			
			ManagerTask<int> n = GenerateRandomInteger(0, SuitableCells.Count);
			
			TerrainCell cell = SuitableCells[n];
			
			//int population = (int)Mathf.Floor(StartPopulationDensity * cell.Area);
			
			CellGroup group = new CellGroup(this, cell, initialPopulation);
			
			AddGroup(group);
			
			if (first) {
				MigrationTagGroup(group);
				
				first = false;
			}
		}
	}

	private void GenerateContinents () {
		
		float longitudeFactor = 15f;
		float latitudeFactor = 6f;

		float minLatitude = Height / latitudeFactor;
		float maxLatitude = Height * (latitudeFactor - 1f) / latitudeFactor;
		
		Manager.EnqueueTaskAndWait (() => {
			
			Vector2 prevPos = new Vector2(
				Random.Range(0, Width),
				Random.Range(minLatitude, maxLatitude));

			//Vector2 prevPrevPos = prevPos;

			for (int i = 0; i < NumContinents; i++) {

				int widthOff = Random.Range(0, 2) * 3;
				
				_continentOffsets[i] = prevPos;
				_continentWidths[i] = Random.Range(ContinentMinWidthFactor + widthOff, ContinentMaxWidthFactor + widthOff);
				_continentHeights[i] = Random.Range(ContinentMinWidthFactor + widthOff, ContinentMaxWidthFactor + widthOff);

				float xPos = Mathf.Repeat(prevPos.x + Random.Range(Width / longitudeFactor, Width * 2 / longitudeFactor), Width);
				float yPos = Random.Range(minLatitude, maxLatitude);

				if (i % 3 == 2) {
					xPos = Mathf.Repeat(prevPos.x + Random.Range(Width * 4 / longitudeFactor, Width * 5 / longitudeFactor), Width);
				}

				Vector2 newPos = new Vector2(xPos, yPos);

				//prevPrevPos = prevPos;
				prevPos = newPos;
			}
			
			return true;
		});
	}
	
	private float GetContinentModifier (int x, int y) {

		float maxValue = 0;

		for (int i = 0; i < NumContinents; i++)
		{
			float dist = GetContinentDistance(i, x, y);

			float value = Mathf.Clamp(1f - dist/((float)Width), 0 , 1);

			float otherValue = value;

			if (maxValue < value) {

				otherValue = maxValue;
				maxValue = value;
			}

			float valueMod = otherValue;
			otherValue *= 2;
			otherValue = Mathf.Min(1, otherValue);

			maxValue = MathUtility.MixValues(maxValue, otherValue, valueMod);
		}

		return maxValue;
	}

	private float GetContinentDistance (int id, int x, int y) {
		
		float betaFactor = Mathf.Sin(Mathf.PI * y / Height);

		Vector2 continentOffset = _continentOffsets[id];
		float contX = continentOffset.x;
		float contY = continentOffset.y;
		
		float distX = Mathf.Min(Mathf.Abs(contX - x), Mathf.Abs(Width + contX - x));
		distX = Mathf.Min(distX, Mathf.Abs(contX - x - Width));
		distX *= betaFactor;
		
		float distY = Mathf.Abs(contY - y);
		
		float continentWidth = _continentWidths[id];
		float continentHeight = _continentHeights[id];
		
		return new Vector2(distX*continentWidth, distY*continentHeight).magnitude;
	}

	private void GenerateTerrainAltitude () {

		GenerateContinents();
		
		int sizeX = Width;
		int sizeY = Height;

		float radius1 = 0.75f;
		float radius1b = 1.25f;
		float radius2 = 8f;
		float radius3 = 4f;
		float radius4 = 8f;
		float radius5 = 16f;
		float radius6 = 64f;
		float radius7 = 128f;
		float radius8 = 1.5f;
		float radius9 = 1f;

		ManagerTask<Vector3> offset1 = GenerateRandomOffsetVector();
		ManagerTask<Vector3> offset2 = GenerateRandomOffsetVector();
		ManagerTask<Vector3> offset1b = GenerateRandomOffsetVector();
		ManagerTask<Vector3> offset2b = GenerateRandomOffsetVector();
		ManagerTask<Vector3> offset3 = GenerateRandomOffsetVector();
		ManagerTask<Vector3> offset4 = GenerateRandomOffsetVector();
		ManagerTask<Vector3> offset5 = GenerateRandomOffsetVector();
		ManagerTask<Vector3> offset6 = GenerateRandomOffsetVector();
		ManagerTask<Vector3> offset7 = GenerateRandomOffsetVector();
		ManagerTask<Vector3> offset8 = GenerateRandomOffsetVector();
		ManagerTask<Vector3> offset9 = GenerateRandomOffsetVector();
		
		for (int i = 0; i < sizeX; i++)
		{
			float beta = (i / (float)sizeX) * Mathf.PI * 2;
			
			for (int j = 0; j < sizeY; j++)
			{
				float alpha = (j / (float)sizeY) * Mathf.PI;

				float value1 = GetRandomNoiseFromPolarCoordinates(alpha, beta, radius1, offset1);
				float value2 = GetRandomNoiseFromPolarCoordinates(alpha, beta, radius2, offset2);
				float value1b = GetRandomNoiseFromPolarCoordinates(alpha, beta, radius1b, offset1b);
				float value2b = GetRandomNoiseFromPolarCoordinates(alpha, beta, radius2, offset2b);
				float value3 = GetRandomNoiseFromPolarCoordinates(alpha, beta, radius3, offset3);
				float value4 = GetRandomNoiseFromPolarCoordinates(alpha, beta, radius4, offset4);
				float value5 = GetRandomNoiseFromPolarCoordinates(alpha, beta, radius5, offset5);
				float value6 = GetRandomNoiseFromPolarCoordinates(alpha, beta, radius6, offset6);
				float value7 = GetRandomNoiseFromPolarCoordinates(alpha, beta, radius7, offset7);
				float value8 = GetRandomNoiseFromPolarCoordinates(alpha, beta, radius8, offset8);
				float value9 = GetRandomNoiseFromPolarCoordinates(alpha, beta, radius9, offset9);

				value8 = value8 * 1.5f + 0.25f;

				float valueA = GetContinentModifier(i, j);
				valueA = MathUtility.MixValues(valueA, value3, 0.22f * value8);
				valueA = MathUtility.MixValues(valueA, value4, 0.15f * value8);
				valueA = MathUtility.MixValues(valueA, value5, 0.1f * value8);
				valueA = MathUtility.MixValues(valueA, value6, 0.03f * value8);
				valueA = MathUtility.MixValues(valueA, value7, 0.005f * value8);
				
				float valueC = MathUtility.MixValues(value1, value9, 0.5f * value8);
				valueC = MathUtility.MixValues(valueC, value2, 0.04f * value8);
				valueC = GetMountainRangeNoiseFromRandomNoise(valueC, 25);
				float valueCb = MathUtility.MixValues(value1b, value9, 0.5f * value8);
				valueCb = MathUtility.MixValues(valueCb, value2b, 0.04f * value8);
				valueCb = GetMountainRangeNoiseFromRandomNoise(valueCb, 25);
				valueC = MathUtility.MixValues(valueC, valueCb, 0.5f * value8);

				valueC = MathUtility.MixValues(valueC, value3, 0.55f * value8);
				valueC = MathUtility.MixValues(valueC, value4, 0.075f);
				valueC = MathUtility.MixValues(valueC, value5, 0.05f);
				valueC = MathUtility.MixValues(valueC, value6, 0.02f);
				valueC = MathUtility.MixValues(valueC, value7, 0.01f);
				
//				float valueB = MathUtility.MixValues (valueA, (valueA * 0.02f) + 0.49f, Mathf.Max(0, 0.9f * valueA - Mathf.Max(0, (2f * valueC) - 1)));
//
//				float valueD = MathUtility.MixValues (valueB, valueC, 0.225f * value8);

				float valueB = MathUtility.MixValues (valueA, valueC, 0.35f * value8);

				float valueD = MathUtility.MixValues (valueB, (valueA * 0.02f) + 0.49f, Mathf.Clamp01(1.3f * valueA - Mathf.Max(0, (2.5f * valueC) - 1)));

				CalculateAndSetAltitude(i, j, valueD);
//				CalculateAndSetAltitude(i, j, valueA);
			}

			ProgressCastMethod (_accumulatedProgress + _progressIncrement * (i + 1)/(float)sizeX);
		}

		_accumulatedProgress += _progressIncrement;
	}
	
//	private void GenerateTerrainRivers () {
//
//		int sizeX = Width;
//		int sizeY = Height;
//		
//		float radius1 = 4f;
//		float radius2 = 12f;
//		
//		ManagerTask<Vector3> offset1 = GenerateRandomOffsetVector();
//		ManagerTask<Vector3> offset2 = GenerateRandomOffsetVector();
//		
//		for (int i = 0; i < sizeX; i++) {
//
//			float beta = (i / (float)sizeX) * Mathf.PI * 2;
//			
//			for (int j = 0; j < sizeY; j++) {
//
//				TerrainCell cell = Terrain[i][j];
//				
//				float alpha = (j / (float)sizeY) * Mathf.PI;
//				
//				float value1 = GetRandomNoiseFromPolarCoordinates(alpha, beta, radius1, offset1);
//				float value2 = GetRandomNoiseFromPolarCoordinates(alpha, beta, radius2, offset2);
//				
//				float altitudeValue = cell.Altitude;
//				float rainfallValue = cell.Rainfall;
//
//				float altitudeFactor = Mathf.Max(0, altitudeValue / MaxPossibleAltitude);
//				float depthFactor = Mathf.Max(0, altitudeValue / MinPossibleAltitude);
//				float altitudeFactor1 = Mathf.Clamp(10f * altitudeFactor, 0.25f , 1);
//				float rainfallFactor = Mathf.Max(0, rainfallValue / MaxPossibleRainfall);
//
//				float valueA = MathUtility.MixValues(value2, value1, altitudeFactor1);
//				valueA = GetRiverNoiseFromRandomNoise(valueA, 25);
//
//				if (altitudeValue >= 0) {
//					valueA = valueA * Mathf.Max(1 - altitudeFactor * rainfallFactor * 2.5f, 0);
//				} else {
//					valueA = valueA * Mathf.Max(1 - depthFactor * 8, 0);
//				}
//
//				float altitudeMod = valueA * MaxPossibleAltitude * 0.1f;
//
//				cell.Altitude -= altitudeMod;
//			}
//			
//			ProgressCastMethod (_accumulatedProgress + _progressIncrement * (i + 1)/(float)sizeX);
//		}
//		
//		_accumulatedProgress += _progressIncrement;
//	}
	
	private ManagerTask<int> GenerateRandomInteger (int min, int max) {
		
		return Manager.EnqueueTask (() => Random.Range(min, max));
	}

	private ManagerTask<Vector3> GenerateRandomOffsetVector () {

		return Manager.EnqueueTask (() => Random.insideUnitSphere * 1000);
	}

	// Returns a value between 0 and 1
	private float GetRandomNoiseFromPolarCoordinates (float alpha, float beta, float radius, Vector3 offset) {

		Vector3 pos = MathUtility.GetCartesianCoordinates(alpha,beta,radius) + offset;
		
		return PerlinNoise.GetValue(pos.x, pos.y, pos.z);
	}
	
	private float GetMountainRangeNoiseFromRandomNoise(float noise, float widthFactor) {

		noise = (noise * 2) - 1;
		
		float value1 = -Mathf.Exp (-Mathf.Pow (noise * widthFactor + 1f, 2));
		float value2 = Mathf.Exp(-Mathf.Pow(noise * widthFactor - 1f, 2));

		float value = (value1 + value2 + 1) / 2f;
		
		return value;
	}
	
	private float GetRiverNoiseFromRandomNoise(float noise, float widthFactor) {
		
		noise = (noise * 2) - 1;

		float value = Mathf.Exp(-Mathf.Pow(noise * widthFactor, 2));
		
		value = (value + 1) / 2f;
		
		return value;
	}

	private float CalculateAltitude (float value) {
	
		float span = MaxPossibleAltitude - MinPossibleAltitude;

		float altitude = (value * span) + MinPossibleAltitude;

		altitude -= Manager.SeaLevelOffset;

		altitude = Mathf.Clamp (altitude, MinPossibleAltitudeWithOffset, MaxPossibleAltitudeWithOffset);

		return altitude;
	}
	
	private void CalculateAndSetAltitude (int longitude, int latitude, float value) {
		
		float altitude = CalculateAltitude(value);
		TerrainCells[longitude][latitude].Altitude = altitude;
		
		if (altitude > MaxAltitude) MaxAltitude = altitude;
		if (altitude < MinAltitude) MinAltitude = altitude;
	}
	
	private void GenerateTerrainRainfall () {
		
		int sizeX = Width;
		int sizeY = Height;
		
		float radius1 = 2f;
		float radius2 = 1f;
		float radius3 = 16f;
		
		ManagerTask<Vector3> offset1 = GenerateRandomOffsetVector();
		ManagerTask<Vector3> offset2 = GenerateRandomOffsetVector();
		ManagerTask<Vector3> offset3 = GenerateRandomOffsetVector();
		
		for (int i = 0; i < sizeX; i++)
		{
			float beta = (i / (float)sizeX) * Mathf.PI * 2;
			
			for (int j = 0; j < sizeY; j++)
			{
				TerrainCell cell = TerrainCells[i][j];
				
				float alpha = (j / (float)sizeY) * Mathf.PI;
				
				float value1 = GetRandomNoiseFromPolarCoordinates(alpha, beta, radius1, offset1);
				float value2 = GetRandomNoiseFromPolarCoordinates(alpha, beta, radius2, offset2);
				float value3 = GetRandomNoiseFromPolarCoordinates(alpha, beta, radius3, offset3);

				value2 = value2 * 1.5f + 0.25f;

				float valueA = MathUtility.MixValues(value1, value3, 0.15f);

				float latitudeFactor = alpha + (((valueA * 2) - 1f) * Mathf.PI * 0.2f);
				float latitudeModifier1 = (1.5f * Mathf.Sin(latitudeFactor)) - 0.5f;
				float latitudeModifier2 = Mathf.Cos(latitudeFactor);

				int offCellX = (Width + i + (int)Mathf.Floor(latitudeModifier2 * Width/40f)) % Width;
				int offCellX2 = (Width + i + (int)Mathf.Floor(latitudeModifier2 * Width/20f)) % Width;
				int offCellX3 = (Width + i + (int)Mathf.Floor(latitudeModifier2 * Width/10f)) % Width;
				int offCellX4 = (Width + i + (int)Mathf.Floor(latitudeModifier2 * Width/5f)) % Width;
				int offCellY = j + (int)Mathf.Floor(latitudeModifier2 * Height/10f);

				TerrainCell offCell = TerrainCells[offCellX][j];
				TerrainCell offCell2 = TerrainCells[offCellX2][j];
				TerrainCell offCell3 = TerrainCells[offCellX3][j];
				TerrainCell offCell4 = TerrainCells[offCellX4][j];
				TerrainCell offCell5 = TerrainCells[i][offCellY];

				float altitudeValue = Mathf.Max(0, cell.Altitude);
				float offAltitude = Mathf.Max(0, offCell.Altitude);
				float offAltitude2 = Mathf.Max(0, offCell2.Altitude);
				float offAltitude3 = Mathf.Max(0, offCell3.Altitude);
				float offAltitude4 = Mathf.Max(0, offCell4.Altitude);
				float offAltitude5 = Mathf.Max(0, offCell5.Altitude);

				float altitudeModifier = (altitudeValue - 
				                          (offAltitude * 0.7f) - 
				                          (offAltitude2 * 0.6f) -  
				                          (offAltitude3 * 0.5f) -
				                          (offAltitude4 * 0.4f) - 
				                          (offAltitude5 * 0.5f) + 
				                          (MaxPossibleAltitude * 0.17f * value2) -
				                          (altitudeValue * 0.25f)) / MaxPossibleAltitude;
				float rainfallValue = MathUtility.MixValues(latitudeModifier1, altitudeModifier, 0.85f);
				rainfallValue = MathUtility.MixValues(Mathf.Abs(rainfallValue) * rainfallValue, rainfallValue, 0.75f);

				float rainfall = Mathf.Min(MaxPossibleRainfall, CalculateRainfall(rainfallValue));
				cell.Rainfall = rainfall;

				if (rainfall > MaxRainfall) MaxRainfall = rainfall;
				if (rainfall < MinRainfall) MinRainfall = rainfall;
			}
			
			ProgressCastMethod (_accumulatedProgress + _progressIncrement * (i + 1)/(float)sizeX);
		}
		
		_accumulatedProgress += _progressIncrement;
	}
	
	private void GenerateTerrainTemperature () {
		
		int sizeX = Width;
		int sizeY = Height;
		
		float radius1 = 2f;
		float radius2 = 16f;
		
		ManagerTask<Vector3> offset1 = GenerateRandomOffsetVector();
		ManagerTask<Vector3> offset2 = GenerateRandomOffsetVector();
		
		for (int i = 0; i < sizeX; i++)
		{
			float beta = (i / (float)sizeX) * Mathf.PI * 2;
			
			for (int j = 0; j < sizeY; j++)
			{
				TerrainCell cell = TerrainCells[i][j];
				
				float alpha = (j / (float)sizeY) * Mathf.PI;
				
				float value1 = GetRandomNoiseFromPolarCoordinates(alpha, beta, radius1, offset1);
				float value2 = GetRandomNoiseFromPolarCoordinates(alpha, beta, radius2, offset2);

				float latitudeModifier = (alpha * 0.9f) + ((value1 + value2) * 0.05f * Mathf.PI);

				float altitudeSpan = MaxPossibleAltitude - MinPossibleAltitude;

				float absAltitude = cell.Altitude - MinPossibleAltitudeWithOffset;
				
				float altitudeFactor1 = (absAltitude / altitudeSpan) * 0.7f;
				float altitudeFactor2 = Mathf.Max(0, (cell.Altitude / MaxPossibleAltitude) * 1.3f);
				float altitudeFactor3 = -0.18f;
				
				float temperature = CalculateTemperature(Mathf.Sin(latitudeModifier) - altitudeFactor1 - altitudeFactor2 - altitudeFactor3);

				cell.Temperature = temperature;
				
				if (temperature > MaxTemperature) MaxTemperature = temperature;
				if (temperature < MinTemperature) MinTemperature = temperature;
			}
			
			ProgressCastMethod (_accumulatedProgress + _progressIncrement * (i + 1)/(float)sizeX);
		}
		
		_accumulatedProgress += _progressIncrement;
	}

	private void GenerateTerrainArability () {

		int sizeX = Width;
		int sizeY = Height;

		float radius = 2f;

		ManagerTask<Vector3> offset = GenerateRandomOffsetVector();

		for (int i = 0; i < sizeX; i++)
		{
			float beta = (i / (float)sizeX) * Mathf.PI * 2;

			for (int j = 0; j < sizeY; j++)
			{
				TerrainCell cell = TerrainCells[i][j];

				float alpha = (j / (float)sizeY) * Mathf.PI;

				float baseArability = CalculateCellBaseArability (cell);

				cell.Arability = 0;

				if (baseArability <= 0)
					continue;

				// This simulates things like stoniness, impracticality of drainage, excessive salts, etc.
				float noiseFactor = 0.0f + 1.0f * GetRandomNoiseFromPolarCoordinates(alpha, beta, radius, offset);

				cell.Arability = baseArability * noiseFactor;
			}

			ProgressCastMethod (_accumulatedProgress + _progressIncrement * (i + 1)/(float)sizeX);
		}

		_accumulatedProgress += _progressIncrement;
	}

	private void GenerateTerrainBiomes () {
		
		int sizeX = Width;
		int sizeY = Height;
		
		for (int i = 0; i < sizeX; i++) {

			for (int j = 0; j < sizeY; j++) {

				TerrainCell cell = TerrainCells[i][j];

				float totalPresence = 0;

				Dictionary<string, float> biomePresences = new Dictionary<string, float> ();

				foreach (Biome biome in Biome.Biomes.Values) {

					float presence = CalculateBiomePresence (cell, biome);

					if (presence <= 0) continue;

					biomePresences.Add(biome.Name, presence);

					totalPresence += presence;
				}

				cell.Survivability = 0;
				cell.ForagingCapacity = 0;
				cell.Accessibility = 0;

				foreach (Biome biome in Biome.Biomes.Values)
				{
					float presence = 0;

					if (biomePresences.TryGetValue(biome.Name, out presence))
					{
						presence = presence/totalPresence;

						cell.AddBiomePresence (biome.Name, presence);

						cell.Survivability += biome.Survivability * presence;
						cell.ForagingCapacity += biome.ForagingCapacity * presence;
						cell.Accessibility += biome.Accessibility * presence;
					}
				}

				float altitudeSurvivabilityFactor = 1 - (cell.Altitude / MaxPossibleAltitude);

				cell.Survivability *= altitudeSurvivabilityFactor;
			}
			
			ProgressCastMethod (_accumulatedProgress + _progressIncrement * (i + 1)/(float)sizeX);
		}
		
		_accumulatedProgress += _progressIncrement;
	}

	public long GenerateCellGroupId () {
	
		return ++CurrentCellGroupId;
	}
	
	public long GenerateEventId () {
		
		return ++CurrentEventId;
	}

	public long GeneratePolityId () {

		return ++CurrentPolityId;
	}

	public long GenerateRegionId () {

		return ++CurrentRegionId;
	}

	public long GenerateLanguageId () {

		return ++CurrentLanguageId;
	}

	private float CalculateCellBaseArability (TerrainCell cell) {

		float landFactor = 1 - cell.GetBiomePresence (Biome.Ocean);

		if (landFactor == 0)
			return 0;

		float rainfallFactor = 0;

		if (cell.Rainfall > OptimalRainfallForArability) {
		
			rainfallFactor = (MaxRainfallForArability - cell.Rainfall) / (MaxRainfallForArability - OptimalRainfallForArability);

		} else {
			
			rainfallFactor = (cell.Rainfall - MinRainfallForArability) / (OptimalRainfallForArability - MinRainfallForArability);
		}

		rainfallFactor = Mathf.Clamp01 (rainfallFactor);

		float temperatureFactor = (cell.Temperature - MinTemperatureForArability) / (OptimalTemperatureForArability - MinTemperatureForArability);
		temperatureFactor = Mathf.Clamp01 (temperatureFactor);

		return rainfallFactor * temperatureFactor * landFactor;
	}

	private float CalculateBiomePresence (TerrainCell cell, Biome biome) {

		float presence = 1f;

		// Altitude

		float altitudeSpan = biome.MaxAltitude - biome.MinAltitude;


		float altitudeDiff = cell.Altitude - biome.MinAltitude;

		if (altitudeDiff < 0)
			return -1f;

		float altitudeFactor = altitudeDiff / altitudeSpan;

		if (float.IsInfinity (altitudeSpan)) {

			altitudeFactor = 0.5f;
		}
		
		if (altitudeFactor > 1)
			return -1f;

		if (altitudeFactor > 0.5f)
			altitudeFactor = 1f - altitudeFactor;

		presence *= altitudeFactor*2;

		// Rainfall
		
		float rainfallSpan = biome.MaxRainfall - biome.MinRainfall;
		
		float rainfallDiff = cell.Rainfall - biome.MinRainfall;
		
		if (rainfallDiff < 0)
			return -1f;
		
		float rainfallFactor = rainfallDiff / rainfallSpan;
		
		if (float.IsInfinity (rainfallSpan)) {
			
			rainfallFactor = 0.5f;
		}
		
		if (rainfallFactor > 1)
			return -1f;
		
		if (rainfallFactor > 0.5f)
			rainfallFactor = 1f - rainfallFactor;
		
		presence *= rainfallFactor*2;
		
		// Temperature
		
		float temperatureSpan = biome.MaxTemperature - biome.MinTemperature;
		
		float temperatureDiff = cell.Temperature - biome.MinTemperature;
		
		if (temperatureDiff < 0)
			return -1f;
		
		float temperatureFactor = temperatureDiff / temperatureSpan;
		
		if (float.IsInfinity (temperatureSpan)) {
			
			temperatureFactor = 0.5f;
		}
		
		if (temperatureFactor > 1)
			return -1f;
		
		if (temperatureFactor > 0.5f)
			temperatureFactor = 1f - temperatureFactor;
		
		presence *= temperatureFactor*2;

		return presence;
	}
	
	private float CalculateRainfall (float value) {

		float drynessOffset = MaxPossibleRainfall * RainfallDrynessOffsetFactor;
		
		float span = MaxPossibleRainfall - MinPossibleRainfall + drynessOffset;

		float rainfall = (value * span) + MinPossibleRainfall - drynessOffset;

		rainfall += Manager.RainfallOffset;

		rainfall = Mathf.Clamp(rainfall, 0, MaxPossibleRainfallWithOffset);
		
		return rainfall;
	}
	
	private float CalculateTemperature (float value) {
		
		float span = MaxPossibleTemperature - MinPossibleTemperature;
		
		float temperature = (value * span) + MinPossibleTemperature;

		temperature += Manager.TemperatureOffset;
		
		temperature = Mathf.Clamp(temperature, MinPossibleTemperatureWithOffset, MaxPossibleTemperatureWithOffset);
		
		return temperature;
	}
}
