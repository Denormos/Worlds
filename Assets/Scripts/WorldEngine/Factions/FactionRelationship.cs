﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine.Profiling;

public class FactionRelationship
{
    [XmlAttribute("Val")]
    public float Value;

    public Identifier Id;

    [XmlIgnore]
    public Faction Faction;

    public FactionRelationship()
    {
    }

    public FactionRelationship(Faction faction, float value)
    {
        Faction = faction;

        Id = faction.Id;

        Value = value;
    }
}
