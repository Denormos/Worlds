﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class NotCondition : UnaryOpCondition
{
    public NotCondition(string conditionStr) : base(conditionStr)
    {
    }

    public override bool Evaluate(CellGroup group)
    {
        return !Condition.Evaluate(group);
    }

    public override bool Evaluate(TerrainCell cell)
    {
        return !Condition.Evaluate(cell);
    }

    public override string ToString()
    {
        return "NOT (" + Condition.ToString() + ")";
    }
}
