﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

public class Tribe : Polity {

	public static string[] TribeNounVariations = new string[] { "tribe", "people", "folk", "community", "[ipn(man)]men", "[ipn(woman)]women", "[ipn(child)]children" };

	public const float BaseCoreInfluence = 0.5f;

	public Tribe () {

	}

	private Tribe (CellGroup coreGroup, float coreGroupInfluence) : base (coreGroup, coreGroupInfluence) {

	}

	public static Tribe GenerateNewTribe (CellGroup coreGroup) {

		float randomValue = coreGroup.Cell.GetNextLocalRandomFloat ();
		float coreInfluence = BaseCoreInfluence + randomValue * (1 - BaseCoreInfluence);

		coreInfluence *= 1 - coreGroup.TotalPolityInfluenceValue;
	
		Tribe newTribe = new Tribe (coreGroup, coreInfluence);

		return newTribe;
	}

	public override void UpdateInternal ()
	{
		TryRelocateCore ();
	}

	protected override void GenerateName ()
	{
		Region coreRegion = CoreGroup.Cell.Region;

		int randomInt = CoreGroup.GetNextLocalRandomInt (TribeNounVariations.Length);

		string tribeNounVariation = TribeNounVariations[randomInt];

		string regionAttributeNounVariation = coreRegion.GetRandomAttributeVariation (CoreGroup.GetNextLocalRandomInt);

		if (regionAttributeNounVariation != string.Empty) {
			regionAttributeNounVariation += " ";
		}

		string untranslatedName = "the [nad]" + regionAttributeNounVariation + tribeNounVariation;

		Language.NounPhrase namePhrase = Culture.Language.TranslateNounPhrase (untranslatedName, CoreGroup.GetNextLocalRandomFloat);

		Name = new Name (namePhrase, untranslatedName, Culture.Language, World);

//		#if DEBUG
//		Debug.Log ("Tribe #" + Id + " name: " + Name);
//		#endif
	}
}
