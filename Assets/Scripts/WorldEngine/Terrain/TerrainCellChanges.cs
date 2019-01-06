using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

public class TerrainCellChanges
{
    [XmlAttribute("Lon")]
    public int Longitude;
    [XmlAttribute("Lat")]
    public int Latitude;

    [XmlAttribute("A")]
    public float Altitude;
    [XmlAttribute("B")]
    public float BaseValue;
    [XmlAttribute("T")]
    public float Temperature;
    [XmlAttribute("R")]
    public float Rainfall;

    [XmlAttribute("Fp")]
    public float FarmlandPercentage = 0;

    public List<string> Flags = new List<string>();

    public TerrainCellChanges()
    {
        Manager.UpdateWorldLoadTrackEventCount();
    }

    public TerrainCellChanges(TerrainCell cell)
    {
        Longitude = cell.Longitude;
        Latitude = cell.Latitude;

        Altitude = cell.Altitude;
        BaseValue = cell.BaseValue;
        Temperature = cell.Temperature;
        Rainfall = cell.Rainfall;
        
        FarmlandPercentage = cell.FarmlandPercentage;
    }
}
