﻿using UnityEngine;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Linq;

public abstract class SuperRegion : Region
{
    public List<long> SubRegionIds;

    private HashSet<Region> _subRegions = new HashSet<Region>();

    private HashSet<TerrainCell> _cells = null;

    private HashSet<TerrainCell> _innerBorderCells = null;

    private TerrainCell _mostCenteredCell = null;

    public SuperRegion()
    {

    }

    public SuperRegion(TerrainCell originCell, Language language)
    {
        Info = new RegionInfo(this, originCell, language);
    }

    private void RefreshCells()
    {
        if (_cells != null)
            return;

        _cells = new HashSet<TerrainCell>();

        foreach (Region region in _subRegions)
        {
            _cells.UnionWith(region.GetCells());
        }
    }

    public override ICollection<TerrainCell> GetCells()
    {
        RefreshCells();

        return _cells;
    }

    public override bool IsWithinRegion(TerrainCell cell)
    {
        Region cellRegion = cell.Region;

        if (cellRegion == null)
            return false;

        if (!cellRegion.IsEqualToOrDescentantFrom(this))
            return false;

        return true;
    }

    public override bool IsInnerBorderCell(TerrainCell cell)
    {
        Region cellRegion = cell.Region;

        if (cellRegion == null)
            return false;

        if (!cellRegion.IsEqualToOrDescentantFrom(this))
            return false;

        if (!cellRegion.IsInnerBorderCell(cell))
            return false;

        foreach (TerrainCell nCell in cell.Neighbors.Values)
        {
            if (!IsWithinRegion(nCell))
                return true;
        }

        return false;
    }

    public override void Synchronize()
    {
        SubRegionIds = new List<long>();

        foreach (Region region in _subRegions)
        {
            SubRegionIds.Add(region.Id);
        }
    }

    public override void FinalizeLoad()
    {
        foreach (long id in SubRegionIds)
        {
            RegionInfo info = World.GetRegionInfo(id);

            _subRegions.Add(info.Region);

            info.Region.Parent = this;
        }
    }

    private void RefreshMostCenteredCell()
    {
        if (_mostCenteredCell != null)
            return;

        RefreshCells();

        int centerLongitude = 0, centerLatitude = 0;

        foreach (TerrainCell cell in _cells)
        {
            centerLongitude += cell.Longitude;
            centerLatitude += cell.Latitude;
        }

        centerLongitude /= _cells.Count;
        centerLatitude /= _cells.Count;

        TerrainCell centerCell = World.GetCell(centerLongitude, centerLatitude);

        if (IsWithinRegion(centerCell))
        {
            _mostCenteredCell = centerCell;
            return;
        }

        TerrainCell closestCell = null;
        int closestDistCenter = int.MaxValue;

        foreach (TerrainCell cell in _cells)
        {
            int distCenter = Mathf.Abs(cell.Longitude - centerLongitude) + Mathf.Abs(cell.Latitude - centerLatitude);

            if ((closestCell == null) || (distCenter < closestDistCenter))
            {
                closestDistCenter = distCenter;
                closestCell = cell;
            }
        }

        _mostCenteredCell = closestCell;
    }

    public override TerrainCell GetMostCenteredCell()
    {
        RefreshMostCenteredCell();

        return _mostCenteredCell;
    }
}
