using System;
using MegaCrit.Sts2.Core.Map;

namespace YuWanCard.Relics;

public class ScaledActMap : ActMap
{
    private const int MaxSerializedMapCoordRow = byte.MaxValue;

    private readonly MapPoint?[,] _grid;
    private readonly MapPoint _bossPoint;
    private readonly MapPoint _startingPoint;
    private readonly MapPoint? _secondBossPoint;

    public ScaledActMap(ActMap original, int multiplier = 2)
    {
        if (multiplier < 1)
            throw new ArgumentOutOfRangeException(nameof(multiplier), multiplier, "Map length multiplier must be at least 1.");

        int cols = original.GetColumnCount();
        int origRows = original.GetRowCount();
        int newRows = origRows * multiplier;
        int maxRegularRows = original.SecondBossMapPoint != null
            ? MaxSerializedMapCoordRow - 1
            : MaxSerializedMapCoordRow;
        if (newRows > maxRegularRows)
        {
            throw new InvalidOperationException(
                $"Scaled map row count {newRows} exceeds the serialized map row limit {maxRegularRows}. " +
                "This would corrupt saved map coords and break run history.");
        }

        _grid = new MapPoint[cols, newRows];

        for (int copy = 0; copy < multiplier; copy++)
        {
            int rowOffset = copy * origRows;
            for (int r = 0; r < origRows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var origPoint = original.GetPoint(new MapCoord(c, r));
                    if (origPoint == null) continue;
                    if (ReferenceEquals(origPoint, original.StartingMapPoint)) continue;
                    if (ReferenceEquals(origPoint, original.BossMapPoint)) continue;
                    if (ReferenceEquals(origPoint, original.SecondBossMapPoint)) continue;

                    var p = new MapPoint(c, r + rowOffset)
                    {
                        PointType = origPoint.PointType,
                        CanBeModified = origPoint.CanBeModified
                    };
                    _grid[c, r + rowOffset] = p;
                }
            }
        }

        for (int copy = 0; copy < multiplier; copy++)
        {
            int rowOffset = copy * origRows;
            for (int r = 0; r < origRows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var origPoint = original.GetPoint(new MapCoord(c, r));
                    if (origPoint == null) continue;
                    if (ReferenceEquals(origPoint, original.StartingMapPoint)) continue;
                    if (ReferenceEquals(origPoint, original.BossMapPoint)) continue;
                    if (ReferenceEquals(origPoint, original.SecondBossMapPoint)) continue;

                    var p = _grid[c, r + rowOffset];
                    if (p == null) continue;

                    foreach (var child in origPoint.Children)
                    {
                        if (child.PointType == MapPointType.Boss) continue;

                        var childCopy = _grid[child.coord.col, child.coord.row + rowOffset];
                        if (childCopy != null)
                            p.AddChildPoint(childCopy);
                    }
                }
            }
        }

        for (int copy = 0; copy < multiplier - 1; copy++)
        {
            int restRow = (copy + 1) * origRows - 1;
            for (int c = 0; c < cols; c++)
            {
                var p = _grid[c, restRow];
                if (p != null && p.PointType == MapPointType.RestSite)
                {
                    p.PointType = MapPointType.Monster;
                    p.CanBeModified = true;
                }
            }
        }

        for (int copy = 1; copy < multiplier; copy++)
        {
            int ancientRow = copy * origRows;
            for (int c = 0; c < cols; c++)
            {
                var p = _grid[c, ancientRow];
                if (p != null && p.PointType == MapPointType.Ancient)
                {
                    p.PointType = MapPointType.Monster;
                    p.CanBeModified = true;
                }
            }
        }

        _startingPoint = new MapPoint(original.StartingMapPoint.coord.col, original.StartingMapPoint.coord.row)
        {
            PointType = original.StartingMapPoint.PointType,
            CanBeModified = original.StartingMapPoint.CanBeModified
        };

        for (int c = 0; c < cols; c++)
        {
            var p = _grid[c, 1];
            if (p != null)
            {
                _startingPoint.AddChildPoint(p);
                startMapPoints.Add(p);
            }
        }

        for (int copy = 0; copy < multiplier - 1; copy++)
        {
            int bridgeRow = (copy + 1) * origRows;
            int firstHalfLastRow = bridgeRow - 1;
            int secondHalfFirstRow = bridgeRow + 1;

            var bridge = _grid[cols / 2, bridgeRow];
            if (bridge == null)
            {
                bridge = new MapPoint(cols / 2, bridgeRow)
                {
                    PointType = MapPointType.Monster,
                    CanBeModified = true
                };
                _grid[cols / 2, bridgeRow] = bridge;
            }

            for (int c = 0; c < cols; c++)
            {
                var prevLast = _grid[c, firstHalfLastRow];
                if (prevLast != null)
                    prevLast.AddChildPoint(bridge);
            }

            for (int c = 0; c < cols; c++)
            {
                var nextFirst = _grid[c, secondHalfFirstRow];
                if (nextFirst != null)
                    bridge.AddChildPoint(nextFirst);
            }
        }

        _bossPoint = new MapPoint(cols / 2, newRows)
        {
            PointType = MapPointType.Boss
        };

        for (int c = 0; c < cols; c++)
        {
            var lastPoint = _grid[c, newRows - 1];
            if (lastPoint != null)
                lastPoint.AddChildPoint(_bossPoint);
        }

        if (original.SecondBossMapPoint != null)
        {
            _secondBossPoint = new MapPoint(cols / 2, newRows + 1)
            {
                PointType = MapPointType.Boss
            };
            _bossPoint.AddChildPoint(_secondBossPoint);
        }

        MainFile.Logger.Info(
            $"[ScaledActMap] Map scaled: {origRows} x {multiplier} -> {newRows} rows");
    }

    public override MapPoint BossMapPoint => _bossPoint;
    public override MapPoint StartingMapPoint => _startingPoint;
    public override MapPoint? SecondBossMapPoint => _secondBossPoint;
    protected override MapPoint?[,] Grid => _grid;
}
